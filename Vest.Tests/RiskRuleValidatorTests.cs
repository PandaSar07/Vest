using Vest.Services;
using Xunit;

namespace Vest.Tests;

public class RiskRuleValidatorTests
{
    [Fact]
    public void ValidateLong_RejectsStopLossAboveEntry()
    {
        var (ok, error, _) = RiskRuleValidator.ValidateLong(100m, 105m, null, null, null);
        Assert.False(ok);
        Assert.Contains("below entry", error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ValidateLong_RejectsTakeProfitBelowEntry()
    {
        var (ok, error, _) = RiskRuleValidator.ValidateLong(100m, null, null, 95m, null);
        Assert.False(ok);
        Assert.Contains("above entry", error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ValidateLong_ResolvesPercentages()
    {
        var (ok, _, levels) = RiskRuleValidator.ValidateLong(100m, null, 5m, null, 10m);
        Assert.True(ok);
        Assert.Equal(95m, levels.StopLossPrice);
        Assert.Equal(110m, levels.TakeProfitPrice);
    }

    [Theory]
    [InlineData(100, 95, 110, null)]
    [InlineData(94.99, 95, 0, TradeExitReason.StopLoss)]
    [InlineData(110.01, 0, 110, TradeExitReason.TakeProfit)]
    public void EvaluateTrigger_FiresOnBoundary(decimal live, decimal slArg, decimal tpArg, string? expected)
    {
        decimal? sl = slArg == 0 ? null : slArg;
        decimal? tp = tpArg == 0 ? null : tpArg;
        var result = RiskRuleValidator.EvaluateTrigger(live, sl, tp);
        Assert.Equal(expected, result);
    }
}
