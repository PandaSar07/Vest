using Microsoft.AspNetCore.Mvc;
using Vest.Services;

namespace Vest.Controllers
{
    [ApiController]
    [Route("market")]
    public class MarketController : ControllerBase
    {
        /// <summary>Drop obvious non-US Finnhub symbology (e.g. AAPL.SW) even if search leaks; keep BRK.B-style class shares.</summary>
        private static bool IsUsListedSymbol(string symbol)
        {
            if (string.IsNullOrWhiteSpace(symbol)) return false;
            if (symbol.Contains('-', StringComparison.Ordinal)) return false;
            var i = symbol.LastIndexOf('.');
            if (i < 0) return true;
            var suffix = symbol[(i + 1)..];
            return suffix.Length < 2;
        }

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

        /// <summary>Latest crypto market news (not tied to a single token).</summary>
        [HttpGet("crypto-news")]
        public async Task<IActionResult> GetCryptoNews()
        {
            try
            {
                var news = await _finnhubService.GetCryptoNewsAsync();
                return Ok(news);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>Symbol autocomplete for navbar search.</summary>
        [HttpGet("searchsymbols")]
        public async Task<IActionResult> SearchSymbols([FromQuery] string q)
        {
            if (string.IsNullOrWhiteSpace(q) || q.Trim().Length < 1)
                return Ok(new { result = Array.Empty<object>() });

            try
            {
                var raw = await _finnhubService.SearchSymbolsAsync(q.Trim());
                if (raw is null || raw.Value.ValueKind != System.Text.Json.JsonValueKind.Object)
                    return Ok(new { result = Array.Empty<object>() });

                if (!raw.Value.TryGetProperty("result", out var results) || results.ValueKind != System.Text.Json.JsonValueKind.Array)
                    return Ok(new { result = Array.Empty<object>() });

                // US equities only (Finnhub search uses exchange=US; types exclude crypto / non-equity).
                var allowedTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Common Stock", "ADR", "ETF", "ETP" };
                var mapped = results.EnumerateArray()
                    .Where(x => x.TryGetProperty("symbol", out var s) && s.ValueKind == System.Text.Json.JsonValueKind.String)
                    .Select(x => new
                    {
                        symbol = x.GetProperty("symbol").GetString() ?? "",
                        description = x.TryGetProperty("description", out var d) && d.ValueKind == System.Text.Json.JsonValueKind.String ? d.GetString() ?? "" : "",
                        type = x.TryGetProperty("type", out var t) && t.ValueKind == System.Text.Json.JsonValueKind.String ? t.GetString() ?? "" : ""
                    })
                    .Where(x => !string.IsNullOrWhiteSpace(x.symbol))
                    .Where(x => string.IsNullOrWhiteSpace(x.type) || allowedTypes.Contains(x.type))
                    .Where(x => IsUsListedSymbol(x.symbol))
                    .Take(10)
                    .ToArray();

                return Ok(new { result = mapped });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }
}
