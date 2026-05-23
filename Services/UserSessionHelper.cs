namespace Vest.Services;

public static class UserSessionHelper
{
    public static void SetUserSession(
        ISession session,
        string userId,
        string email,
        string username,
        string? avatarUrl)
    {
        session.SetString("UserId", userId);
        session.SetString("UserEmail", email);
        session.SetString("Username", username);
        if (!string.IsNullOrWhiteSpace(avatarUrl))
            session.SetString("AvatarUrl", avatarUrl.Trim());
        else
            session.Remove("AvatarUrl");
    }
}
