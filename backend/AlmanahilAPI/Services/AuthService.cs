// ============================================
// AuthService — the 'login and passwords' brain of the app.
// Job: check login details, let users change their password, and run the
// "forgot password" flow (email a 6-digit code, then let them set a new
// password using that code). It talks to the database and the email sender.
// Used by: AuthController (the login / password endpoints).
// ============================================
using AlmanahilAPI.Data;
using AlmanahilAPI.DTOs;
using AlmanahilAPI.Helpers;
using Microsoft.EntityFrameworkCore;

namespace AlmanahilAPI.Services;

/// <summary>Contract for authentication-related operations.</summary>
// A blueprint listing the login/password jobs the app must be able to do.
public interface IAuthService
{
    /// <summary>
    /// Validates credentials and, when valid, returns a <see cref="LoginResponse"/>
    /// with a freshly issued JWT. Returns null when the credentials are invalid
    /// or the account is inactive.
    /// </summary>
    // Checks email + password. If correct, returns the user info + login token; otherwise null.
    Task<LoginResponse?> LoginAsync(LoginRequest request);

    /// <summary>
    /// Changes the password for the given user after verifying the current one.
    /// Enforces the confirm-password match and blocks reusing the current password.
    /// Returns (false, reason) on any failure.
    /// </summary>
    // Changes a logged-in user's password (after checking their old one). Returns ok + a message.
    Task<(bool Success, string Message)> ChangePasswordAsync(int userId, ChangePasswordRequest request);

    /// <summary>
    /// Starts a password reset: if the email belongs to an active user, generates a
    /// 6-digit code, stores its hash with a 15-minute expiry, and emails the code.
    /// Returns (false, message) when no active account matches the email so the
    /// caller can reject the request explicitly.
    /// </summary>
    // Starts "forgot password": emails a 6-digit code to the user. Returns ok + a message.
    Task<(bool Success, string Message)> ForgotPasswordAsync(ForgotPasswordRequest request);
    


    /// <summary>
    /// Completes a password reset. Verifies the emailed code against the stored hash
    /// (checking expiry) and, if the new password meets the policy, updates it and
    /// clears the reset fields. Returns (false, reason) on any failure.
    /// </summary>
    // Finishes "forgot password": checks the emailed code, then sets the new password.
    Task<(bool Success, string Message)> ResetPasswordAsync(ResetPasswordRequest request);
}

