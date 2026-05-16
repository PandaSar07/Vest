using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Vest.Services;

/// <summary>Reads and writes rows in the user_prefs table.</summary>
public class UserPrefsService
{
    public const string PublicProfileKey = "public_profile";

    private readonly HttpClient _http;
    private static readonly JsonSerializerOptions _json = new() { PropertyNameCaseInsensitive = true };

    public UserPrefsService(IHttpClientFactory factory)
    {
        _http = factory.CreateClient("supabase");
    }

    public async Task<string?> GetPrefAsync(string userId, string key)
    {
        var resp = await _http.GetAsync(
            $"user_prefs?user_id=eq.{Uri.EscapeDataString(userId)}&key=eq.{Uri.EscapeDataString(key)}&select=value");
        if (!resp.IsSuccessStatusCode) return null;
        using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
        var arr = doc.RootElement;
        if (arr.ValueKind == JsonValueKind.Array && arr.GetArrayLength() > 0)
            return arr[0].TryGetProperty("value", out var v) ? v.GetString() : null;
        return null;
    }

    public async Task SetPrefAsync(string userId, string key, string value)
    {
        var payload = JsonSerializer.Serialize(new { user_id = userId, key, value });
        var msg = new HttpRequestMessage(HttpMethod.Post, "user_prefs")
        {
            Headers = { { "Prefer", "resolution=merge-duplicates" } },
            Content = new StringContent(payload, Encoding.UTF8, "application/json")
        };
        var resp = await _http.SendAsync(msg);
        resp.EnsureSuccessStatusCode();
    }

    public async Task<bool> IsPublicProfileAsync(string userId)
    {
        var v = await GetPrefAsync(userId, PublicProfileKey);
        return string.Equals(v, "true", StringComparison.OrdinalIgnoreCase);
    }

    public async Task SetPublicProfileAsync(string userId, bool isPublic) =>
        await SetPrefAsync(userId, PublicProfileKey, isPublic ? "true" : "false");

    public async Task<List<string>> GetPublicUserIdsAsync()
    {
        var resp = await _http.GetAsync(
            $"user_prefs?key=eq.{PublicProfileKey}&value=eq.true&select=user_id");
        if (!resp.IsSuccessStatusCode) return [];
        var rows = JsonSerializer.Deserialize<List<PrefRow>>(
            await resp.Content.ReadAsStringAsync(), _json) ?? [];
        return rows.Select(r => r.UserId).Where(id => !string.IsNullOrWhiteSpace(id)).Distinct().ToList();
    }

    private class PrefRow
    {
        [JsonPropertyName("user_id")] public string UserId { get; set; } = "";
    }
}
