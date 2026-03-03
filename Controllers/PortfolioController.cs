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

    // ── Auth guard ──────────────────────────────────────────────────────────

    private string? CurrentUserId =>
        HttpContext.Session.GetString("UserId");

    // ── GET /portfolio/summary ──────────────────────────────────────────────
    // Returns cash, holdings with live prices, and total portfolio value.

    [HttpGet("summary")]
    public async Task<IActionResult> Summary()
    {
        var userId = CurrentUserId;
        if (userId == null) return Unauthorized(new { error = "Not logged in." });

        try
        {
            var portfolio = await _portfolio.GetOrCreatePortfolioAsync(userId);
            var holdings  = await _portfolio.GetHoldingsAsync(userId);

            // Fetch live prices for all holdings in parallel
            var priceMap = new Dictionary<string, decimal>();
            await Task.WhenAll(holdings.Select(async h =>
            {
                try
                {
                    var q = await _finnhub.GetFullQuoteAsync(h.Symbol);
                    if (q.HasValue && q.Value.TryGetProperty("c", out var c))
                        priceMap[h.Symbol] = c.GetDecimal();
                }
                catch { /* ignore per-symbol failures */ }
            }));

            var holdingDtos = holdings.Select(h =>
            {
                var livePrice  = priceMap.GetValueOrDefault(h.Symbol, h.AvgCost);
                var mktValue   = Math.Round(h.Shares * livePrice, 2);
                var costBasis  = Math.Round(h.Shares * h.AvgCost, 2);
                var gainLoss   = Math.Round(mktValue - costBasis, 2);
                var gainLossPct = costBasis == 0 ? 0 : Math.Round(gainLoss / costBasis * 100, 2);
                return new
                {
                    h.Symbol, h.Shares, h.AvgCost,
                    LivePrice = livePrice,
                    MarketValue = mktValue,
                    GainLoss = gainLoss,
                    GainLossPct = gainLossPct,
                };
            }).ToList();

            var stockValue = holdingDtos.Sum(h => h.MarketValue);
            var totalValue = portfolio.Cash + stockValue;

            return Ok(new
            {
                Cash       = portfolio.Cash,
                StockValue = stockValue,
                TotalValue = totalValue,
                Holdings   = holdingDtos,
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    // ── GET /portfolio/trades ───────────────────────────────────────────────

    [HttpGet("trades")]
    public async Task<IActionResult> Trades([FromQuery] int limit = 20)
    {
        var userId = CurrentUserId;
        if (userId == null) return Unauthorized(new { error = "Not logged in." });

        try
        {
            var trades = await _portfolio.GetTradesAsync(userId, limit);
            return Ok(trades);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    // ── POST /portfolio/buy ─────────────────────────────────────────────────

    [HttpPost("buy")]
    public async Task<IActionResult> Buy([FromBody] TradeRequest req)
    {
        var userId = CurrentUserId;
        if (userId == null) return Unauthorized(new { error = "Not logged in." });
        if (string.IsNullOrWhiteSpace(req.Symbol) || req.Shares <= 0)
            return BadRequest(new { error = "Symbol and positive shares are required." });

        try
        {
            // Get live price
            var q = await _finnhub.GetFullQuoteAsync(req.Symbol.ToUpper());
            if (q == null || !q.Value.TryGetProperty("c", out var priceEl) || priceEl.GetDecimal() == 0)
                return BadRequest(new { error = "Could not fetch live price for this symbol." });

            var price = priceEl.GetDecimal();
            var (ok, error) = await _portfolio.ExecuteBuyAsync(userId, req.Symbol.ToUpper(), req.Shares, price);

            if (!ok) return BadRequest(new { error });

            var portfolio = await _portfolio.GetOrCreatePortfolioAsync(userId);
            return Ok(new { message = $"Bought {req.Shares} × {req.Symbol.ToUpper()} @ ${price:N2}", newCash = portfolio.Cash, price });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    // ── POST /portfolio/sell ────────────────────────────────────────────────

    [HttpPost("sell")]
    public async Task<IActionResult> Sell([FromBody] TradeRequest req)
    {
        var userId = CurrentUserId;
        if (userId == null) return Unauthorized(new { error = "Not logged in." });
        if (string.IsNullOrWhiteSpace(req.Symbol) || req.Shares <= 0)
            return BadRequest(new { error = "Symbol and positive shares are required." });

        try
        {
            var q = await _finnhub.GetFullQuoteAsync(req.Symbol.ToUpper());
            if (q == null || !q.Value.TryGetProperty("c", out var priceEl) || priceEl.GetDecimal() == 0)
                return BadRequest(new { error = "Could not fetch live price for this symbol." });

            var price = priceEl.GetDecimal();
            var (ok, error) = await _portfolio.ExecuteSellAsync(userId, req.Symbol.ToUpper(), req.Shares, price);

            if (!ok) return BadRequest(new { error });

            var portfolio = await _portfolio.GetOrCreatePortfolioAsync(userId);
            return Ok(new { message = $"Sold {req.Shares} × {req.Symbol.ToUpper()} @ ${price:N2}", newCash = portfolio.Cash, price });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    // ── GET /portfolio/holding?symbol= ──────────────────────────────────────

    [HttpGet("holding")]
    public async Task<IActionResult> Holding([FromQuery] string symbol)
    {
        var userId = CurrentUserId;
        if (userId == null) return Unauthorized(new { error = "Not logged in." });

        try
        {
            var portfolio = await _portfolio.GetOrCreatePortfolioAsync(userId);
            var holding   = await _portfolio.GetHoldingAsync(userId, symbol.ToUpper());
            return Ok(new
            {
                Cash   = portfolio.Cash,
                Shares = holding?.Shares ?? 0m,
                AvgCost = holding?.AvgCost ?? 0m,
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }
}

public class TradeRequest
{
    public string  Symbol { get; set; } = "";
    public decimal Shares { get; set; }
}
