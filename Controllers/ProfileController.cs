using Microsoft.AspNetCore.Mvc;
using Vest.Services;

namespace Vest.Controllers;

[ApiController]
[Route("api/profile")]
public class ProfileController : ControllerBase
{
    private readonly AvatarService _avatars;

    public ProfileController(AvatarService avatars) => _avatars = avatars;

    private string? CurrentUserId => HttpContext.Session.GetString("UserId");

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var userId = CurrentUserId;
        if (userId == null) return Unauthorized(new { error = "Not logged in." });

        var avatarUrl = HttpContext.Session.GetString("AvatarUrl")
            ?? await _avatars.GetAvatarUrlAsync(userId);

        return Ok(new
        {
            username  = HttpContext.Session.GetString("Username"),
            email     = HttpContext.Session.GetString("UserEmail"),
            avatarUrl,
        });
    }

    [HttpPost("avatar")]
    [RequestSizeLimit(6 * 1024 * 1024)]
    [RequestFormLimits(MultipartBodyLengthLimit = 6 * 1024 * 1024)]
    public async Task<IActionResult> UploadAvatar(IFormFile? file)
    {
        var userId = CurrentUserId;
        if (userId == null) return Unauthorized(new { error = "Not logged in." });
        if (file == null) return BadRequest(new { error = "Choose an image file to upload." });

        var (ok, error, avatarUrl) = await _avatars.UploadAsync(userId, file);
        if (!ok) return BadRequest(new { error });

        if (!string.IsNullOrEmpty(avatarUrl))
            HttpContext.Session.SetString("AvatarUrl", avatarUrl);

        return Ok(new { message = "Profile picture updated.", avatarUrl });
    }

    [HttpDelete("avatar")]
    public async Task<IActionResult> DeleteAvatar()
    {
        var userId = CurrentUserId;
        if (userId == null) return Unauthorized(new { error = "Not logged in." });

        var (ok, error) = await _avatars.RemoveAsync(userId);
        if (!ok) return BadRequest(new { error });

        HttpContext.Session.Remove("AvatarUrl");
        return Ok(new { message = "Profile picture removed." });
    }
}
