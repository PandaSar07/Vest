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

        /// <summary>Full real-time quote with change, % change, high, low, open, prev-close.</summary>
        [HttpGet("fullquote")]
        public async Task<IActionResult> GetFullQuote(string symbol)
        {
            if (string.IsNullOrWhiteSpace(symbol))
                return BadRequest("Symbol is required.");

            try
            {
                var quote = await _finnhubService.GetFullQuoteAsync(symbol.ToUpper());
                return Ok(quote);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>Company profile (name, logo, exchange, industry, currency, market cap).</summary>
        [HttpGet("profile")]
        public async Task<IActionResult> GetProfile(string symbol)
        {
            if (string.IsNullOrWhiteSpace(symbol))
                return BadRequest("Symbol is required.");

            try
            {
                var profile = await _finnhubService.GetCompanyProfileAsync(symbol.ToUpper());
                return Ok(profile);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>Latest company news headlines (last 7 days).</summary>
        [HttpGet("news")]
        public async Task<IActionResult> GetNews(string symbol)
        {
            if (string.IsNullOrWhiteSpace(symbol))
                return BadRequest("Symbol is required.");

            try
            {
                var news = await _finnhubService.GetCompanyNewsAsync(symbol.ToUpper());
                return Ok(news);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>Historical close prices via Yahoo Finance (free, no key needed).</summary>
        [HttpGet("history")]
        public async Task<IActionResult> GetHistory(string symbol, string range = "1mo", string interval = "1d")
        {
            if (string.IsNullOrWhiteSpace(symbol))
                return BadRequest("Symbol is required.");

            try
            {
                var data = await _finnhubService.GetHistoricalDataAsync(symbol.ToUpper(), range, interval);
                return Ok(data);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }
}
