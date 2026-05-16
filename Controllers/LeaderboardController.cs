using Microsoft.AspNetCore.Mvc;
using Vest.Services;

namespace Vest.Controllers;

public class LeaderboardController : Controller
{
    private readonly LeaderboardService _leaderboard;
    private readonly LeaderboardBuilder _builder;

    public LeaderboardController(LeaderboardService leaderboard, LeaderboardBuilder builder)
    {
        _leaderboard = leaderboard;
        _builder     = builder;
    }

    public IActionResult Index()
    {
        var page = _leaderboard.GetPage(1, 25);
        ViewBag.UpdatedAt = page.UpdatedAt;
        return View(page);
    }

    public async Task<IActionResult> Profile(string username)
    {
        if (!LeaderboardBuilder.IsValidUsername(username))
            return NotFound();

        var viewerId = HttpContext.Session.GetString("UserId");
        var rank = _leaderboard.GetSnapshot().Entries
            .FirstOrDefault(e => string.Equals(e.Username, username, StringComparison.OrdinalIgnoreCase))
            ?.Rank;

        var profile = await _builder.GetPublicProfileAsync(username.Trim(), viewerId, rank);
        if (profile == null) return NotFound();

        return View(profile);
    }
}
