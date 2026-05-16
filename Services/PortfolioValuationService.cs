using System.Text.Json;

namespace Vest.Services;

/// <summary>Computes live paper-portfolio totals (cash + marked holdings).</summary>
public class PortfolioValuationService
{
    public const decimal StartingCash = 100_000m;

    private readonly PortfolioService _portfolio;
    private readonly FinnhubService _finnhub;

    public PortfolioValuationService(PortfolioService portfolio, FinnhubService finnhub)
    {
        _portfolio = portfolio;
        _finnhub   = finnhub;
    }

    private static bool IsLegacySyntheticOptionSymbol(string symbol) =>
        symbol.Contains("|OPT|", StringComparison.Ordinal);

    public async Task<PortfolioValuation> ComputeAsync(string userId)
    {
        var portfolio = await _portfolio.GetOrCreatePortfolioAsync(userId);
        var holdings  = await _portfolio.GetHoldingsAsync(userId);
        var equityRows = holdings.Where(h => !IsLegacySyntheticOptionSymbol(h.Symbol)).ToList();

        var priceMap = new Dictionary<string, decimal>();
        await Task.WhenAll(equityRows.Select(async h =>
        {
            try
            {
                var quoteSym = QuoteSymbolResolver.ForFinnhubQuote(h.Symbol);
                var q = await _finnhub.GetFullQuoteAsync(quoteSym);
                if (q.HasValue && q.Value.TryGetProperty("c", out var c) && c.GetDecimal() != 0)
                    priceMap[h.Symbol] = c.GetDecimal();
            }
            catch { /* use avg cost fallback */ }
        }));

        decimal stockValue = 0;
        foreach (var h in equityRows)
        {
            var livePrice = priceMap.GetValueOrDefault(h.Symbol, h.AvgCost);
            stockValue += Math.Round(h.Shares * livePrice, 2);
        }

        var totalValue = portfolio.Cash + stockValue;
        var returnPct  = StartingCash == 0
            ? 0
            : Math.Round((totalValue - StartingCash) / StartingCash * 100, 2);

        return new PortfolioValuation(portfolio.Cash, stockValue, totalValue, returnPct, equityRows.Count);
    }
}

public record PortfolioValuation(
    decimal Cash,
    decimal StockValue,
    decimal TotalValue,
    decimal ReturnPct,
    int HoldingCount);
