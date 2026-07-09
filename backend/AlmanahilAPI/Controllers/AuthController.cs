// ============================================
// AuthController — the front door for signing in and passwords.
// Job: handles login, forgot/reset/change password requests coming from the website.
// It checks the details, then asks the AuthService to do the real work.
// Used by: the Vue login & profile pages.
// ============================================
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using AlmanahilAPI.Data;
using AlmanahilAPI.DTOs;
using AlmanahilAPI.Helpers;
using AlmanahilAPI.Models;
using AlmanahilAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AlmanahilAPI.Controllers;

// This class handles sign-in and all password actions. There is no [Authorize] on the
// class, so each endpoint decides for itself whether you must be signed in.
[ApiController]
[Route("api/auth")]
public class AuthController(IAuthService authService, AlmanahilDbContext context, IHostEnvironment environment) : ControllerBase
{
    private readonly IAuthService _authService = authService;
    private readonly AlmanahilDbContext _context = context;
    private readonly IHostEnvironment _environment = environment;

    /// <summary>
    /// POST api/auth/login
    /// Authenticates a user and returns a JWT on success.
    /// </summary>
    // [Anyone, no sign-in needed] Checks the email + password. If they are correct it hands
    // back a sign-in token (JWT) the website uses for every later request.
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        try
        {
            var result = await _authService.LoginAsync(request);

            // Invalid credentials or inactive account → 401, no detail leaked.
            if (result is null)
                return Unauthorized(ApiResponse<LoginResponse>.Fail("Invalid email or password."));

            return Ok(ApiResponse<LoginResponse>.Ok(result, "Login successful."));
        }
        catch (Exception)
        {
            // Never expose raw exception details to the client.
            return StatusCode(500,
                ApiResponse<LoginResponse>.Fail("An error occurred while logging in. Please try again."));
        }
    }

    // TEMPORARY: remove before production
    /// <summary>
    /// GET api/auth/admin-only
    /// Protected test endpoint used to verify role-based authorization.
    /// Only a caller whose JWT carries the "Admin" role may reach it.
    /// </summary>
    // [Admin only] A small test endpoint just to prove the "admin" permission works.
    // (Temporary — remove before going live.)
    [HttpGet("admin-only")]
    [Authorize(Roles = "Admin")]
    public IActionResult AdminOnly()
    {
        try
        {
            // Read the caller's identity straight from the validated JWT claims.
            var email = User.FindFirstValue(ClaimTypes.Email);
            var role = User.FindFirstValue(ClaimTypes.Role);

            var data = new { Email = email, Role = role };
            return Ok(ApiResponse<object>.Ok(data, "You are an Admin!"));
        }
        catch (Exception)
        {
            // Never expose raw exception details to the client.
            return StatusCode(500,
                ApiResponse<object>.Fail("An error occurred while verifying your access."));
        }
    }

    // TEMPORARY: remove before production
    /// <summary>
    /// POST api/auth/seed-admin
    /// Creates the first admin user so login can be tested. Dev-only.
    /// </summary>
    // [Development only] Creates the very first admin account so login can be tested.
    // Refuses to run outside development, and refuses if an admin already exists.
    [HttpPost("seed-admin")]
    public async Task<IActionResult> SeedAdmin()
    {
        try
        {
            // Extra safety: never allow seeding outside of development.
            if (!_environment.IsDevelopment())
                return StatusCode(403, ApiResponse<object>.Fail("This endpoint is only available in development."));

            // Prevent running twice — bail out if any admin already exists.
            var adminExists = await _context.Users.AnyAsync(u => u.Role == UserRole.Admin);
            if (adminExists)
                return BadRequest(ApiResponse<object>.Fail("Admin already exists"));

            var adminUser = new User
            {
                FirstName = "System",
                LastName = "Admin",
                // Main administrator identity. MUST match UsersController.MainAdminEmail and
                // the frontend's MAIN_ADMIN_EMAIL — this is the only account allowed to
                // create other Admins.
                Email = "admin@almanahilschool.com",
                Role = UserRole.Admin,
                IsActive = true,
                IsFirstLogin = true,
                CreatedAt = DateTime.UtcNow,
                PasswordHash = PasswordHasher.HashPassword("Admin@123")
            };

            await _context.Users.AddAsync(adminUser);
            await _context.SaveChangesAsync();

            var response = new { adminUser.Id, adminUser.Email };
            return Ok(ApiResponse<object>.Ok(response, "Admin user created successfully."));
        }
        catch (Exception)
        {
            return StatusCode(500,
                ApiResponse<object>.Fail("An error occurred while creating the admin user."));
        }
    }

    /// <summary>
    /// POST api/auth/forgot-password
    /// Starts a password reset by emailing a 6-digit code. Rejects the request when
    /// no active account matches the email.
    /// </summary>
    // [Anyone, no sign-in needed] Starts the "I forgot my password" flow by emailing a
    // 6-digit code to the account. Rejects emails that don't match an active account.
    [HttpPost("forgot-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
    {
        try
        {
            var (success, message) = await _authService.ForgotPasswordAsync(request);

            // Unknown / inactive email → 400 with a clear message.
            if (!success)
                return BadRequest(ApiResponse<string>.Fail(message));

            return Ok(ApiResponse<string>.Ok(message, message));
        }
        catch (Exception)
        {
            return StatusCode(500,
                ApiResponse<string>.Fail("An error occurred while processing your request. Please try again."));
        }
    }

    /// <summary>
    /// POST api/auth/reset-password
    /// Completes a password reset using the emailed 6-digit code.
    /// </summary>
    // [Anyone, no sign-in needed] Finishes the reset: the user sends the emailed 6-digit
    // code plus a new password, and this sets it (if the code is valid and not expired).
    [HttpPost("reset-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
    {
        try
        {
            var (success, message) = await _authService.ResetPasswordAsync(request);

            // Invalid/expired code or a password that fails the policy → 400.
            if (!success)
                return BadRequest(ApiResponse<string>.Fail(message));

            return Ok(ApiResponse<string>.Ok(message, message));
        }
        catch (Exception)
        {
            return StatusCode(500,
                ApiResponse<string>.Fail("An error occurred while resetting your password. Please try again."));
        }
    }

    /// <summary>
    /// POST api/auth/change-password
    /// Changes the authenticated user's password.
    /// </summary>
    // [Any signed-in user] Lets the logged-in user change their own password (they must give
    // their current password too). The user is taken from their sign-in token, not the body.
    [HttpPost("change-password")]
    [Authorize]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        try
        {
            // Pull the user id from the JWT. The token stores it under
            // ClaimTypes.NameIdentifier, but because inbound claims aren't remapped
            // it arrives under the short JWT name "nameid" — so we check both (and
            // "sub" as a final fallback) to stay robust regardless of remapping.
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier)
                              ?? User.FindFirstValue(JwtRegisteredClaimNames.NameId) // "nameid"
                              ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);    // "sub"
            if (!int.TryParse(userIdClaim, out var userId))
                return Unauthorized(ApiResponse<string>.Fail("Invalid or missing authentication token."));

            var (success, message) = await _authService.ChangePasswordAsync(userId, request);

            // Wrong current password, mismatched confirm, or same-as-current → 400.
            if (!success)
                return BadRequest(ApiResponse<string>.Fail(message));

            return Ok(ApiResponse<string>.Ok(message, message));
        }
        catch (Exception)
        {
            return StatusCode(500,
                ApiResponse<string>.Fail("An error occurred while changing your password. Please try again."));
        }
    }
}
