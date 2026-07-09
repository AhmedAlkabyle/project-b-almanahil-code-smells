// ============================================
// PasswordHasher — the security tool for passwords.
// Job: turns a plain password into a scrambled 'hash' (using BCrypt) before saving,
// and checks a typed password against the stored hash at login.
// We never save or see real passwords — only hashes.
// Used by: AuthController (login) and UsersController (creating users).
// ============================================
namespace AlmanahilAPI.Helpers;

/// <summary>
/// Thin wrapper around BCrypt for hashing and verifying user passwords.
/// Centralising it here keeps the hashing algorithm in one place.
/// </summary>
// A small helper class that hides the password-scrambling details in one place.
public static class PasswordHasher
{
    /// <summary>Hashes a plain-text password using BCrypt (with an auto-generated salt).</summary>
    // Takes the real password and returns its scrambled 'hash' text, which is what we store.
    public static string HashPassword(string password) =>
        BCrypt.Net.BCrypt.HashPassword(password);

    /// <summary>Verifies a plain-text password against a previously stored BCrypt hash.</summary>
    // Checks if the typed password matches the saved hash. Returns true if correct.
    public static bool VerifyPassword(string password, string hash) =>
        BCrypt.Net.BCrypt.Verify(password, hash);
}
