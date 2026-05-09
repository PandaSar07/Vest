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

    // ── Portfolio snapshots ────────────────────────────────────────────────

    public async Task TakeSnapshotAsync(string userId, decimal totalValue)
    {
        // UNIQUE(user_id, snapped_at) handles idempotency — just upsert
        var payload = JsonSerializer.Serialize(new { user_id = userId, value = totalValue });
        var msg = new HttpRequestMessage(HttpMethod.Post, "portfolio_snapshots")
        {
            Headers = { { "Prefer", "resolution=merge-duplicates" } },
            Content = new StringContent(payload, Encoding.UTF8, "application/json")
        };
        await _http.SendAsync(msg);
    }

    public async Task<List<SnapshotRow>> GetSnapshotsAsync(string userId, int days = 30)
    {
        var since = DateTime.UtcNow.AddDays(-days).ToString("yyyy-MM-dd");
        var resp = await _http.GetAsync(
            $"portfolio_snapshots?user_id=eq.{Uri.EscapeDataString(userId)}&snapped_at=gte.{since}&order=snapped_at.asc&select=*");
        resp.EnsureSuccessStatusCode();
        return JsonSerializer.Deserialize<List<SnapshotRow>>(
            await resp.Content.ReadAsStringAsync(), _json) ?? [];
    }

    // ── Limit orders ───────────────────────────────────────────────────────

    public async Task<LimitOrderRow> PlaceLimitOrderAsync(
        string userId, string symbol, string action, decimal shares, decimal limitPrice)
    {
        var payload = JsonSerializer.Serialize(new
        {
            user_id = userId, symbol, action, shares, limit_price = limitPrice
        });
        var msg = new HttpRequestMessage(HttpMethod.Post, "limit_orders")
        {
            Headers = { { "Prefer", "return=representation" } },
            Content = new StringContent(payload, Encoding.UTF8, "application/json")
        };
        var resp = await _http.SendAsync(msg);
        resp.EnsureSuccessStatusCode();
        var rows = JsonSerializer.Deserialize<List<LimitOrderRow>>(
            await resp.Content.ReadAsStringAsync(), _json);
        return rows![0];
    }

    public async Task<List<LimitOrderRow>> GetPendingOrdersAsync(string userId)
    {
        var resp = await _http.GetAsync(
            $"limit_orders?user_id=eq.{Uri.EscapeDataString(userId)}&status=eq.PENDING&order=created_at.desc&select=*");
        resp.EnsureSuccessStatusCode();
        return JsonSerializer.Deserialize<List<LimitOrderRow>>(
            await resp.Content.ReadAsStringAsync(), _json) ?? [];
    }

    /// <summary>Used by the background executor — fetches ALL PENDING orders across users.</summary>
    public async Task<List<LimitOrderRow>> GetAllPendingOrdersAsync()
    {
        var resp = await _http.GetAsync("limit_orders?status=eq.PENDING&select=*");
        resp.EnsureSuccessStatusCode();
        return JsonSerializer.Deserialize<List<LimitOrderRow>>(
            await resp.Content.ReadAsStringAsync(), _json) ?? [];
    }

    public async Task<bool> CancelOrderAsync(string userId, long orderId)
    {
        var resp = await _http.PatchAsync(
            $"limit_orders?id=eq.{orderId}&user_id=eq.{Uri.EscapeDataString(userId)}&status=eq.PENDING",
            Json(new { status = "CANCELLED" }));
        return resp.IsSuccessStatusCode;
    }

    /// <summary>Marks order FILLED and executes the buy/sell trade.</summary>
    public async Task FillOrderAsync(LimitOrderRow order)
    {
        if (order.Action == "BUY")
            await ExecuteBuyAsync(order.UserId, order.Symbol, order.Shares, order.LimitPrice);
        else
            await ExecuteSellAsync(order.UserId, order.Symbol, order.Shares, order.LimitPrice);

        await _http.PatchAsync(
            $"limit_orders?id=eq.{order.Id}",
            Json(new { status = "FILLED", filled_at = DateTime.UtcNow.ToString("o") }));
    }



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

    // ── User helpers ──────────────────────────────────────────────────────────

    /// <summary>Fetches the user's email from the public users table.</summary>
    public async Task<string?> GetUserEmailAsync(string userId)
    {
        var resp = await _http.GetAsync(
            $"users?id=eq.{Uri.EscapeDataString(userId)}&select=email");
        if (!resp.IsSuccessStatusCode) return null;
        var json = await resp.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var arr = doc.RootElement;
        if (arr.ValueKind == JsonValueKind.Array && arr.GetArrayLength() > 0)
            return arr[0].TryGetProperty("email", out var e) ? e.GetString() : null;
        return null;
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

public class SnapshotRow
{
    [JsonPropertyName("id")]          public long    Id       { get; set; }
    [JsonPropertyName("user_id")]     public string  UserId   { get; set; } = "";
    [JsonPropertyName("value")]       public decimal Value    { get; set; }
    [JsonPropertyName("snapped_at")] public string  SnappedAt { get; set; } = "";
}

public class LimitOrderRow
{
    [JsonPropertyName("id")]          public long    Id         { get; set; }
    [JsonPropertyName("user_id")]     public string  UserId     { get; set; } = "";
    [JsonPropertyName("symbol")]      public string  Symbol     { get; set; } = "";
    [JsonPropertyName("action")]      public string  Action     { get; set; } = "";
    [JsonPropertyName("shares")]      public decimal Shares     { get; set; }
    [JsonPropertyName("limit_price")] public decimal LimitPrice { get; set; }
    [JsonPropertyName("status")]      public string  Status     { get; set; } = "";
    [JsonPropertyName("created_at")]  public DateTime CreatedAt { get; set; }
    [JsonPropertyName("filled_at")]   public DateTime? FilledAt { get; set; }
}
