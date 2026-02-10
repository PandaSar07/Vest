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

        public async Task<decimal?> GetQuoteAsync(string symbol)
        {
            var url = $"https://finnhub.io/api/v1/quote?symbol={symbol}&token={_apiKey}";

            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();

            using var doc = JsonDocument.Parse(json);

            // "c" = current price
            if (doc.RootElement.TryGetProperty("c", out var price))
            {
                return price.GetDecimal();
            }

            return null;
        }
    }
}
