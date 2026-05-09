using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Text.Json;
using Vest.Services;

namespace Vest.Controllers;

[Route("push")]
public class PushController : Controller
{
    private readonly IHttpClientFactory _http;
    private readonly PushNotificationService _push;
    private readonly ILogger<PushController> _logger;

    public PushController(IHttpClientFactory http, PushNotificationService push, ILogger<PushController> logger)
    {
        _http   = http;
        _push   = push;
        _logger = logger;
    }

    /// <summary>Returns the VAPID public key so the browser can create a subscription.</summary>
    [HttpGet("public-key")]
    public IActionResult PublicKey() => Json(new { publicKey = _push.PublicKey });

    /// <summary>Saves a browser push subscription to Supabase.</summary>
    [HttpPost("subscribe")]
    public async Task<IActionResult> Subscribe([FromBody] PushSubscribeRequest req)
    {
        var userId = HttpContext.Session.GetString("UserId");
        if (userId == null) return Unauthorized();

        var http    = _http.CreateClient("supabase");
        var payload = JsonSerializer.Serialize(new
        {
            user_id  = userId,
            endpoint = req.Endpoint,
            p256dh   = req.P256dh,
            auth     = req.Auth
        });

        // Upsert so duplicate subscriptions are not created
        var msg = new HttpRequestMessage(HttpMethod.Post, "push_subscriptions")
        {
            Headers = { { "Prefer", "resolution=merge-duplicates" } },
            Content = new StringContent(payload, Encoding.UTF8, "application/json")
        };
        var resp = await http.SendAsync(msg);
        if (!resp.IsSuccessStatusCode)
        {
            _logger.LogWarning("Failed to save push subscription: {Status}", resp.StatusCode);
            return StatusCode(500, "Could not save subscription");
        }

        _logger.LogInformation("Push subscription saved for user {UserId}", userId);
        return Ok();
    }

    /// <summary>Removes a browser push subscription.</summary>
    [HttpDelete("unsubscribe")]
    public async Task<IActionResult> Unsubscribe([FromBody] PushSubscribeRequest req)
    {
        var userId = HttpContext.Session.GetString("UserId");
        if (userId == null) return Unauthorized();

        var http = _http.CreateClient("supabase");
        await http.DeleteAsync(
            $"push_subscriptions?user_id=eq.{Uri.EscapeDataString(userId)}&endpoint=eq.{Uri.EscapeDataString(req.Endpoint)}");

        return Ok();
    }

    /// <summary>Persists a notification preference (key/value) to the user_prefs table.</summary>
    [HttpPost("pref")]
    public async Task<IActionResult> SetPref([FromBody] PrefRequest req)
    {
        var userId = HttpContext.Session.GetString("UserId");
        if (userId == null) return Unauthorized();

        var http    = _http.CreateClient("supabase");
        var payload = JsonSerializer.Serialize(new { user_id = userId, key = req.Key, value = req.Value });
        var msg     = new HttpRequestMessage(HttpMethod.Post, "user_prefs")
        {
            Headers = { { "Prefer", "resolution=merge-duplicates" } },
            Content = new StringContent(payload, Encoding.UTF8, "application/json")
        };
        await http.SendAsync(msg);
        return Ok();
    }
}

public class PushSubscribeRequest
{
    public string Endpoint { get; set; } = "";
    public string P256dh   { get; set; } = "";
    public string Auth     { get; set; } = "";
}

public class PrefRequest
{
    public string Key   { get; set; } = "";
    public string Value { get; set; } = "";
}
