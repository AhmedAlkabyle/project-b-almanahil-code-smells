// ============================================
// EmailService — the part that actually sends emails.
// Job: send an email (like the password-reset code) by calling the Brevo
// email service over the internet. If sending fails it just returns false
// instead of crashing the app.
// Used by: AuthService when a user asks to reset their password.
// ============================================
using System.Text;
using System.Text.Json;
using AlmanahilAPI.Settings;
using Microsoft.Extensions.Options;

namespace AlmanahilAPI.Services;

/// <summary>Contract for sending transactional emails.</summary>
// A promise/blueprint: anything that sends email must have a SendEmailAsync method.
public interface IEmailService
{
    /// <summary>
    /// Sends a single HTML email. Returns true on success (2xx response),
    /// false on any failure. Never throws — failures are logged and swallowed.
    /// </summary>
    // Sends one email. Receives who it's to, the subject and the HTML body. Returns true if sent.
    Task<bool> SendEmailAsync(string toEmail, string toName, string subject, string htmlContent);
}

/// <summary>
/// <see cref="IEmailService"/> implementation backed by the Brevo
/// transactional email API (https://api.brevo.com/v3/smtp/email).
/// Uses a typed/factory <see cref="HttpClient"/> registered in Program.cs.
/// </summary>
// The real email sender. It fills in the interface promise using the Brevo service.
public class BrevoEmailService(
    HttpClient httpClient,
    IOptions<BrevoSettings> options,
    ILogger<BrevoEmailService> logger) : IEmailService
{
    private const string BrevoEndpoint = "https://api.brevo.com/v3/smtp/email";

    private readonly HttpClient _httpClient = httpClient;
    private readonly BrevoSettings _settings = options.Value;
    private readonly ILogger<BrevoEmailService> _logger = logger;

    // Does the actual sending. Returns true if Brevo accepted the email, false otherwise.
    public async Task<bool> SendEmailAsync(string toEmail, string toName, string subject, string htmlContent)
    {
        // We 'try' the send; if anything goes wrong we catch it below so the app never crashes.
        try
        {
            // Step 1: Package the email details (from, to, subject, body) into JSON,
            // which is the format Brevo expects.
            // Build the JSON payload exactly as Brevo's /smtp/email endpoint expects.
            var payload = new
            {
                sender = new { name = _settings.SenderName, email = _settings.SenderEmail },
                to = new[] { new { email = toEmail, name = toName } },
                subject,
                htmlContent
            };

            var json = JsonSerializer.Serialize(payload);

            // Step 2: Build the web request to Brevo's email address (URL) carrying that JSON.
            using var request = new HttpRequestMessage(HttpMethod.Post, BrevoEndpoint)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };

            // Step 3: Add our secret API key so Brevo knows the request is from us.
            // Brevo authenticates via the "api-key" header (not a Bearer token).
            request.Headers.Add("api-key", _settings.ApiKey);

            // Step 4: Actually send the request over the internet and wait for the reply.
            var response = await _httpClient.SendAsync(request);

            // Step 5: If Brevo replied "OK", log it and report success.
            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Brevo email sent to {ToEmail} (subject: {Subject}).", toEmail, subject);
                return true;
            }

            // Step 6: If Brevo rejected it, write down why (for us to read later) and report failure.
            // Log the status + body to help diagnose rejected sends, but don't throw.
            var body = await response.Content.ReadAsStringAsync();
            _logger.LogError(
                "Brevo email to {ToEmail} failed with status {StatusCode}. Response: {Body}",
                toEmail, (int)response.StatusCode, body);
            return false;
        }
        // This 'catch' runs if something unexpected broke (e.g. no internet). We log it
        // and return false so the caller keeps working instead of crashing.
        catch (Exception ex)
        {
            // Network errors, serialization issues, etc. — never bubble up to the caller.
            _logger.LogError(ex, "Unexpected error sending Brevo email to {ToEmail}.", toEmail);
            return false;
        }
    }
}