/// <summary>
/// EF Core–backed implementation of <see cref="IAuthService"/>.
/// All data access uses LINQ — no raw SQL.
/// </summary>
// The real class that carries out all the login/password jobs above.
// It receives (via the brackets) the database, the token maker and the email sender.
public class AuthService(
    AlmanahilDbContext context,
    TokenService tokenService,
    IEmailService emailService) : IAuthService
{
    private readonly AlmanahilDbContext _context = context;
    private readonly TokenService _tokenService = tokenService;
    private readonly IEmailService _emailService = emailService;

    // Shown in the reset email. Kept here so the copy lives in one place.
    private const string SchoolName = "Almanahil Libyan School";

    // How long an emailed reset code stays valid.
    private static readonly TimeSpan ResetCodeLifetime = TimeSpan.FromMinutes(15);

    // Handles logging in: verify email + password, and give back a token if correct.
    public async Task<LoginResponse?> LoginAsync(LoginRequest request)
    {
        // Step 1: Look up the user by their email (ignoring upper/lower case).
        // Find the user by email (case-insensitive) using EF Core LINQ.
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Email.ToLower() == request.Email.ToLower());

        // Step 2: If there's no such user, or their account is switched off, stop here.
        // No such user, or the account has been deactivated.
        if (user is null || !user.IsActive)
            return null;

        // Step 3: Check the typed password against the stored hash. Wrong password = stop.
        // Verify the supplied password against the stored BCrypt hash.
        if (!PasswordHasher.VerifyPassword(request.Password, user.PasswordHash))
            return null;

        // Step 4: Everything is correct — make a login token (JWT) for this user.
        // Credentials are valid — issue a token and return the user info.
        var token = _tokenService.GenerateToken(user);

        // Step 5: Send back the token plus basic profile info the website will display.
        return new LoginResponse
        {
            Token = token,
            Id = user.Id,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Email = user.Email,
            Role = user.Role.ToString(),
            TeacherLevel = user.TeacherLevel,
            IsFirstLogin = user.IsFirstLogin
        };
    }

    // Lets a signed-in user change their password. Runs several safety checks first.
    public async Task<(bool Success, string Message)> ChangePasswordAsync(int userId, ChangePasswordRequest request)
    {
        // Step 1: Find the user by their id. If not found, stop.
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user is null)
            return (false, "User not found.");

        // Step 2: Make sure they typed their CURRENT password correctly.
        // The current password must match before we allow a change.
        if (!PasswordHasher.VerifyPassword(request.CurrentPassword, user.PasswordHash))
            return (false, "Current password is incorrect.");

        // Step 3: The new password and its "confirm" box must be the same.
        // Confirm-password check first, before any other password validation.
        if (request.NewPassword != request.ConfirmPassword)
            return (false, "Passwords do not match.");

        // Step 4: The new password must follow the rules (8+ chars, letters and numbers).
        // Enforce the new-password policy (min 8 chars, letters + numbers).
        if (!IsPasswordStrong(request.NewPassword, out var policyError))
            return (false, policyError);

        // Step 5: The new password can't be the same as the old one.
        // Block reusing the current password.
        if (PasswordHasher.VerifyPassword(request.NewPassword, user.PasswordHash))
            return (false, "New password must be different from your current password.");

        // Step 6: All checks passed — scramble (hash) the new password and save it.
        // Hash and persist the new password, and clear the first-login flag.
        user.PasswordHash = PasswordHasher.HashPassword(request.NewPassword);
        user.IsFirstLogin = false;
        user.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return (true, "Password changed successfully.");
    }

    // "Forgot password" part 1: makes a one-time code and emails it to the user.
    public async Task<(bool Success, string Message)> ForgotPasswordAsync(ForgotPasswordRequest request)
    {
        // Step 1: Find the user by email (ignoring upper/lower case).
        // Look up the user (case-insensitive) with EF Core LINQ.
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Email.ToLower() == request.Email.ToLower());

        // Step 2: Only real, active accounts may receive a code. Otherwise stop.
        // Reject unknown or inactive accounts explicitly — the reset code is only
        // ever sent to a real, active user.
        if (user is null || !user.IsActive)
            return (false, "No account found with this email address.");

        // Step 3: Make a random 6-digit code (like 482913).
        // Random 6-digit numeric code (100000–999999). Upper bound is exclusive.
        var code = Random.Shared.Next(100000, 1000000).ToString();

        // Step 4: Save only the SCRAMBLED (hashed) code in the database, plus a
        // time 15 minutes from now when it stops working. We never store the plain code.
        // Persist only the BCrypt hash of the code — never the plain value —
        // together with a UTC expiry.
        user.ResetCode = PasswordHasher.HashPassword(code);
        user.ResetCodeExpiresAt = DateTime.UtcNow.Add(ResetCodeLifetime);
        user.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        // Step 5: Build the email and send the real (plain) 6-digit code to the user.
        // Email the plain code to the user. IEmailService never throws; if the
        // send fails it logs internally and we still return success upstream.
        var html = BuildResetEmailHtml(user.FirstName, code);
        await _emailService.SendEmailAsync(
            user.Email,
            $"{user.FirstName} {user.LastName}",
            "Almanahil Password Reset Code",
            html);

        return (true, "A reset code has been sent to your email.");
    }

    // "Forgot password" part 2: checks the emailed code, then sets the new password.
    public async Task<(bool Success, string Message)> ResetPasswordAsync(ResetPasswordRequest request)
    {
        // We use ONE vague error message on purpose, so attackers can't tell which check failed.
        // Deliberately generic so we never reveal which check failed.
        const string invalidCode = "Invalid or expired code.";

        // Step 1: Find the user by email.
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Email.ToLower() == request.Email.ToLower());

        if (user is null)
            return (false, invalidCode);

        // Step 2: There must be a saved code that hasn't expired yet.
        // No reset in progress, or the code has expired.
        if (string.IsNullOrEmpty(user.ResetCode)
            || user.ResetCodeExpiresAt is null
            || user.ResetCodeExpiresAt.Value < DateTime.UtcNow)
            return (false, invalidCode);

        // Step 3: The code they typed must match the scrambled code we saved earlier.
        // The submitted code must match the stored BCrypt hash.
        if (!PasswordHasher.VerifyPassword(request.Code, user.ResetCode))
            return (false, invalidCode);

        // Step 4: The new password and its "confirm" box must match.
        // Confirm-password check first, before any other password validation.
        if (request.NewPassword != request.ConfirmPassword)
            return (false, "Passwords do not match.");

        // Step 5: The new password must follow the rules (8+ chars, letters and numbers).
        // Enforce the new-password policy before changing anything.
        if (!IsPasswordStrong(request.NewPassword, out var policyError))
            return (false, policyError);

        // Step 6: The new password can't be the same as the old one. (We keep the code
        // valid here so they can simply try a different password.)
        // Block reusing the current password. Note: we return WITHOUT clearing the
        // reset code, so the user can retry with a different password using the
        // same code (while it is still valid).
        if (PasswordHasher.VerifyPassword(request.NewPassword, user.PasswordHash))
            return (false, "New password must be different from your current password.");

        // Step 7: All good — save the new hashed password and delete the used code so
        // it can't be reused.
        // Success: set the new password and clear the reset fields so the code
        // cannot be reused. Clearing IsFirstLogin too, since they've now set it.
        user.PasswordHash = PasswordHasher.HashPassword(request.NewPassword);
        user.ResetCode = null;
        user.ResetCodeExpiresAt = null;
        user.IsFirstLogin = false;
        user.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return (true, "Your password has been reset successfully.");
    }

    /// <summary>
    /// New-password policy: at least 8 characters, containing at least one letter
    /// and one number. Returns false with a friendly reason when it doesn't pass.
    /// </summary>
    // Checks a password is strong enough. Returns true if ok, or false + a reason if not.
    private static bool IsPasswordStrong(string password, out string error)
    {
        if (string.IsNullOrWhiteSpace(password) || password.Length < 8)
        {
            error = "Password must be at least 8 characters long.";
            return false;
        }

        if (!password.Any(char.IsLetter) || !password.Any(char.IsDigit))
        {
            error = "Password must contain at least one letter and one number.";
            return false;
        }

        error = string.Empty;
        return true;
    }

    /// <summary>
    /// Builds the responsive HTML body for the password-reset email.
    /// Uses a table-based layout with inline CSS only, for broad email-client
    /// support. Only the presentation is here — the code value is passed in.
    /// </summary>
    // Builds the nice-looking HTML for the reset email. Receives the name + code, returns HTML text.
    private static string BuildResetEmailHtml(string firstName, string code)
    {
        // Personalize the greeting when we have a name; otherwise a plain "Hello,".
        var greeting = string.IsNullOrWhiteSpace(firstName) ? "Hello," : $"Hello {firstName},";

        return $@"
<!DOCTYPE html>
<html lang=""en"">
<head>
  <meta charset=""utf-8"" />
  <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"" />
  <title>{SchoolName}</title>
</head>
<body style=""margin:0; padding:0; background-color:#f3f4f6;"">
  <!-- Full-width gray page background -->
  <table role=""presentation"" width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""background-color:#f3f4f6; padding:24px 12px;"">
    <tr>
      <td align=""center"">
        <!-- Centered white card -->
        <table role=""presentation"" width=""600"" cellpadding=""0"" cellspacing=""0"" style=""width:100%; max-width:600px; background-color:#ffffff; border-radius:12px; overflow:hidden; box-shadow:0 6px 24px rgba(0,0,0,0.08); font-family:'Segoe UI', Arial, sans-serif;"">
          <!-- Navy header -->
          <tr>
            <td align=""center"" style=""background-color:#1E4C9A; padding:28px 24px;"">
              <span style=""color:#ffffff; font-size:22px; font-weight:700; letter-spacing:0.3px;"">{SchoolName}</span>
            </td>
          </tr>
          <!-- Orange accent divider -->
          <tr>
            <td style=""height:4px; line-height:4px; font-size:0; background-color:#F2A03D;"">&nbsp;</td>
          </tr>
          <!-- Body: greeting + intro -->
          <tr>
            <td style=""padding:36px 40px 8px 40px; color:#1f2937;"">
              <p style=""margin:0 0 16px 0; font-size:16px;"">{greeting}</p>
              <p style=""margin:0 0 26px 0; font-size:15px; line-height:1.6; color:#4b5563;"">
                We received a request to reset your password. Use the verification code below:
              </p>
            </td>
          </tr>
          <!-- Code box -->
          <tr>
            <td align=""center"" style=""padding:0 40px;"">
              <div style=""display:inline-block; background-color:#eef3fb; border:2px solid #F2A03D; border-radius:12px; padding:18px 34px;"">
                <span style=""font-family:'Segoe UI', Arial, sans-serif; font-size:34px; font-weight:700; letter-spacing:10px; color:#1E4C9A;"">{code}</span>
              </div>
            </td>
          </tr>
          <!-- Expiry + ignore note -->
          <tr>
            <td style=""padding:26px 40px 0 40px;"">
              <p style=""margin:0 0 8px 0; font-size:14px; color:#4b5563;"">This code will expire in <strong>15 minutes</strong>.</p>
              <p style=""margin:0; font-size:13px; color:#9ca3af;"">If you did not request this, you can safely ignore this email.</p>
            </td>
          </tr>
          <!-- Divider before footer -->
          <tr>
            <td style=""padding:28px 40px 0 40px;"">
              <div style=""border-top:1px solid #e5e7eb; font-size:0; line-height:0;"">&nbsp;</div>
            </td>
          </tr>
          <!-- Footer -->
          <tr>
            <td align=""center"" style=""padding:18px 40px 32px 40px;"">
              <p style=""margin:0; font-size:12px; color:#9ca3af; line-height:1.7;"">
                {SchoolName}<br />
                &copy; 2026 {SchoolName}. All rights reserved.
              </p>
            </td>
          </tr>
        </table>
      </td>
    </tr>
  </table>
</body>
</html>";
    }
}
