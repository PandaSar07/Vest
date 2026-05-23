namespace Vest.Services;

public record LeaderboardEntry(
    int Rank,
    string UserId,
    string Username,
    string? DisplayName,
    string? AvatarUrl,
    decimal TotalValue,
    decimal ReturnPct,
    int TradeCount);

public sealed class LeaderboardSnapshot
{
    public static readonly LeaderboardSnapshot Empty = new(DateTime.MinValue, []);

    public LeaderboardSnapshot(DateTime updatedAt, IReadOnlyList<LeaderboardEntry> entries)
    {
        UpdatedAt = updatedAt;
        Entries   = entries;
    }

    public DateTime UpdatedAt { get; }
    public IReadOnlyList<LeaderboardEntry> Entries { get; }
}

/// <summary>In-memory leaderboard cache refreshed by <see cref="LeaderboardRefreshHostedService"/>.</summary>
public class LeaderboardService
{
    private readonly object _lock = new();
    private LeaderboardSnapshot _snapshot = LeaderboardSnapshot.Empty;

    public LeaderboardSnapshot GetSnapshot()
    {
        lock (_lock) return _snapshot;
    }

    public void UpdateSnapshot(LeaderboardSnapshot snapshot)
    {
        lock (_lock) _snapshot = snapshot;
    }

    public LeaderboardPage GetPage(int page, int pageSize, string? search = null)
    {
        var snap = GetSnapshot();
        
        var query = snap.Entries.AsEnumerable();
        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(e => 
                (e.Username != null && e.Username.Contains(search, StringComparison.OrdinalIgnoreCase)) ||
                (e.DisplayName != null && e.DisplayName.Contains(search, StringComparison.OrdinalIgnoreCase)));
        }

        var total = query.Count();
        var safePage = Math.Max(1, page);
        var safeSize = Math.Clamp(pageSize, 1, 50);
        var items = query
            .Skip((safePage - 1) * safeSize)
            .Take(safeSize)
            .ToList();
        return new LeaderboardPage(snap.UpdatedAt, items, total, safePage, safeSize);
    }

    public LeaderboardEntry? GetEntryForUser(string userId) =>
        GetSnapshot().Entries.FirstOrDefault(e =>
            string.Equals(e.UserId, userId, StringComparison.Ordinal));

    public int? GetRankForUser(string userId) =>
        GetEntryForUser(userId)?.Rank;
}

public record LeaderboardPage(
    DateTime UpdatedAt,
    IReadOnlyList<LeaderboardEntry> Entries,
    int TotalCount,
    int Page,
    int PageSize);
