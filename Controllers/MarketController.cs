using Microsoft.AspNetCore.Mvc;
using Vest.Services;

namespace Vest.Controllers
{
    [ApiController]
    [Route("market")]
    public class MarketController : ControllerBase
    {
        private readonly FinnhubService _finnhubService;

        public MarketController(FinnhubService finnhubService)
        {
            _finnhubService = finnhubService;
        }

        [HttpGet("quote")]
        public async Task<IActionResult> GetQuote(string symbol)
        {
            if (string.IsNullOrWhiteSpace(symbol))
            {
                return BadRequest("Symbol is required.");
            }

            var price = await _finnhubService.GetQuoteAsync(symbol);
            if (price == null)
            {
                return NotFound($"Quote for symbol '{symbol}' not found.");
            }

            return Ok(new { symbol, price });
        }

        [HttpGet("candles")]
        public async Task<IActionResult> GetCandles(string symbol, string resolution = "D")
        {
            if (string.IsNullOrWhiteSpace(symbol))
            {
                return BadRequest("Symbol is required.");
            }

            try
            {
                // Default to last 30 days
                long to = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                long from = DateTimeOffset.UtcNow.AddDays(-30).ToUnixTimeSeconds();

                var data = await _finnhubService.GetCandlesAsync(symbol, resolution, from, to);
                return Content(data, "application/json");
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message, details = ex.InnerException?.Message });
            }
        }
    }
}
