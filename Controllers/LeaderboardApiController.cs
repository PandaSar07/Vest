using Microsoft.AspNetCore.Mvc;
using Vest.Services;

namespace Vest.Controllers;

[ApiController]
[Route("api/leaderboard")]
public class LeaderboardApiController : ControllerBase
{
    private readonly LeaderboardService _leaderboard;
    private readonly LeaderboardBuilder _builder;
    private readonly UserPrefsService _prefs;
    private readonly IServiceScopeFactory _scopeFactory;

    public LeaderboardApiController(
        LeaderboardService leaderboard,
        LeaderboardBuilder builder,
        UserPrefsService prefs,
        IServiceScopeFactory scopeFactory)
    {
        _leaderboard   = leaderboard;
        _builder       = builder;
        _prefs         = prefs;
        _scopeFactory  = scopeFactory;
    }

    private string? CurrentUserId => HttpContext.Session.GetString("UserId");

    /// <summary>Paginated global leaderboard (public profiles only).</summary>
    [HttpGet]
    public IActionResult List([FromQuery] int page = 1, [FromQuery] int pageSize = 25, [FromQuery] string? search = null)
    {
        var result = _leaderboard.GetPage(page, pageSize, search);
        return Ok(new
        {
            updatedAt = result.UpdatedAt,
            page = result.Page,
            pageSize = result.PageSize,
            totalCount = result.TotalCount,
            hasMore = result.Page * result.PageSize < result.TotalCount,
            entries = result.Entries.Select(e => new
            {
                e.Rank,
                e.Username,
                displayName = e.DisplayName,
                avatarUrl = e.AvatarUrl,
                e.TotalValue,
                e.ReturnPct,
                e.TradeCount,
                profileUrl = Url.Action("Profile", "Leaderboard", new { username = e.Username })
            })
        });
    }

    /// <summary>Current user's rank and privacy setting (requires login).</summary>
    [HttpGet("me")]
    public async Task<IActionResult> Me()
    {
        var userId = CurrentUserId;
        if (userId == null) return Unauthorized(new { error = "Not logged in." });

        var entry    = _leaderboard.GetEntryForUser(userId);
        var isPublic = await _prefs.IsPublicProfileAsync(userId);
        var snap     = _leaderboard.GetSnapshot();

        return Ok(new
        {
            isPublic,
            rank = entry?.Rank,
            totalValue = entry?.TotalValue,
            returnPct = entry?.ReturnPct,
            tradeCount = entry?.TradeCount,
            leaderboardUpdatedAt = snap.UpdatedAt,
            onLeaderboard = entry != null
        });
    }

    [HttpGet("privacy")]
    public async Task<IActionResult> GetPrivacy()
    {
        var userId = CurrentUserId;
        if (userId == null) return Unauthorized(new { error = "Not logged in." });
        var isPublic = await _prefs.IsPublicProfileAsync(userId);
        return Ok(new { isPublic });
    }

    [HttpPost("privacy")]
    public async Task<IActionResult> SetPrivacy([FromBody] PrivacyRequest req)
    {
        var userId = CurrentUserId;
        if (userId == null) return Unauthorized(new { error = "Not logged in." });
        await _prefs.SetPublicProfileAsync(userId, req.IsPublic);
        _ = TriggerLeaderboardRefreshAsync();
        return Ok(new { isPublic = req.IsPublic });
    }

    private async Task TriggerLeaderboardRefreshAsync()
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var builder  = scope.ServiceProvider.GetRequiredService<LeaderboardBuilder>();
            var cache    = scope.ServiceProvider.GetRequiredService<LeaderboardService>();
            cache.UpdateSnapshot(await builder.BuildAsync());
        }
        catch { /* best-effort refresh after privacy change */ }
    }

    /// <summary>Public profile JSON (only if profile is public, or viewer is owner).</summary>
    [HttpGet("profile/{username}")]
    public async Task<IActionResult> Profile(string username)
    {
        if (!LeaderboardBuilder.IsValidUsername(username))
            return BadRequest(new { error = "Invalid username." });

        var rank = _leaderboard.GetSnapshot().Entries
            .FirstOrDefault(e => string.Equals(e.Username, username, StringComparison.OrdinalIgnoreCase))
            ?.Rank;

        var profile = await _builder.GetPublicProfileAsync(
            username.Trim(),
            CurrentUserId,
            rank);

        if (profile == null)
            return NotFound(new { error = "Profile not found or is private." });

        return Ok(profile);
    }
}

public class PrivacyRequest
{
    public bool IsPublic { get; set; }
}
