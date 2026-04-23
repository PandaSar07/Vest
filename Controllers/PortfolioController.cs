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

    private static bool IsOptionSymbol(string symbol) =>
        symbol.Contains("|OPT|", StringComparison.Ordinal);

    private static string BuildOptionHoldingSymbol(OptionTradeRequest req)
    {
        var underlying = req.Underlying.Trim().ToUpperInvariant();
        var expiry = req.Expiration.ToString("yyyyMMdd");
        var type = req.OptionType.Trim().ToUpperInvariant();
        var strike = req.Strike.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture);
        return $"{underlying}|OPT|{expiry}|{type}|{strike}";
    }

    private static bool TryParseOptionSymbol(string symbol, out ParsedOptionContract parsed)
    {
        parsed = default;
        var parts = symbol.Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length != 5 || !parts[1].Equals("OPT", StringComparison.OrdinalIgnoreCase))
            return false;

        if (!DateOnly.TryParseExact(parts[2], "yyyyMMdd", out var expiration))
            return false;

        if (!decimal.TryParse(parts[4], System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.InvariantCulture, out var strike))
            return false;

        parsed = new ParsedOptionContract(
            parts[0].ToUpperInvariant(),
            expiration,
            parts[3].ToUpperInvariant(),
            strike);
        return parsed.OptionType is "CALL" or "PUT";
    }

    private static decimal EstimateOptionPremiumPerShare(decimal underlyingPrice, ParsedOptionContract contract, DateOnly utcToday)
    {
        var days = Math.Max(0, contract.Expiration.DayNumber - utcToday.DayNumber);
        var years = Math.Max(0.01m, days / 365m);
        const decimal volatility = 0.30m;
        const decimal timeValueFloor = 0.10m;
        var timeValue = Math.Max(timeValueFloor, underlyingPrice * volatility * (decimal)Math.Sqrt((double)years) * 0.15m);

        var intrinsic = contract.OptionType == "CALL"
            ? Math.Max(0m, underlyingPrice - contract.Strike)
            : Math.Max(0m, contract.Strike - underlyingPrice);

        return Math.Round(intrinsic + timeValue, 2, MidpointRounding.AwayFromZero);
    }

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

            var priceMap  = new Dictionary<string, decimal>();
            var sectorMap = new Dictionary<string, string>();

            await Task.WhenAll(holdings.Select(async h =>
            {
                try
                {
                    if (TryParseOptionSymbol(h.Symbol, out var optionContract))
                    {
                        var underlyingQuoteSym = QuoteSymbolResolver.ForFinnhubQuote(optionContract.Underlying);
                        var uq = await _finnhub.GetFullQuoteAsync(underlyingQuoteSym);
                        if (uq.HasValue && uq.Value.TryGetProperty("c", out var uc) && uc.GetDecimal() != 0)
                        {
                            var premium = EstimateOptionPremiumPerShare(uc.GetDecimal(), optionContract, DateOnly.FromDateTime(DateTime.UtcNow));
                            priceMap[h.Symbol] = premium * 100m; // per-contract price
                        }
                        sectorMap[h.Symbol] = "Options";
                        return;
                    }

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

            var holdingDtos = holdings.Select(h =>
            {
                var livePrice   = priceMap.GetValueOrDefault(h.Symbol, h.AvgCost);
                var mktValue    = Math.Round(h.Shares * livePrice, 2);
                var costBasis   = Math.Round(h.Shares * h.AvgCost, 2);
                var gainLoss    = Math.Round(mktValue - costBasis, 2);
                var gainLossPct = costBasis == 0 ? 0 : Math.Round(gainLoss / costBasis * 100, 2);
                return new
                {
                    h.Symbol, h.Shares, h.AvgCost,
                    LivePrice   = livePrice,
                    MarketValue = mktValue,
                    GainLoss    = gainLoss,
                    GainLossPct = gainLossPct,
                    Sector      = sectorMap.GetValueOrDefault(h.Symbol, "Other"),
                };
            }).ToList();

            var stockValue = holdingDtos.Sum(h => h.MarketValue);
            var totalValue = portfolio.Cash + stockValue;

            // Take daily snapshot (idempotent via UNIQUE constraint)
            _ = _portfolio.TakeSnapshotAsync(userId, totalValue);

            return Ok(new { Cash = portfolio.Cash, StockValue = stockValue, TotalValue = totalValue, Holdings = holdingDtos });
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
            var holding   = await _portfolio.GetHoldingAsync(userId, sym);
            return Ok(new { Cash = portfolio.Cash, Shares = holding?.Shares ?? 0m, AvgCost = holding?.AvgCost ?? 0m });
        }
        catch (Exception ex) { return StatusCode(500, new { error = ex.Message }); }
    }

    [HttpGet("options/position")]
    public async Task<IActionResult> OptionPosition([FromQuery] string underlying)
    {
        var userId = CurrentUserId;
        if (userId == null) return Unauthorized(new { error = "Not logged in." });
        if (string.IsNullOrWhiteSpace(underlying)) return BadRequest(new { error = "Underlying is required." });

        try
        {
            var portfolio = await _portfolio.GetOrCreatePortfolioAsync(userId);
            var under = underlying.Trim().ToUpperInvariant();
            var holdings = await _portfolio.GetHoldingsAsync(userId);
            var optionHoldings = holdings
                .Where(h => h.Symbol.StartsWith($"{under}|OPT|", StringComparison.Ordinal))
                .Select(h =>
                {
                    if (!TryParseOptionSymbol(h.Symbol, out var c)) return null;
                    return new
                    {
                        Symbol = h.Symbol,
                        c.Expiration,
                        c.OptionType,
                        c.Strike,
                        Contracts = h.Shares,
                        AvgContractCost = h.AvgCost
                    };
                })
                .Where(x => x != null)
                .OrderBy(x => x!.Expiration)
                .ThenBy(x => x!.OptionType)
                .ThenBy(x => x!.Strike)
                .ToList();

            return Ok(new { Cash = portfolio.Cash, Contracts = optionHoldings });
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
            var p = await _portfolio.GetOrCreatePortfolioAsync(userId);
            return Ok(new { message = $"Bought {req.Shares} \u00d7 {sym} @ ${price:N2}", newCash = p.Cash, price });
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

    [HttpPost("options/buy")]
    public async Task<IActionResult> BuyOption([FromBody] OptionTradeRequest req)
    {
        var userId = CurrentUserId;
        if (userId == null) return Unauthorized(new { error = "Not logged in." });
        var validation = ValidateOptionRequest(req);
        if (validation != null) return BadRequest(new { error = validation });
        if (!IsUsEquityMarketOpen())
            return BadRequest(new { error = "Options trading is only available during regular market hours (9:30 AM - 4:00 PM ET)." });

        try
        {
            var symbol = BuildOptionHoldingSymbol(req);
            var contractPrice = Math.Round(req.Premium * 100m, 2, MidpointRounding.AwayFromZero);
            var (ok, error) = await _portfolio.ExecuteBuyAsync(userId, symbol, req.Contracts, contractPrice);
            if (!ok) return BadRequest(new { error });
            var p = await _portfolio.GetOrCreatePortfolioAsync(userId);
            return Ok(new
            {
                message = $"Bought {req.Contracts} {req.OptionType.ToUpperInvariant()} contract(s) on {req.Underlying.ToUpperInvariant()} {req.Expiration:yyyy-MM-dd} ${req.Strike:N2} @ ${req.Premium:N2}/share",
                newCash = p.Cash
            });
        }
        catch (Exception ex) { return StatusCode(500, new { error = ex.Message }); }
    }

    [HttpPost("options/sell")]
    public async Task<IActionResult> SellOption([FromBody] OptionTradeRequest req)
    {
        var userId = CurrentUserId;
        if (userId == null) return Unauthorized(new { error = "Not logged in." });
        var validation = ValidateOptionRequest(req);
        if (validation != null) return BadRequest(new { error = validation });
        if (!IsUsEquityMarketOpen())
            return BadRequest(new { error = "Options trading is only available during regular market hours (9:30 AM - 4:00 PM ET)." });

        try
        {
            var symbol = BuildOptionHoldingSymbol(req);
            var contractPrice = Math.Round(req.Premium * 100m, 2, MidpointRounding.AwayFromZero);
            var (ok, error) = await _portfolio.ExecuteSellAsync(userId, symbol, req.Contracts, contractPrice);
            if (!ok) return BadRequest(new { error });
            var p = await _portfolio.GetOrCreatePortfolioAsync(userId);
            return Ok(new
            {
                message = $"Sold {req.Contracts} {req.OptionType.ToUpperInvariant()} contract(s) on {req.Underlying.ToUpperInvariant()} {req.Expiration:yyyy-MM-dd} ${req.Strike:N2} @ ${req.Premium:N2}/share",
                newCash = p.Cash
            });
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

    private static string? ValidateOptionRequest(OptionTradeRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Underlying))
            return "Underlying symbol is required.";
        if (req.Contracts <= 0 || decimal.Truncate(req.Contracts) != req.Contracts)
            return "Contracts must be a positive whole number.";
        if (req.Strike <= 0)
            return "Strike price must be positive.";
        if (req.Premium <= 0)
            return "Premium must be positive.";
        if (req.Expiration < DateOnly.FromDateTime(DateTime.UtcNow.Date))
            return "Expiration must be today or later.";
        var type = req.OptionType?.Trim().ToUpperInvariant();
        if (type is not ("CALL" or "PUT"))
            return "Option type must be CALL or PUT.";
        return null;
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
}

public class TradeRequest      { public string Symbol { get; set; } = ""; public decimal Shares { get; set; } }
public class LimitOrderRequest { public string? Action { get; set; } public string Symbol { get; set; } = ""; public decimal Shares { get; set; } public decimal LimitPrice { get; set; } }
public class OptionTradeRequest
{
    public string Underlying { get; set; } = "";
    public string OptionType { get; set; } = "CALL";
    public decimal Strike { get; set; }
    public DateOnly Expiration { get; set; }
    public decimal Contracts { get; set; }
    public decimal Premium { get; set; } // per-share premium, brokerage-style quote
}

public readonly record struct ParsedOptionContract(
    string Underlying,
    DateOnly Expiration,
    string OptionType,
    decimal Strike);
