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
    }
}
