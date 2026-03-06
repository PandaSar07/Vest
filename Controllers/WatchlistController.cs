using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Text.Json;

namespace Vest.Controllers;

/// <summary>
/// Page: GET /Watchlist   (served by default MVC route → this.Index())
/// API:  GET/POST/DELETE  /api/watchlist[/{symbol}]
/// </summary>
public class WatchlistController : Controller
{
    private readonly IHttpClientFactory _factory;
    private static readonly JsonSerializerOptions _json =
        new() { PropertyNameCaseInsensitive = true };

    public WatchlistController(IHttpClientFactory factory) => _factory = factory;

    private string? UserId => HttpContext.Session.GetString("UserId");
    private HttpClient Supabase => _factory.CreateClient("supabase");

    // ── MVC page ─────────────────────────────────────────────────────────────

    // GET /Watchlist  (default MVC route)
    public IActionResult Index() => View("~/Views/Watchlist/Index.cshtml");

    // ── REST API  (/api/watchlist) ────────────────────────────────────────────

    // GET /api/watchlist  → returns ["AAPL","MSFT",…]
    [HttpGet("/api/watchlist")]
    public async Task<IActionResult> Get()
    {
        var uid = UserId;
        if (uid == null) return Unauthorized();
        try
        {
            var resp = await Supabase.GetAsync(
                $"watchlists?user_id=eq.{Uri.EscapeDataString(uid)}&order=added_at.desc&select=symbol");
            resp.EnsureSuccessStatusCode();
            var rows = JsonSerializer.Deserialize<List<WlRow>>(
                await resp.Content.ReadAsStringAsync(), _json) ?? [];
            return Json(rows.Select(r => r.Symbol).ToList());
        }
        catch (Exception ex) { return StatusCode(500, new { error = ex.Message }); }
    }

    // POST /api/watchlist  body: { "symbol": "AAPL" }
    [HttpPost("/api/watchlist")]
    public async Task<IActionResult> Add([FromBody] WlRequest req)
    {
        var uid = UserId;
        if (uid == null) return Unauthorized();
        if (string.IsNullOrWhiteSpace(req?.Symbol)) return BadRequest();
        try
        {
            var payload = JsonSerializer.Serialize(
                new { user_id = uid, symbol = req.Symbol.ToUpper() });
            var msg = new HttpRequestMessage(HttpMethod.Post, "watchlists")
            {
                Headers  = { { "Prefer", "resolution=merge-duplicates" } },
                Content  = new StringContent(payload, Encoding.UTF8, "application/json")
            };
            var resp = await Supabase.SendAsync(msg);
            return resp.IsSuccessStatusCode ? Ok() : StatusCode(500, new { error = "Supabase error." });
        }
        catch (Exception ex) { return StatusCode(500, new { error = ex.Message }); }
    }

    // DELETE /api/watchlist/{symbol}
    [HttpDelete("/api/watchlist/{symbol}")]
    public async Task<IActionResult> Remove(string symbol)
    {
        var uid = UserId;
        if (uid == null) return Unauthorized();
        try
        {
            var resp = await Supabase.DeleteAsync(
                $"watchlists?user_id=eq.{Uri.EscapeDataString(uid)}&symbol=eq.{Uri.EscapeDataString(symbol.ToUpper())}");
            return resp.IsSuccessStatusCode ? Ok() : StatusCode(500, new { error = "Supabase error." });
        }
        catch (Exception ex) { return StatusCode(500, new { error = ex.Message }); }
    }
}

public class WlRow     { public string Symbol { get; set; } = ""; }
public class WlRequest { public string Symbol { get; set; } = ""; }
