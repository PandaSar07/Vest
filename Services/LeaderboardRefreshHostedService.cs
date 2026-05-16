using Microsoft.Extensions.DependencyInjection;

namespace Vest.Services;

/// <summary>Refreshes the global leaderboard cache on a fixed interval.</summary>
public class LeaderboardRefreshHostedService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly LeaderboardService _leaderboard;
    private readonly ILogger<LeaderboardRefreshHostedService> _logger;
    private readonly TimeSpan _interval;

    public LeaderboardRefreshHostedService(
        IServiceScopeFactory scopeFactory,
        LeaderboardService leaderboard,
        IConfiguration config,
        ILogger<LeaderboardRefreshHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _leaderboard  = leaderboard;
        _logger       = logger;
        var minutes = config.GetValue("Leaderboard:RefreshIntervalMinutes", 5);
        _interval = TimeSpan.FromMinutes(Math.Clamp(minutes, 1, 60));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(TimeSpan.FromSeconds(8), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try { await RefreshOnceAsync(stoppingToken); }
            catch (Exception ex) { _logger.LogWarning(ex, "Leaderboard refresh failed"); }
            await Task.Delay(_interval, stoppingToken);
        }
    }

    private async Task RefreshOnceAsync(CancellationToken ct)
    {
        using var scope   = _scopeFactory.CreateScope();
        var builder       = scope.ServiceProvider.GetRequiredService<LeaderboardBuilder>();
        var snapshot      = await builder.BuildAsync(ct);
        _leaderboard.UpdateSnapshot(snapshot);
        _logger.LogInformation(
            "Leaderboard refreshed: {Count} public traders at {At:u}",
            snapshot.Entries.Count,
            snapshot.UpdatedAt);
    }
}
