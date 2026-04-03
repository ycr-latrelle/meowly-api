using SendGrid;
using SendGrid.Helpers.Mail;

namespace MeowlyAPI.Services;

public class EmailService
{
    private readonly IConfiguration _config;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IConfiguration config, ILogger<EmailService> logger)
    {
        _config = config;
        _logger  = logger;
    }

    public async Task SendEmployeeIdEmailAsync(
        string toEmail, string firstName, string employeeId)
    {
        var apiKey = _config["SendGrid:ApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey) || apiKey == "YOUR_SENDGRID_API_KEY")
        {
            // Just log during development — don't crash
            _logger.LogWarning(
                "[EMAIL SKIPPED - No SendGrid key] Employee: {Name}, ID: {Id}",
                firstName, employeeId);
            return;
        }

        var client = new SendGridClient(apiKey);
        var from   = new EmailAddress(
            _config["SendGrid:FromEmail"], _config["SendGrid:FromName"]);
        var to     = new EmailAddress(toEmail, firstName);

        var subject = "Welcome to Meowly — Your Employee ID";
        var html    = $@"
<div style='font-family:sans-serif;max-width:480px;margin:auto;'>
  <h2 style='color:#5c7a5f;'>🐾 Welcome to Meowly, {firstName}!</h2>
  <p>Your employee account has been created. Use the ID below to sign in:</p>
  <div style='background:#e8f0e9;border-radius:12px;padding:1.5rem;text-align:center;margin:1.5rem 0;'>
    <p style='color:#8a7f78;font-size:.8rem;font-weight:700;text-transform:uppercase;letter-spacing:.1em;margin:0'>Your Employee ID</p>
    <p style='font-size:2rem;font-weight:900;color:#5c7a5f;letter-spacing:4px;margin:.5rem 0 0;'>{employeeId}</p>
  </div>
  <p style='color:#8a7f78;font-size:.85rem;'>Keep this ID private. You will need it every time you log in.</p>
  <p style='color:#8a7f78;font-size:.85rem;'>— The Meowly Team 🐱</p>
</div>";

        var msg = MailHelper.CreateSingleEmail(from, to, subject, null, html);
        var res = await client.SendEmailAsync(msg);

        if (!res.IsSuccessStatusCode)
            _logger.LogError("SendGrid failed with status {Status}", res.StatusCode);
    }
}
