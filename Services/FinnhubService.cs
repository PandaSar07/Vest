using System.Net.Http;
using System.Text.Json;
using Microsoft.Extensions.Configuration;

namespace Vest.Services
{
    public class FinnhubService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;

        public FinnhubService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _apiKey = configuration["Finnhub:ApiKey"]
                      ?? throw new Exception("Finnhub API key not found");
        }

        /// <summary>Full real-time quote: c, d, dp, h, l, o, pc, t</summary>
        public async Task<JsonElement?> GetFullQuoteAsync(string symbol)
        {
            var url = $"https://finnhub.io/api/v1/quote?symbol={Uri.EscapeDataString(symbol)}&token={_apiKey}";
            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);

            // Clone so we can return after doc is disposed
            return doc.RootElement.Clone();
        }

        /// <summary>Company profile: name, logo, exchange, industry, currency, market cap, etc.</summary>
        public async Task<JsonElement?> GetCompanyProfileAsync(string symbol)
        {
            var url = $"https://finnhub.io/api/v1/stock/profile2?symbol={Uri.EscapeDataString(symbol)}&token={_apiKey}";
            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            return doc.RootElement.Clone();
        }

        /// <summary>Latest company/market news headlines.</summary>
        public async Task<JsonElement?> GetCompanyNewsAsync(string symbol)
        {
            // Finnhub requires a date range for company news
            var to = DateTimeOffset.UtcNow.ToString("yyyy-MM-dd");
            var from = DateTimeOffset.UtcNow.AddDays(-7).ToString("yyyy-MM-dd");
            var url = $"https://finnhub.io/api/v1/company-news?symbol={Uri.EscapeDataString(symbol)}&from={from}&to={to}&token={_apiKey}";

            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            return doc.RootElement.Clone();
        }

        /// <summary>
        /// Historical OHLCV data via Yahoo Finance (free, no key needed).
        /// range: 1d | 5d | 1mo | 3mo | 6mo | 1y | 2y | 5y | max
        /// interval: 1m | 5m | 15m | 30m | 1h | 1d | 1wk | 1mo
        /// Returns a normalised object: { timestamps, opens, highs, lows, closes }
        /// </summary>
        public async Task<object> GetHistoricalDataAsync(string symbol, string range, string interval)
        {
            var url = $"https://query1.finance.yahoo.com/v8/finance/chart/{Uri.EscapeDataString(symbol)}" +
                      $"?interval={interval}&range={range}&includePrePost=false";

            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            // Yahoo Finance requires a browser-like User-Agent
            request.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
            request.Headers.Add("Accept", "application/json");

            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);

            var result = doc.RootElement
                .GetProperty("chart")
                .GetProperty("result")[0];

            var timestamps = result.GetProperty("timestamp").EnumerateArray()
                .Select(t => t.GetInt64()).ToArray();

            var quote = result
                .GetProperty("indicators")
                .GetProperty("quote")[0]
                .Clone();

            static decimal?[] ReadNullableDecimalArray(JsonElement parent, string propertyName)
            {
                if (!parent.TryGetProperty(propertyName, out var values) || values.ValueKind != JsonValueKind.Array)
                    return [];

                return values.EnumerateArray()
                    .Select(v => v.ValueKind == JsonValueKind.Null ? (decimal?)null : v.GetDecimal())
                    .ToArray();
            }

            var opens = ReadNullableDecimalArray(quote, "open");
            var highs = ReadNullableDecimalArray(quote, "high");
            var lows = ReadNullableDecimalArray(quote, "low");
            var closes = ReadNullableDecimalArray(quote, "close");

            return new { timestamps, opens, highs, lows, closes };
        }

        /// <summary>General cryptocurrency market news headlines (Finnhub category=crypto).</summary>
        public async Task<JsonElement?> GetCryptoNewsAsync()
        {
            var url = $"https://finnhub.io/api/v1/news?category=crypto&token={_apiKey}";
            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            return doc.RootElement.Clone();
        }

        /// <summary>Symbol search autocomplete from Finnhub.</summary>
        public async Task<JsonElement?> SearchSymbolsAsync(string query)
        {
            var url = $"https://finnhub.io/api/v1/search?q={Uri.EscapeDataString(query)}&token={_apiKey}";
            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            return doc.RootElement.Clone();
        }
    }
}
