using Microsoft.AspNetCore.Mvc;
using Vest.Services;

namespace Vest.Controllers;

[ApiController]
[Route("portfolio")]
public class PortfolioController : ControllerBase
{
    private readonly PortfolioService _portfolio;
    private readonly FinnhubService   _finnhub;

    public PortfolioController(PortfolioService portfolio, FinnhubService finnhub)
    {
        _portfolio = portfolio;
        _finnhub   = finnhub;
    }

    private string? CurrentUserId => HttpContext.Session.GetString("UserId");
    private static bool HasHundredthPrecision(decimal shares) =>
        decimal.Round(shares, 2, MidpointRounding.AwayFromZero) == shares;

    private static bool IsUsEquityMarketOpen()
    {
        DateTime etNow;
        try { etNow = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateTime.UtcNow, "Eastern Standard Time"); }
        catch (TimeZoneNotFoundException) { etNow = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateTime.UtcNow, "America/New_York"); }
        if (etNow.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday) return false;
        var now = etNow.TimeOfDay;
        return now >= new TimeSpan(9, 30, 0) && now < new TimeSpan(16, 0, 0);
    }

    private static bool IsLegacySyntheticOptionSymbol(string symbol) =>
        symbol.Contains("|OPT|", StringComparison.Ordinal);

    // ── GET /portfolio/summary ──────────────────────────────────────────────

    [HttpGet("summary")]
    public async Task<IActionResult> Summary()
    {
        var userId = CurrentUserId;
        if (userId == null) return Unauthorized(new { error = "Not logged in." });
        try
        {
            var portfolio = await _portfolio.GetOrCreatePortfolioAsync(userId);
            var holdings  = await _portfolio.GetHoldingsAsync(userId);
            // Exclude legacy synthetic option symbols from holdings display and totals.
            var equityRows = holdings.Where(h => !IsLegacySyntheticOptionSymbol(h.Symbol)).ToList();

            var priceMap  = new Dictionary<string, decimal>();
            var sectorMap = new Dictionary<string, string>();

            await Task.WhenAll(equityRows.Select(async h =>
            {
                try
                {
                    var quoteSym = QuoteSymbolResolver.ForFinnhubQuote(h.Symbol);
                    var q = await _finnhub.GetFullQuoteAsync(quoteSym);
                    if (q.HasValue && q.Value.TryGetProperty("c", out var c) && c.GetDecimal() != 0)
                        priceMap[h.Symbol] = c.GetDecimal();

                    if (!QuoteSymbolResolver.IsUsdCryptoSpot(h.Symbol))
                    {
                        var p = await _finnhub.GetCompanyProfileAsync(h.Symbol);
                        if (p.HasValue && p.Value.TryGetProperty("finnhubIndustry", out var ind))
                            sectorMap[h.Symbol] = ind.GetString() ?? "Other";
                    }
                    else
                        sectorMap[h.Symbol] = "Crypto";
                }
                catch { }
            }));

            var riskRules = await _portfolio.GetRiskRulesForUserAsync(userId);
            var riskBySymbol = riskRules.ToDictionary(r => r.Symbol, StringComparer.OrdinalIgnoreCase);

            var holdingDtos = equityRows.Select(h =>
            {
                var livePrice   = priceMap.GetValueOrDefault(h.Symbol, h.AvgCost);
                var mktValue    = Math.Round(h.Shares * livePrice, 2);
                var costBasis   = Math.Round(h.Shares * h.AvgCost, 2);
                var gainLoss    = Math.Round(mktValue - costBasis, 2);
                var gainLossPct = costBasis == 0 ? 0 : Math.Round(gainLoss / costBasis * 100, 2);
                riskBySymbol.TryGetValue(h.Symbol, out var risk);
                return new
                {
                    h.Symbol, h.Shares, h.AvgCost,
                    LivePrice   = livePrice,
                    MarketValue = mktValue,
                    GainLoss    = gainLoss,
                    GainLossPct = gainLossPct,
                    Sector      = sectorMap.GetValueOrDefault(h.Symbol, "Other"),
                    Risk        = risk == null ? null : MapRiskDto(risk),
                };
            }).ToList();

            var stockValue = holdingDtos.Sum(h => h.MarketValue);
            var totalValue = portfolio.Cash + stockValue;

            // Take daily snapshot (idempotent via UNIQUE constraint)
            _ = _portfolio.TakeSnapshotAsync(userId, totalValue);

            return Ok(new
            {
                Cash = portfolio.Cash,
                StockValue = stockValue,
                TotalValue = totalValue,
                Holdings = holdingDtos,
            });
        }
        catch (Exception ex) { return StatusCode(500, new { error = ex.Message }); }
    }

    // ── GET /portfolio/trades ───────────────────────────────────────────────

    [HttpGet("trades")]
    public async Task<IActionResult> Trades([FromQuery] int limit = 20)
    {
        var userId = CurrentUserId;
        if (userId == null) return Unauthorized(new { error = "Not logged in." });
        try { return Ok(await _portfolio.GetTradesAsync(userId, limit)); }
        catch (Exception ex) { return StatusCode(500, new { error = ex.Message }); }
    }

    // ── GET /portfolio/snapshots ────────────────────────────────────────────

    [HttpGet("snapshots")]
    public async Task<IActionResult> Snapshots([FromQuery] int days = 30)
    {
        var userId = CurrentUserId;
        if (userId == null) return Unauthorized(new { error = "Not logged in." });
        try { return Ok(await _portfolio.GetSnapshotsAsync(userId, days)); }
        catch (Exception ex) { return StatusCode(500, new { error = ex.Message }); }
    }

    // ── GET /portfolio/holding ──────────────────────────────────────────────

    [HttpGet("holding")]
    public async Task<IActionResult> Holding([FromQuery] string symbol)
    {
        var userId = CurrentUserId;
        if (userId == null) return Unauthorized(new { error = "Not logged in." });
        try
        {
            var portfolio = await _portfolio.GetOrCreatePortfolioAsync(userId);
            var sym = symbol.Trim().ToUpperInvariant();
            var holding = await _portfolio.GetHoldingAsync(userId, sym);
            var risk    = await _portfolio.GetRiskRuleAsync(userId, sym);
            return Ok(new
            {
                Cash = portfolio.Cash,
                Shares = holding?.Shares ?? 0m,
                AvgCost = holding?.AvgCost ?? 0m,
                Risk = risk == null ? null : MapRiskDto(risk),
            });
        }
        catch (Exception ex) { return StatusCode(500, new { error = ex.Message }); }
    }

    // ── POST /portfolio/buy ─────────────────────────────────────────────────

    [HttpPost("buy")]
    public async Task<IActionResult> Buy([FromBody] TradeRequest req)
    {
        var userId = CurrentUserId;
        if (userId == null) return Unauthorized(new { error = "Not logged in." });
        if (string.IsNullOrWhiteSpace(req.Symbol) || req.Shares <= 0)
            return BadRequest(new { error = "Symbol and positive shares are required." });
        if (!HasHundredthPrecision(req.Shares))
            return BadRequest(new { error = "Shares must be in increments of 0.01." });
        try
        {
            var sym = req.Symbol.Trim().ToUpperInvariant();
            if (!QuoteSymbolResolver.IsUsdCryptoSpot(sym) && !IsUsEquityMarketOpen())
                return BadRequest(new { error = "Market is closed. Buying is only available during regular market hours (9:30 AM - 4:00 PM ET)." });
            var quoteSym = QuoteSymbolResolver.ForFinnhubQuote(sym);
            var q = await _finnhub.GetFullQuoteAsync(quoteSym);
            if (q == null || !q.Value.TryGetProperty("c", out var pEl) || pEl.GetDecimal() == 0)
                return BadRequest(new { error = "Could not fetch live price." });
            var price = pEl.GetDecimal();
            var (ok, error) = await _portfolio.ExecuteBuyAsync(userId, sym, req.Shares, price);
            if (!ok) return BadRequest(new { error });

            string? riskWarning = null;
            object? risk = null;
            if (HasRiskParams(req))
            {
                var holding = await _portfolio.GetHoldingAsync(userId, sym);
                var entry   = holding?.AvgCost ?? price;
                var (rok, rerr, rule) = await _portfolio.UpsertRiskRuleAsync(
                    userId, sym, entry,
                    req.StopLossPrice, req.StopLossPct,
                    req.TakeProfitPrice, req.TakeProfitPct);
                if (!rok) riskWarning = rerr;
                else if (rule != null) risk = MapRiskDto(rule);
            }

            var p = await _portfolio.GetOrCreatePortfolioAsync(userId);
            return Ok(new
            {
                message = $"Bought {req.Shares} \u00d7 {sym} @ ${price:N2}",
                newCash = p.Cash,
                price,
                riskWarning,
                risk,
            });
        }
        catch (Exception ex) { return StatusCode(500, new { error = ex.Message }); }
    }

    // ── POST /portfolio/sell ────────────────────────────────────────────────

    [HttpPost("sell")]
    public async Task<IActionResult> Sell([FromBody] TradeRequest req)
    {
        var userId = CurrentUserId;
        if (userId == null) return Unauthorized(new { error = "Not logged in." });
        if (string.IsNullOrWhiteSpace(req.Symbol) || req.Shares <= 0)
            return BadRequest(new { error = "Symbol and positive shares are required." });
        if (!HasHundredthPrecision(req.Shares))
            return BadRequest(new { error = "Shares must be in increments of 0.01." });
        try
        {
            var sym = req.Symbol.Trim().ToUpperInvariant();
            if (!QuoteSymbolResolver.IsUsdCryptoSpot(sym) && !IsUsEquityMarketOpen())
                return BadRequest(new { error = "Market is closed. Selling is only available during regular market hours (9:30 AM - 4:00 PM ET)." });
            var quoteSym = QuoteSymbolResolver.ForFinnhubQuote(sym);
            var q = await _finnhub.GetFullQuoteAsync(quoteSym);
            if (q == null || !q.Value.TryGetProperty("c", out var pEl) || pEl.GetDecimal() == 0)
                return BadRequest(new { error = "Could not fetch live price." });
            var price = pEl.GetDecimal();
            var (ok, error) = await _portfolio.ExecuteSellAsync(userId, sym, req.Shares, price);
            if (!ok) return BadRequest(new { error });
            var p = await _portfolio.GetOrCreatePortfolioAsync(userId);
            return Ok(new { message = $"Sold {req.Shares} \u00d7 {sym} @ ${price:N2}", newCash = p.Cash, price });
        }
        catch (Exception ex) { return StatusCode(500, new { error = ex.Message }); }
    }

    // ── GET /portfolio/orders ───────────────────────────────────────────────

    [HttpGet("orders")]
    public async Task<IActionResult> GetOrders()
    {
        var userId = CurrentUserId;
        if (userId == null) return Unauthorized(new { error = "Not logged in." });
        try { return Ok(await _portfolio.GetPendingOrdersAsync(userId)); }
        catch (Exception ex) { return StatusCode(500, new { error = ex.Message }); }
    }

    // ── POST /portfolio/order ───────────────────────────────────────────────

    [HttpPost("order")]
    public async Task<IActionResult> PlaceOrder([FromBody] LimitOrderRequest req)
    {
        var userId = CurrentUserId;
        if (userId == null) return Unauthorized(new { error = "Not logged in." });
        if (string.IsNullOrWhiteSpace(req.Symbol) || req.Shares <= 0 || req.LimitPrice <= 0)
            return BadRequest(new { error = "Symbol, shares, and limit price are required." });
        if (!HasHundredthPrecision(req.Shares))
            return BadRequest(new { error = "Shares must be in increments of 0.01." });
        var action = req.Action?.ToUpper();
        if (action != "BUY" && action != "SELL")
            return BadRequest(new { error = "Action must be BUY or SELL." });
        try
        {
            var sym = req.Symbol.Trim().ToUpperInvariant();
            var order = await _portfolio.PlaceLimitOrderAsync(
                userId, sym, action, req.Shares, req.LimitPrice);
            return Ok(new { message = $"Limit {action} order placed for {req.Shares} \u00d7 {sym} @ ${req.LimitPrice:N2}", order });
        }
        catch (Exception ex) { return StatusCode(500, new { error = ex.Message }); }
    }

    // ── DELETE /portfolio/order/{id} ────────────────────────────────────────

    [HttpDelete("order/{id:long}")]
    public async Task<IActionResult> CancelOrder(long id)
    {
        var userId = CurrentUserId;
        if (userId == null) return Unauthorized(new { error = "Not logged in." });
        try
        {
            var ok = await _portfolio.CancelOrderAsync(userId, id);
            return ok ? Ok(new { message = "Order cancelled." }) : NotFound(new { error = "Order not found or already filled." });
        }
        catch (Exception ex) { return StatusCode(500, new { error = ex.Message }); }
    }

    // ── Risk rules (stop-loss / take-profit) ────────────────────────────────

    [HttpGet("risk")]
    public async Task<IActionResult> GetRisk([FromQuery] string symbol)
    {
        var userId = CurrentUserId;
        if (userId == null) return Unauthorized(new { error = "Not logged in." });
        if (string.IsNullOrWhiteSpace(symbol))
            return BadRequest(new { error = "Symbol is required." });
        try
        {
            var sym  = symbol.Trim().ToUpperInvariant();
            var rule = await _portfolio.GetRiskRuleAsync(userId, sym);
            return Ok(rule == null ? null : MapRiskDto(rule));
        }
        catch (Exception ex) { return StatusCode(500, new { error = ex.Message }); }
    }

    [HttpPut("risk")]
    public async Task<IActionResult> UpsertRisk([FromBody] RiskRuleRequest req)
    {
        var userId = CurrentUserId;
        if (userId == null) return Unauthorized(new { error = "Not logged in." });
        if (string.IsNullOrWhiteSpace(req.Symbol))
            return BadRequest(new { error = "Symbol is required." });

        try
        {
            var sym     = req.Symbol.Trim().ToUpperInvariant();
            var holding = await _portfolio.GetHoldingAsync(userId, sym);
            if (holding == null || holding.Shares <= 0)
                return BadRequest(new { error = "You need an open position to set risk rules." });

            var entry = req.EntryPrice > 0 ? req.EntryPrice : holding.AvgCost;
            var (ok, error, rule) = await _portfolio.UpsertRiskRuleAsync(
                userId, sym, entry,
                req.StopLossPrice, req.StopLossPct,
                req.TakeProfitPrice, req.TakeProfitPct);
            if (!ok) return BadRequest(new { error });
            return Ok(MapRiskDto(rule!));
        }
        catch (Exception ex) { return StatusCode(500, new { error = ex.Message }); }
    }

    [HttpDelete("risk")]
    public async Task<IActionResult> DeleteRisk([FromQuery] string symbol)
    {
        var userId = CurrentUserId;
        if (userId == null) return Unauthorized(new { error = "Not logged in." });
        if (string.IsNullOrWhiteSpace(symbol))
            return BadRequest(new { error = "Symbol is required." });
        try
        {
            var sym = symbol.Trim().ToUpperInvariant();
            await _portfolio.CancelRiskRuleAsync(userId, sym);
            return Ok(new { message = "Risk rules removed." });
        }
        catch (Exception ex) { return StatusCode(500, new { error = ex.Message }); }
    }

    private static bool HasRiskParams(TradeRequest req) =>
        req.StopLossPrice.HasValue || req.StopLossPct.HasValue ||
        req.TakeProfitPrice.HasValue || req.TakeProfitPct.HasValue;

    private static object MapRiskDto(PositionRiskRuleRow rule) => new
    {
        rule.Symbol,
        rule.EntryPrice,
        stopLossPrice   = rule.StopLossPrice,
        takeProfitPrice = rule.TakeProfitPrice,
        stopLossPct     = rule.StopLossPct,
        takeProfitPct   = rule.TakeProfitPct,
        rule.Status,
    };
}

public class TradeRequest
{
    public string Symbol { get; set; } = "";
    public decimal Shares { get; set; }
    public decimal? StopLossPrice { get; set; }
    public decimal? StopLossPct { get; set; }
    public decimal? TakeProfitPrice { get; set; }
    public decimal? TakeProfitPct { get; set; }
}

public class RiskRuleRequest
{
    public string Symbol { get; set; } = "";
    public decimal EntryPrice { get; set; }
    public decimal? StopLossPrice { get; set; }
    public decimal? StopLossPct { get; set; }
    public decimal? TakeProfitPrice { get; set; }
    public decimal? TakeProfitPct { get; set; }
}

public class LimitOrderRequest { public string? Action { get; set; } public string Symbol { get; set; } = ""; public decimal Shares { get; set; } public decimal LimitPrice { get; set; } }
