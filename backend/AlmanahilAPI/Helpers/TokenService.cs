// ============================================
// TokenService — makes the login 'ID card' (a JWT).
// A JWT is a small signed token the browser keeps after login and sends back
// with every request to prove who the user is (their id, email and role).
// Because we sign it with a secret key, nobody can fake or edit it.
// Used by: AuthService right after a correct password is entered.
// ============================================
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using AlmanahilAPI.Models;
using Microsoft.IdentityModel.Tokens;

namespace AlmanahilAPI.Helpers;

/// <summary>
/// Generates signed JWT access tokens for authenticated users.
/// Settings are read from the "JwtSettings" section of appsettings.json.
/// </summary>
// The class that builds JWT tokens. It reads its settings from appsettings.json.
public class TokenService(IConfiguration configuration)
{
    private readonly IConfiguration _configuration = configuration;

    /// <summary>
    /// Builds a signed JWT for the given user containing their Id, Email and Role.
    /// </summary>
    // Makes the ID-card token for one user. Receives the user, returns the token as text.
    public string GenerateToken(User user)
    {
        // Step 1: Read the token settings (secret key, issuer, audience, how long it lasts)
        // from appsettings.json. The secret key is required — if it's missing we stop.
        var jwtSettings = _configuration.GetSection("JwtSettings");

        var secretKey = jwtSettings["SecretKey"]
            ?? throw new InvalidOperationException("JwtSettings:SecretKey is not configured.");
        var issuer = jwtSettings["Issuer"];
        var audience = jwtSettings["Audience"];
        var expiryHours = double.TryParse(jwtSettings["ExpiryHours"], out var hours) ? hours : 24;

        // Claims embedded in the token. Role is stored as the enum name (e.g. "Admin").
        // Step 2: Put the facts we want inside the card — the user's id, email and role.
        // (These 'claims' are what the app later reads to know who is calling.)
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role.ToString())
        };

        // Symmetric signing key derived from the configured secret.
        // Step 3: Turn the secret key into a signing key so we can 'stamp' the token.
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        // Step 4: Build the token with the claims, an expiry time, and the signature.
        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(expiryHours),
            signingCredentials: credentials);

        // Step 5: Convert the token into the compact text string sent to the browser.
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
