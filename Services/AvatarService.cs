using System.Text;
using System.Text.Json;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

namespace Vest.Services;

public class AvatarService
{
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".webp"
    };

    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg", "image/png", "image/webp"
    };

    private readonly IWebHostEnvironment _env;
    private readonly HttpClient _http;
    private readonly IConfiguration _config;
    private readonly ILogger<AvatarService> _logger;

    public AvatarService(
        IWebHostEnvironment env,
        IHttpClientFactory httpFactory,
        IConfiguration config,
        ILogger<AvatarService> logger)
    {
        _env    = env;
        _http   = httpFactory.CreateClient("supabase");
        _config = config;
        _logger = logger;
    }

    public int TargetSize => _config.GetValue("Avatar:Size", 256);
    public long MaxBytes  => _config.GetValue("Avatar:MaxBytes", 5 * 1024 * 1024L);

    public async Task<string?> GetAvatarUrlAsync(string userId)
    {
        var resp = await _http.GetAsync(
            $"users?id=eq.{Uri.EscapeDataString(userId)}&select=avatar_url");
        if (!resp.IsSuccessStatusCode) return null;
        using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
        if (doc.RootElement.ValueKind != JsonValueKind.Array || doc.RootElement.GetArrayLength() == 0)
            return null;
        var row = doc.RootElement[0];
        return row.TryGetProperty("avatar_url", out var el) && el.ValueKind == JsonValueKind.String
            ? el.GetString()
            : null;
    }

    public async Task<(bool ok, string error, string? avatarUrl)> UploadAsync(string userId, IFormFile file)
    {
        if (file == null || file.Length == 0)
            return (false, "Choose an image file to upload.", null);

        if (file.Length > MaxBytes)
        {
            var mb = MaxBytes / (1024 * 1024);
            return (false, $"Image is too large. Maximum size is {mb} MB.", null);
        }

        var ext = Path.GetExtension(file.FileName);
        if (string.IsNullOrEmpty(ext) || !AllowedExtensions.Contains(ext))
            return (false, "Unsupported format. Use JPG, PNG, or WEBP.", null);

        if (!string.IsNullOrEmpty(file.ContentType) && !AllowedContentTypes.Contains(file.ContentType))
            return (false, "Unsupported file type. Use JPG, PNG, or WEBP.", null);

        try
        {
            await using var input = file.OpenReadStream();
            using var image = await Image.LoadAsync(input);

            image.Mutate(ctx =>
            {
                var size = Math.Min(image.Width, image.Height);
                var x = (image.Width - size) / 2;
                var y = (image.Height - size) / 2;
                ctx.Crop(new Rectangle(x, y, size, size));
                ctx.Resize(TargetSize, TargetSize);
            });

            var dir = Path.Combine(_env.WebRootPath, "uploads", "avatars");
            Directory.CreateDirectory(dir);

            var fileName = SanitizeUserId(userId) + ".jpg";
            var fullPath = Path.Combine(dir, fileName);
            await image.SaveAsJpegAsync(fullPath);

            var url = $"/uploads/avatars/{fileName}?v={DateTimeOffset.UtcNow.ToUnixTimeSeconds()}";
            var (saved, err) = await SaveAvatarUrlAsync(userId, url);
            if (!saved) return (false, err, null);

            return (true, string.Empty, url);
        }
        catch (UnknownImageFormatException)
        {
            return (false, "Could not read the image. Use a valid JPG, PNG, or WEBP file.", null);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Avatar upload failed for {UserId}", userId);
            return (false, "Upload failed. Please try again.", null);
        }
    }

    public async Task<(bool ok, string error)> RemoveAsync(string userId)
    {
        try
        {
            var path = GetDiskPath(userId);
            if (File.Exists(path)) File.Delete(path);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not delete avatar file for {UserId}", userId);
        }

        var (saved, err) = await SaveAvatarUrlAsync(userId, null);
        return saved ? (true, string.Empty) : (false, err);
    }

    private async Task<(bool ok, string error)> SaveAvatarUrlAsync(string userId, string? avatarUrl)
    {
        var payload = JsonSerializer.Serialize(new { avatar_url = avatarUrl });
        using var req = new HttpRequestMessage(HttpMethod.Patch, $"users?id=eq.{Uri.EscapeDataString(userId)}")
        {
            Content = new StringContent(payload, Encoding.UTF8, "application/json")
        };
        var resp = await _http.SendAsync(req);
        if (resp.IsSuccessStatusCode) return (true, string.Empty);

        var body = await resp.Content.ReadAsStringAsync();
        _logger.LogWarning("Avatar URL save failed: {Status} {Body}", resp.StatusCode, body);
        return (false, "Could not save your profile picture. Please try again.");
    }

    private string GetDiskPath(string userId) =>
        Path.Combine(_env.WebRootPath, "uploads", "avatars", SanitizeUserId(userId) + ".jpg");

    private static string SanitizeUserId(string userId)
    {
        var safe = new string(userId.Where(c => char.IsLetterOrDigit(c) || c is '-' or '_').ToArray());
        return string.IsNullOrEmpty(safe) ? "user" : safe;
    }
}
