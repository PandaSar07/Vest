using WebPush;
using System.Text.Json;

namespace Vest.Services;

/// <summary>
/// Sends Web Push notifications to subscribed browsers using VAPID authentication.
/// </summary>
public class PushNotificationService
{
    private readonly WebPushClient _client;
    private readonly VapidDetails _vapid;
    private readonly ILogger<PushNotificationService> _logger;

    public PushNotificationService(IConfiguration config, ILogger<PushNotificationService> logger)
    {
        _logger = logger;
        _vapid  = new VapidDetails(
            config["Vapid:Subject"]!,
            config["Vapid:PublicKey"]!,
            config["Vapid:PrivateKey"]!);
        _client = new WebPushClient();
    }

    /// <summary>Sends a push notification to one subscription. Returns false if the subscription is expired/gone.</summary>
    public async Task<bool> SendAsync(string endpoint, string p256dh, string auth, string title, string body, string? url = null)
    {
        var subscription = new PushSubscription(endpoint, p256dh, auth);
        var payload = JsonSerializer.Serialize(new { title, body, url = url ?? "/" });

        try
        {
            await _client.SendNotificationAsync(subscription, payload, _vapid);
            return true;
        }
        catch (WebPushException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Gone
                                        || ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            _logger.LogInformation("Push subscription expired for endpoint {Endpoint}", endpoint[..Math.Min(40, endpoint.Length)]);
            return false; // caller should remove this subscription
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send push notification");
            return false;
        }
    }

    public string PublicKey => _vapid.PublicKey;
}
