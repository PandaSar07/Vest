namespace Vest.Services;

/// <summary>
/// Maps portfolio symbols to Finnhub quote symbols. Crypto spot in USD is stored as <c>BTC-USD</c>
/// in Supabase; Finnhub expects <c>BINANCE:BTCUSDT</c> (or passthrough if already set).
/// </summary>
public static class QuoteSymbolResolver
{
    public static string ForFinnhubQuote(string symbol)
    {
        var s = symbol.Trim().ToUpperInvariant();
        if (string.IsNullOrEmpty(s)) return s;
        if (s.StartsWith("BINANCE:", StringComparison.Ordinal)) return s;
        if (s.EndsWith("-USD", StringComparison.Ordinal) && s.Length > 4)
        {
            var baseAsset = s[..^4];
            return $"BINANCE:{baseAsset}USDT";
        }

        return s;
    }

    public static bool IsUsdCryptoSpot(string symbol)
    {
        var s = symbol.Trim();
        return s.EndsWith("-USD", StringComparison.OrdinalIgnoreCase) && s.Length > 4;
    }
}
