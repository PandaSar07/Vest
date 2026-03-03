using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Vest.Services;

/// <summary>
/// Wraps all Supabase REST calls for the paper-trading portfolio system.
/// Tables: portfolios, holdings, trades
/// </summary>
public class PortfolioService
{
    private readonly HttpClient _http;
    private static readonly JsonSerializerOptions _json = new() { PropertyNameCaseInsensitive = true };

    public PortfolioService(IHttpClientFactory factory)
    {
        _http = factory.CreateClient("supabase");
    }

    // ── Portfolio (cash balance) ────────────────────────────────────────────

    public async Task<PortfolioRow> GetOrCreatePortfolioAsync(string userId)
    {
        var resp = await _http.GetAsync($"portfolios?user_id=eq.{Uri.EscapeDataString(userId)}&select=*");
        resp.EnsureSuccessStatusCode();
        var rows = JsonSerializer.Deserialize<List<PortfolioRow>>(
            await resp.Content.ReadAsStringAsync(), _json) ?? [];

        if (rows.Count > 0) return rows[0];

        // First time — insert a fresh $100 000 balance
        var payload = JsonSerializer.Serialize(new { user_id = userId, cash = 100000.00m });
        var post = await _http.PostAsync("portfolios",
            new StringContent(payload, Encoding.UTF8, "application/json"));
        post.EnsureSuccessStatusCode();

        return new PortfolioRow { UserId = userId, Cash = 100000m };
    }

    // ── Holdings ───────────────────────────────────────────────────────────

    public async Task<List<HoldingRow>> GetHoldingsAsync(string userId)
    {
        var resp = await _http.GetAsync(
            $"holdings?user_id=eq.{Uri.EscapeDataString(userId)}&shares=gt.0&select=*");
        resp.EnsureSuccessStatusCode();
        return JsonSerializer.Deserialize<List<HoldingRow>>(
            await resp.Content.ReadAsStringAsync(), _json) ?? [];
    }

    public async Task<HoldingRow?> GetHoldingAsync(string userId, string symbol)
    {
        var resp = await _http.GetAsync(
            $"holdings?user_id=eq.{Uri.EscapeDataString(userId)}&symbol=eq.{Uri.EscapeDataString(symbol)}&select=*");
        resp.EnsureSuccessStatusCode();
        var rows = JsonSerializer.Deserialize<List<HoldingRow>>(
            await resp.Content.ReadAsStringAsync(), _json);
        return rows?.FirstOrDefault();
    }

    // ── Trades history ─────────────────────────────────────────────────────

    public async Task<List<TradeRow>> GetTradesAsync(string userId, int limit = 20)
    {
        var resp = await _http.GetAsync(
            $"trades?user_id=eq.{Uri.EscapeDataString(userId)}&order=traded_at.desc&limit={limit}&select=*");
        resp.EnsureSuccessStatusCode();
        return JsonSerializer.Deserialize<List<TradeRow>>(
            await resp.Content.ReadAsStringAsync(), _json) ?? [];
    }

    // ── Execute BUY ──────────────────────────────────────────────────────────

