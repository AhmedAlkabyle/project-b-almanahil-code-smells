// ============================================
// AuthDtos — the small data parcels used for logging in and passwords.
// A DTO is a simple 'data parcel' the website and server pass back and forth.
// This file groups the login, change-password, and reset-password parcels.
// ============================================
using System.ComponentModel.DataAnnotations;

namespace AlmanahilAPI.DTOs;

// Travels: website -> server. The email + password typed on the login screen.
/// <summary>Payload sent by the client to authenticate.</summary>
public class LoginRequest
{
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;
}

// Travels: server -> website. Sent back after a successful login.
/// <summary>Returned on successful login: the JWT plus basic user info for the client.</summary>
public class LoginResponse
{
    // The login "ticket" (JWT) the website stores and sends with future requests.
    public string Token { get; set; } = string.Empty;
    // The user's database id.
    public int Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;

    /// <summary>The user's role as its enum name (e.g. "Admin").</summary>
    public string Role { get; set; } = string.Empty;

    /// <summary>Teacher only: the level they teach ("Secondary" / "HighSchool"); null for other roles.</summary>
    public string? TeacherLevel { get; set; }

    // True the first time someone logs in, so we can force them to pick a new password.
    /// <summary>True if the user has never changed their initial password.</summary>
    public bool IsFirstLogin { get; set; }
}

// Travels: website -> server. Used when a logged-in user changes their own password.
/// <summary>Payload for changing the authenticated user's password.</summary>
public class ChangePasswordRequest
{
    // The password they use right now (checked before we allow the change).
    [Required]
    public string CurrentPassword { get; set; } = string.Empty;

    // Strength (min 8 chars, letters + numbers) is enforced in AuthService so the
    // policy and its messages live in one place — here we only require presence.
    // The password they want to switch to.
    [Required]
    public string NewPassword { get; set; } = string.Empty;

    // Re-typing the new password so we can catch typos; must match NewPassword.
    /// <summary>Must equal <see cref="NewPassword"/>; verified server-side.</summary>
    [Required]
    public string ConfirmPassword { get; set; } = string.Empty;
}

// Travels: website -> server. Step 1 of "forgot password": just the email to send a code to.
/// <summary>Payload to start a password reset: the account's email address.</summary>
public class ForgotPasswordRequest
{
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;
}

// Travels: website -> server. Step 2 of "forgot password": the emailed code + the new password.
/// <summary>Payload to complete a password reset with the emailed 6-digit code.</summary>
public class ResetPasswordRequest
{
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    // The 6-digit code that was emailed to the user.
    [Required]
    public string Code { get; set; } = string.Empty;

    // The new password they want to set.
    [Required]
    public string NewPassword { get; set; } = string.Empty;

    // Re-typing the new password so we can catch typos; must match NewPassword.
    /// <summary>Must equal <see cref="NewPassword"/>; verified server-side.</summary>
    [Required]
    public string ConfirmPassword { get; set; } = string.Empty;
}
