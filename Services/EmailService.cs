using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace Vest.Services;

public class EmailService
{
    private readonly IConfiguration _config;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IConfiguration config, ILogger<EmailService> logger)
    {
        _config = config;
        _logger = logger;
    }

    public async Task SendPasswordResetAsync(string toEmail, string resetUrl)
    {
        var smtp        = _config["Email:SmtpHost"]!;
        var port        = int.Parse(_config["Email:SmtpPort"]!);
        var user        = _config["Email:Username"]!;
        var pass        = _config["Email:Password"]!;
        var fromAddress = _config["Email:From"]!;
        var fromName    = _config["Email:FromName"] ?? "Vest";

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(fromName, fromAddress));
        message.To.Add(new MailboxAddress(toEmail, toEmail));
        message.Subject = "Reset your Vest password";

        message.Body = new TextPart("html")
        {
            Text = $"""
            <div style="font-family:Inter,sans-serif;background:#05070a;color:#fff;padding:40px;max-width:520px;margin:auto;border-radius:16px;border:1px solid rgba(255,255,255,0.1);">
              <h2 style="font-size:1.5rem;margin-bottom:8px;color:#38bdf8;">Reset Your Password</h2>
              <p style="color:#94a3b8;margin-bottom:24px;">Click the button below to reset your Vest password. This link expires in <strong>1 hour</strong>.</p>
              <a href="{resetUrl}"
                 style="display:inline-block;background:#38bdf8;color:#05070a;font-weight:700;text-decoration:none;padding:14px 28px;border-radius:8px;font-size:0.95rem;">
                Reset Password
              </a>
              <p style="color:#94a3b8;font-size:0.8rem;margin-top:24px;">
                If you didn't request this, you can safely ignore this email.<br/>
                Or copy this URL: <a href="{resetUrl}" style="color:#38bdf8;">{resetUrl}</a>
              </p>
            </div>
            """
        };

        using var client = new SmtpClient();
        await client.ConnectAsync(smtp, port, SecureSocketOptions.StartTls);
        await client.AuthenticateAsync(user, pass);
        await client.SendAsync(message);
        await client.DisconnectAsync(true);

        _logger.LogInformation("Password reset email sent to {Email}", toEmail);
    }
}