    public async Task<(bool ok, string error)> ExecuteBuyAsync(
        string userId, string symbol, decimal shares, decimal price)
    {
        var total = Math.Round(shares * price, 2);
        var portfolio = await GetOrCreatePortfolioAsync(userId);

        if (portfolio.Cash < total)
            return (false, $"Insufficient cash. You have ${portfolio.Cash:N2} but need ${total:N2}.");

        // 1. Deduct cash
        var newCash = portfolio.Cash - total;
        var patchCash = await _http.PatchAsync(
            $"portfolios?user_id=eq.{Uri.EscapeDataString(userId)}",
            Json(new { cash = newCash }));
        if (!patchCash.IsSuccessStatusCode) return (false, "Failed to update cash balance.");

        // 2. Upsert holding — recalculate average cost
        var existing = await GetHoldingAsync(userId, symbol);
        if (existing == null)
        {
            var ins = await _http.PostAsync("holdings",
                Json(new { user_id = userId, symbol, shares, avg_cost = price }));
            if (!ins.IsSuccessStatusCode) return (false, "Failed to create holding.");
        }
        else
        {
            var newShares  = existing.Shares + shares;
            var newAvgCost = Math.Round((existing.Shares * existing.AvgCost + shares * price) / newShares, 4);
            var upd = await _http.PatchAsync(
                $"holdings?user_id=eq.{Uri.EscapeDataString(userId)}&symbol=eq.{Uri.EscapeDataString(symbol)}",
                Json(new { shares = newShares, avg_cost = newAvgCost }));
            if (!upd.IsSuccessStatusCode) return (false, "Failed to update holding.");
        }

        // 3. Record trade
        await _http.PostAsync("trades",
            Json(new { user_id = userId, symbol, action = "BUY", shares, price, total }));

        return (true, string.Empty);
    }

    // ── Execute SELL ─────────────────────────────────────────────────────────

    public async Task<(bool ok, string error)> ExecuteSellAsync(
        string userId, string symbol, decimal shares, decimal price)
    {
        var holding = await GetHoldingAsync(userId, symbol);
        if (holding == null || holding.Shares < shares)
            return (false, $"You only own {holding?.Shares ?? 0:N4} shares of {symbol}.");

        var total    = Math.Round(shares * price, 2);
        var newShares = holding.Shares - shares;
        var portfolio = await GetOrCreatePortfolioAsync(userId);

        // 1. Update/zero holding
        var updH = await _http.PatchAsync(
            $"holdings?user_id=eq.{Uri.EscapeDataString(userId)}&symbol=eq.{Uri.EscapeDataString(symbol)}",
            Json(new { shares = newShares }));
        if (!updH.IsSuccessStatusCode) return (false, "Failed to update holding.");

        // 2. Add cash back
        var newCash = portfolio.Cash + total;
        var updC = await _http.PatchAsync(
            $"portfolios?user_id=eq.{Uri.EscapeDataString(userId)}",
            Json(new { cash = newCash }));
        if (!updC.IsSuccessStatusCode) return (false, "Failed to update cash balance.");

        // 3. Record trade
        await _http.PostAsync("trades",
            Json(new { user_id = userId, symbol, action = "SELL", shares, price, total }));

        return (true, string.Empty);
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    private static StringContent Json(object obj) =>
        new(JsonSerializer.Serialize(obj), Encoding.UTF8, "application/json");
}

// ── Row DTOs ──────────────────────────────────────────────────────────────

public class PortfolioRow
{
    [JsonPropertyName("user_id")]  public string  UserId    { get; set; } = "";
    [JsonPropertyName("cash")]     public decimal Cash      { get; set; }
    [JsonPropertyName("created_at")] public DateTime? CreatedAt { get; set; }
}

public class HoldingRow
{
    [JsonPropertyName("id")]       public long    Id        { get; set; }
    [JsonPropertyName("user_id")]  public string  UserId    { get; set; } = "";
    [JsonPropertyName("symbol")]   public string  Symbol    { get; set; } = "";
    [JsonPropertyName("shares")]   public decimal Shares    { get; set; }
    [JsonPropertyName("avg_cost")] public decimal AvgCost   { get; set; }
}

public class TradeRow
{
    [JsonPropertyName("id")]         public long     Id        { get; set; }
    [JsonPropertyName("user_id")]    public string   UserId    { get; set; } = "";
    [JsonPropertyName("symbol")]     public string   Symbol    { get; set; } = "";
    [JsonPropertyName("action")]     public string   Action    { get; set; } = "";
    [JsonPropertyName("shares")]     public decimal  Shares    { get; set; }
    [JsonPropertyName("price")]      public decimal  Price     { get; set; }
    [JsonPropertyName("total")]      public decimal  Total     { get; set; }
    [JsonPropertyName("traded_at")]  public DateTime TradedAt  { get; set; }
}
