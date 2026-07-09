// ============================================
// BrevoSettings — the email-service configuration for sending emails.
// This is NOT a DTO; it holds settings the app reads from appsettings.json.
// Brevo is the outside service that actually delivers our emails.
// ============================================
namespace AlmanahilAPI.Settings;

// Holds the Brevo email settings, filled in from appsettings.json when the app starts.
/// <summary>
/// Strongly-typed Brevo (transactional email) configuration, bound from the
/// "Brevo" section of appsettings.json in Program.cs.
/// </summary>
public class BrevoSettings
{
    // The name of the block in appsettings.json these settings come from ("Brevo").
    /// <summary>The configuration section name these settings are bound from.</summary>
    public const string SectionName = "Brevo";

    // Names the Brevo API key (the secret value itself lives in appsettings.json, not here).
    /// <summary>Brevo API key sent in the "api-key" request header.</summary>
    public string ApiKey { get; set; } = string.Empty;

    // The "from" email address that emails appear to be sent from.
    /// <summary>The "from" email address shown on outgoing messages.</summary>
    public string SenderEmail { get; set; } = string.Empty;

    // The "from" name shown next to the sender address.
    /// <summary>The "from" display name shown on outgoing messages.</summary>
    public string SenderName { get; set; } = string.Empty;
}
