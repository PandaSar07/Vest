using System.Text.Json;
using System.Text.Json.Serialization;

namespace Vest.Services;

/// <summary>Builds leaderboard rows from Supabase and live portfolio marks.</summary>
public class LeaderboardBuilder
{
    private readonly HttpClient _http;
    private readonly PortfolioService _portfolio;
    private readonly PortfolioValuationService _valuation;
    private readonly UserPrefsService _prefs;
    private static readonly JsonSerializerOptions _json = new() { PropertyNameCaseInsensitive = true };

    public LeaderboardBuilder(
        IHttpClientFactory factory,
        PortfolioService portfolio,
        PortfolioValuationService valuation,
        UserPrefsService prefs)
    {
        _http      = factory.CreateClient("supabase");
        _portfolio = portfolio;
        _valuation = valuation;
        _prefs     = prefs;
    }

    public async Task<LeaderboardSnapshot> BuildAsync(CancellationToken ct = default)
    {
        var publicIds = await _prefs.GetPublicUserIdsAsync();
        if (publicIds.Count == 0)
            return new LeaderboardSnapshot(DateTime.UtcNow, []);

        var users = await GetUsersByIdsAsync(publicIds);
        var rows  = new List<(UserPublicRow User, decimal Total, decimal ReturnPct, int Trades)>();

        foreach (var user in users)
        {
            if (ct.IsCancellationRequested) break;
            try
            {
                var val        = await _valuation.ComputeAsync(user.Id);
                var tradeCount = await CountTradesAsync(user.Id);
                await _portfolio.TakeSnapshotAsync(user.Id, val.TotalValue);
                rows.Add((user, val.TotalValue, val.ReturnPct, tradeCount));
            }
            catch
            {
                /* skip users that fail valuation */
            }
        }

        var ordered = rows
            .OrderByDescending(r => r.Total)
            .ThenBy(r => r.User.Username, StringComparer.OrdinalIgnoreCase)
            .Select((r, i) => new LeaderboardEntry(
                i + 1,
                r.User.Id,
                r.User.Username,
                string.IsNullOrWhiteSpace(r.User.FullName) ? null : r.User.FullName.Trim(),
                r.Total,
                r.ReturnPct,
                r.Trades))
            .ToList();

        return new LeaderboardSnapshot(DateTime.UtcNow, ordered);
    }

    public async Task<UserPublicRow?> GetUserByUsernameAsync(string username)
    {
        if (!IsValidUsername(username)) return null;
        var resp = await _http.GetAsync(
            $"users?username=eq.{Uri.EscapeDataString(username)}&select=id,username,full_name");
        if (!resp.IsSuccessStatusCode) return null;
        var rows = JsonSerializer.Deserialize<List<UserPublicRow>>(
            await resp.Content.ReadAsStringAsync(), _json);
        return rows?.FirstOrDefault();
    }

    public async Task<PublicProfileDto?> GetPublicProfileAsync(
        string username,
        string? viewerUserId,
        int? leaderboardRank)
    {
        var user = await GetUserByUsernameAsync(username);
        if (user == null) return null;

        var isOwner = viewerUserId != null &&
            string.Equals(viewerUserId, user.Id, StringComparison.Ordinal);
        var isPublic = await _prefs.IsPublicProfileAsync(user.Id);
        if (!isPublic && !isOwner) return null;

        var val    = await _valuation.ComputeAsync(user.Id);
        var trades = await _portfolio.GetTradesAsync(user.Id, 50);
        var tradeCount = trades.Count < 50
            ? trades.Count
            : await CountTradesAsync(user.Id);

        return new PublicProfileDto(
            user.Username,
            string.IsNullOrWhiteSpace(user.FullName) ? null : user.FullName.Trim(),
            isPublic,
            isOwner,
            val.Cash,
            val.StockValue,
            val.TotalValue,
            val.ReturnPct,
            val.HoldingCount,
            tradeCount,
            leaderboardRank,
            trades.Select(t => new PublicTradeDto(
                t.Symbol, t.Action, t.Shares, t.Price, t.Total, t.TradedAt)).ToList());
    }

    private async Task<List<UserPublicRow>> GetUsersByIdsAsync(IEnumerable<string> ids)
    {
        var idList = ids.Distinct().ToList();
        if (idList.Count == 0) return [];

        var filter = string.Join(",", idList.Select(Uri.EscapeDataString));
        var resp = await _http.GetAsync($"users?id=in.({filter})&select=id,username,full_name");
        if (!resp.IsSuccessStatusCode) return [];
        return JsonSerializer.Deserialize<List<UserPublicRow>>(
            await resp.Content.ReadAsStringAsync(), _json) ?? [];
    }

    private async Task<int> CountTradesAsync(string userId)
    {
        var resp = await _http.GetAsync(
            $"trades?user_id=eq.{Uri.EscapeDataString(userId)}&select=id",
            HttpCompletionOption.ResponseHeadersRead);
        if (!resp.IsSuccessStatusCode) return 0;
        using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
        return doc.RootElement.ValueKind == JsonValueKind.Array ? doc.RootElement.GetArrayLength() : 0;
    }

    public static bool IsValidUsername(string username) =>
        !string.IsNullOrWhiteSpace(username) &&
        username.Length is >= 2 and <= 32 &&
        username.All(c => char.IsLetterOrDigit(c) || c == '_');
}

public class UserPublicRow
{
    [JsonPropertyName("id")] public string Id { get; set; } = "";
    [JsonPropertyName("username")] public string Username { get; set; } = "";
    [JsonPropertyName("full_name")] public string? FullName { get; set; }
}

public record PublicProfileDto(
    string Username,
    string? DisplayName,
    bool IsPublic,
    bool IsOwner,
    decimal Cash,
    decimal StockValue,
    decimal TotalValue,
    decimal ReturnPct,
    int HoldingCount,
    int TradeCount,
    int? LeaderboardRank,
    IReadOnlyList<PublicTradeDto> RecentTrades);

public record PublicTradeDto(
    string Symbol,
    string Action,
    decimal Shares,
    decimal Price,
    decimal Total,
    DateTime TradedAt);
