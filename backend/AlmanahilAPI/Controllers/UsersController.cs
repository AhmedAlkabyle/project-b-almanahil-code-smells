// ============================================
// UsersController — the admin's tool to manage all accounts.
// Job: list, view, create, edit, activate/deactivate and delete users (students,
// teachers, parents, admins). Creating a user makes a random temporary password,
// scrambles it safely, saves the user, and emails them their login details.
// Used by: the Vue admin "Users" page. Admin-only.
// ============================================
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using AlmanahilAPI.Data;
using AlmanahilAPI.DTOs;
using AlmanahilAPI.Helpers;
using AlmanahilAPI.Models;
using AlmanahilAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AlmanahilAPI.Controllers;

/// <summary>
/// Admin-only user management (Module 2) — the heart of the admin dashboard. Creating a
/// user generates a temporary password, hashes it with BCrypt (never stored/returned in
/// plain text), and emails the credentials via the existing Brevo service. Mirrors
/// ClassesController: EF Core + LINQ only, async, try/catch everywhere, standard
/// ApiResponse envelope. Deactivation replaces hard deletes.
/// </summary>
// This class manages all user accounts. Admin-only.
[ApiController]
[Route("api/users")]
[Authorize(Roles = "Admin")]
public class UsersController(
    AlmanahilDbContext context,
    IEmailService emailService,
    ILogger<UsersController> logger) : ControllerBase
{
    private readonly AlmanahilDbContext _context = context;
    private readonly IEmailService _emailService = emailService;
    private readonly ILogger<UsersController> _logger = logger;

    /// <summary>
    /// The single account allowed to create other Admins. This is the REAL, server-side
    /// enforcement of the rule the frontend also applies — the frontend check is only for
    /// display and must never be trusted on its own. Keep this in sync with the frontend's
    /// MAIN_ADMIN_EMAIL.
    /// </summary>
    private const string MainAdminEmail = "admin@almanahilschool.com";

    /// <summary>
    /// Teacher and Admin accounts must use a real school mailbox on this domain. This is
    /// the REAL enforcement — the frontend performs the same check only for instant UX and
    /// must never be trusted on its own. Kept as a const so the domain changes in one place.
    /// </summary>
    private const string SchoolEmailDomain = "@almanahilschool.com";

    private const string SchoolName = "Almanahil Libyan School";

    /// <summary>Allowed values for a teacher's level (validated server-side; the frontend mirrors these).</summary>
    private static readonly string[] AllowedTeacherLevels = ["Secondary", "HighSchool"];

    /// <summary>
    /// GET /api/users — list users (optional ?role=, ?search= and ?unassigned=true filters).
    /// ?unassigned=true returns only users with no class (ClassId == null) — used by the
    /// "Assign Students" dialog. Password hashes are never selected or returned.
    /// </summary>
    // [Admin only] Lists users. Optional filters: ?role= (e.g. Student), ?search= (name or
    // email), and ?unassigned=true (users with no class yet). Passwords are never returned.
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? role, [FromQuery] string? search, [FromQuery] bool unassigned = false)
    {
        try
        {
            var query = _context.Users.AsQueryable();

            // Optional role filter (silently ignored if it isn't a valid role name).
            if (!string.IsNullOrWhiteSpace(role) && Enum.TryParse<UserRole>(role, ignoreCase: true, out var roleFilter))
                query = query.Where(u => u.Role == roleFilter);

            // Optional "unassigned" filter — users not yet linked to any class (ClassId is null).
            // Combined with role=Student this yields the students available for enrollment.
            if (unassigned)
                query = query.Where(u => u.ClassId == null);

            // Optional search across name + email (case-insensitive).
            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim().ToLower();
                query = query.Where(u =>
                    u.FirstName.ToLower().Contains(term) ||
                    (u.MiddleName != null && u.MiddleName.ToLower().Contains(term)) ||
                    u.LastName.ToLower().Contains(term) ||
                    u.Email.ToLower().Contains(term));
            }

            // Project to a lightweight row (no PasswordHash). Role.ToString() can't be
            // translated to SQL, so we pull the raw columns then map in memory below.
            var rows = await query
                .OrderBy(u => u.FirstName)
                .ThenBy(u => u.LastName)
                .Select(u => new UserRow
                {
                    Id = u.Id,
                    FirstName = u.FirstName,
                    MiddleName = u.MiddleName,
                    LastName = u.LastName,
                    Email = u.Email,
                    Role = u.Role,
                    Gender = u.Gender,
                    DateOfBirth = u.DateOfBirth,
                    PhoneNumber = u.PhoneNumber,
                    ClassId = u.ClassId,
                    ClassName = u.Class != null ? u.Class.Name : null,
                    TeacherLevel = u.TeacherLevel,
                    Photo = u.Photo,
                    IsActive = u.IsActive,
                    CreatedAt = u.CreatedAt
                })
                .ToListAsync();

            var users = rows.Select(MapToDto).ToList();

            return Ok(ApiResponse<List<UserResponseDto>>.Ok(users, "Users retrieved successfully."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve users.");
            return StatusCode(500,
                ApiResponse<List<UserResponseDto>>.Fail("An error occurred while retrieving users."));
        }
    }

    /// <summary>GET /api/users/{id} — one user, or a friendly not-found.</summary>
    // [Admin only] Gets one user by id (safe fields only; no password), or a friendly "not found".
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        try
        {
            var user = await BuildUserResponseAsync(id);
            if (user is null)
                return NotFound(ApiResponse<UserResponseDto>.Fail("User not found."));

            return Ok(ApiResponse<UserResponseDto>.Ok(user, "User retrieved successfully."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve user {UserId}.", id);
            return StatusCode(500,
                ApiResponse<UserResponseDto>.Fail("An error occurred while retrieving the user."));
        }
    }

    /// <summary>
    /// POST /api/users — create a user. Validates email uniqueness and the role rules,
    /// generates + hashes a temporary password, saves the user (and, for students, the
    /// parent link), then emails the credentials. The password is never returned.
    /// </summary>
    // [Admin only] Creates a new user. In short: check the details and role rules, make a
    // random temporary password, scramble it, save the user (plus a parent link for
    // students), then email them their login. The password itself is never returned.
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateUserDto dto)
    {
        try
        {
            // Parse the role first — every validation and step below depends on it.
            if (!Enum.TryParse<UserRole>(dto.Role, ignoreCase: true, out var role) || !Enum.IsDefined(role))
                return BadRequest(ApiResponse<UserResponseDto>.Fail("Please select a valid role."));

            // Validate the basics, then the role-specific rules (which resolve the role fields).
            var basicsError = await ValidateBasicsAsync(dto, role);
            if (basicsError is not null) return basicsError;
            var (roleError, fields) = await ResolveRoleSpecificFieldsAsync(role, dto);
            if (roleError is not null) return roleError;

            // Make + hash a temporary password (only the hash is stored), then build + save the
            // user (+ the parent link for students, in one transaction).
            var tempPassword = GenerateTemporaryPassword();
            var user = BuildUserEntity(dto, role, fields!, PasswordHasher.HashPassword(tempPassword));
            _context.Users.Add(user);
            if (role == UserRole.Student && fields!.ParentIdToLink is not null)
                _context.ParentStudents.Add(new ParentStudent
                {
                    ParentId = fields.ParentIdToLink.Value,
                    Student = user,
                    CreatedAt = DateTime.UtcNow
                });
            await _context.SaveChangesAsync();

            // Email the credentials (a failed send only downgrades the message).
            var emailSent = await SendCredentialsEmailAsync(user, tempPassword);

            // Return the safe DTO (never the hash). The main admin also sees the temp password.
            var result = await BuildUserResponseAsync(user.Id);
            if (string.Equals(GetCallerEmail(), MainAdminEmail, StringComparison.OrdinalIgnoreCase))
                result!.TemporaryPassword = tempPassword;
            var message = emailSent
                ? "User created successfully. Login details have been emailed."
                : "User created, but the email could not be sent.";
            return CreatedAtAction(nameof(GetById), new { id = user.Id },
                ApiResponse<UserResponseDto>.Ok(result!, message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create user.");
            return StatusCode(500,
                ApiResponse<UserResponseDto>.Fail("An error occurred while creating the user."));
        }
    }

    // ---- Create() helpers (extracted from the original long method) ----

    /// <summary>The role-specific fields resolved by <see cref="ResolveRoleSpecificFieldsAsync"/>.</summary>
    private sealed record ResolvedRoleFields(int? ClassId, string? PhoneNumber, int? ParentIdToLink, string? TeacherLevel);

    /// <summary>
    /// Validates the role-independent basics for a new user: date of birth, email domain
    /// (Teacher/Admin), middle name (Student/Parent), photo size, and email uniqueness.
    /// Returns the BadRequest to send, or null when everything is valid.
    /// </summary>
    private async Task<IActionResult?> ValidateBasicsAsync(CreateUserDto dto, UserRole role)
    {
        // Date of birth can't be in the future (matches the frontend rule).
        if (dto.DateOfBirth > DateOnly.FromDateTime(DateTime.UtcNow))
            return BadRequest(ApiResponse<UserResponseDto>.Fail("Date of birth cannot be in the future."));

        var email = dto.Email.Trim();

        // Teacher/Admin must use a real school mailbox. Real enforcement — checked
        // before any user is created or any email is sent. (Frontend mirrors this for UX.)
        if (RequiresSchoolDomain(role) && !email.EndsWith(SchoolEmailDomain, StringComparison.OrdinalIgnoreCase))
            return BadRequest(ApiResponse<UserResponseDto>.Fail("Teacher and admin emails must use the @almanahilschool.com domain."));

        // Students and parents must have a middle name (real enforcement; frontend mirrors it).
        if (RequiresMiddleName(role) && string.IsNullOrWhiteSpace(dto.MiddleName))
            return BadRequest(ApiResponse<UserResponseDto>.Fail("Middle name is required for students and parents."));

        // Photos are optional (the admin form no longer collects them). If one is ever
        // supplied via the API, reject only when it exceeds the size cap.
        if (!IsPhotoWithinLimit(dto.Photo))
            return BadRequest(ApiResponse<UserResponseDto>.Fail("The photo is too large. Please choose an image under 1.5 MB."));

        // Email must be unique (case-insensitive).
        var emailExists = await _context.Users.AnyAsync(u => u.Email.ToLower() == email.ToLower());
        if (emailExists)
            return BadRequest(ApiResponse<UserResponseDto>.Fail("A user with this email already exists."));

        return null;
    }

    /// <summary>
    /// Applies the rules that depend on the role (a student needs a class and a parent; a
    /// parent needs a phone; a teacher needs a level; only the main admin may create admins)
    /// and resolves the role-specific fields. Returns an error result, or the resolved fields.
    /// </summary>
    private async Task<(IActionResult? Error, ResolvedRoleFields? Fields)> ResolveRoleSpecificFieldsAsync(
        UserRole role, CreateUserDto dto)
    {
        int? classId = null;
        string? phoneNumber = null;
        int? parentIdToLink = null;
        string? teacherLevel = null;

        switch (role)
        {
            case UserRole.Admin:
                // REAL backend enforcement: only the main admin may create admins.
                var callerEmail = GetCallerEmail();
                if (!string.Equals(callerEmail, MainAdminEmail, StringComparison.OrdinalIgnoreCase))
                    return (BadRequest(ApiResponse<UserResponseDto>.Fail("Only the main administrator can create admin accounts.")), null);
                break;

            case UserRole.Student:
                // ClassId required + must exist.
                if (dto.ClassId is null or < 1)
                    return (BadRequest(ApiResponse<UserResponseDto>.Fail("Please select a class for the student.")), null);
                if (!await _context.Classes.AnyAsync(c => c.Id == dto.ClassId.Value))
                    return (BadRequest(ApiResponse<UserResponseDto>.Fail("Selected class not found.")), null);
                classId = dto.ClassId.Value;

                // ParentId required + must be an existing Parent.
                if (dto.ParentId is null or < 1)
                    return (BadRequest(ApiResponse<UserResponseDto>.Fail("Please select a parent for the student.")), null);
                if (!await _context.Users.AnyAsync(u => u.Id == dto.ParentId.Value && u.Role == UserRole.Parent))
                    return (BadRequest(ApiResponse<UserResponseDto>.Fail("Selected parent not found.")), null);
                parentIdToLink = dto.ParentId.Value;

                // For a student, PhoneNumber is the parent/guardian contact (optional).
                phoneNumber = Clean(dto.PhoneNumber);
                break;

            case UserRole.Parent:
                // Parent must supply their own phone number.
                if (string.IsNullOrWhiteSpace(dto.PhoneNumber))
                    return (BadRequest(ApiResponse<UserResponseDto>.Fail("Phone number is required for a parent.")), null);
                phoneNumber = dto.PhoneNumber.Trim();
                break;

            case UserRole.Teacher:
                // Teacher level is required and must be one of the allowed values.
                teacherLevel = AllowedTeacherLevels
                    .FirstOrDefault(l => l.Equals(dto.TeacherLevel?.Trim(), StringComparison.OrdinalIgnoreCase));
                if (teacherLevel is null)
                    return (BadRequest(ApiResponse<UserResponseDto>.Fail(
                        "Please choose the teacher's level (Secondary or High School).")), null);
                break;
        }

        return (null, new ResolvedRoleFields(classId, phoneNumber, parentIdToLink, teacherLevel));
    }

    /// <summary>Builds the <see cref="User"/> entity from the DTO, role, resolved fields and password hash.</summary>
    private static User BuildUserEntity(CreateUserDto dto, UserRole role, ResolvedRoleFields fields, string passwordHash) => new()
    {
        FirstName = dto.FirstName.Trim(),
        MiddleName = Clean(dto.MiddleName),
        LastName = dto.LastName.Trim(),
        Email = dto.Email.Trim(),
        Gender = dto.Gender.Trim(),
        DateOfBirth = dto.DateOfBirth,
        Role = role,
        PhoneNumber = fields.PhoneNumber,
        ClassId = fields.ClassId,
        TeacherLevel = fields.TeacherLevel,
        Photo = Clean(dto.Photo),
        PasswordHash = passwordHash,
        IsActive = true,
        IsFirstLogin = true, // force the password change on first login
        CreatedAt = DateTime.UtcNow
    };

    /// <summary>
    /// Emails the new user their login credentials via Brevo. IEmailService never throws —
    /// a false result just means the send failed, so the caller keeps the account and warns.
    /// </summary>
    private async Task<bool> SendCredentialsEmailAsync(User user, string tempPassword)
    {
        var html = BuildAccountEmailHtml(user.FirstName, user.Email, tempPassword);
        return await _emailService.SendEmailAsync(
            user.Email,
            $"{user.FirstName} {user.LastName}",
            "Your Almanahil School Account",
            html);
    }

    /// <summary>PUT /api/users/{id} — update personal info (never the password or role).</summary>
    // [Admin only] Edits a user's personal details (name, email, phone, class, etc.).
    // It never changes the password or the role. Re-checks the email and role rules.
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateUserDto dto)
    {
        try
        {
            var user = await _context.Users.FindAsync(id);
            if (user is null)
                return NotFound(ApiResponse<UserResponseDto>.Fail("User not found."));

            if (dto.DateOfBirth > DateOnly.FromDateTime(DateTime.UtcNow))
                return BadRequest(ApiResponse<UserResponseDto>.Fail("Date of birth cannot be in the future."));

            var email = dto.Email.Trim();

            // Teacher/Admin must keep a school-domain email (the role isn't changed on update).
            if (RequiresSchoolDomain(user.Role) && !email.EndsWith(SchoolEmailDomain, StringComparison.OrdinalIgnoreCase))
                return BadRequest(ApiResponse<UserResponseDto>.Fail("Teacher and admin emails must use the @almanahilschool.com domain."));

            // Students and parents must keep a middle name.
            if (RequiresMiddleName(user.Role) && string.IsNullOrWhiteSpace(dto.MiddleName))
                return BadRequest(ApiResponse<UserResponseDto>.Fail("Middle name is required for students and parents."));

            // Photos are optional (the admin form no longer collects them). If one is ever
            // supplied via the API, reject only when it exceeds the size cap.
            if (!IsPhotoWithinLimit(dto.Photo))
                return BadRequest(ApiResponse<UserResponseDto>.Fail("The photo is too large. Please choose an image under 1.5 MB."));

            // Email must stay unique (excluding this same user).
            var emailClash = await _context.Users.AnyAsync(u => u.Id != id && u.Email.ToLower() == email.ToLower());
            if (emailClash)
                return BadRequest(ApiResponse<UserResponseDto>.Fail("A user with this email already exists."));

            // Students may be re-enrolled into another class; that class must exist.
            if (user.Role == UserRole.Student && dto.ClassId is not null)
            {
                if (!await _context.Classes.AnyAsync(c => c.Id == dto.ClassId.Value))
                    return BadRequest(ApiResponse<UserResponseDto>.Fail("Selected class not found."));
                user.ClassId = dto.ClassId.Value;
            }

            // Teacher level: required + valid for teachers; cleared (null) for everyone else.
            if (user.Role == UserRole.Teacher)
            {
                var normalizedLevel = AllowedTeacherLevels
                    .FirstOrDefault(l => l.Equals(dto.TeacherLevel?.Trim(), StringComparison.OrdinalIgnoreCase));
                if (normalizedLevel is null)
                    return BadRequest(ApiResponse<UserResponseDto>.Fail(
                        "Please choose the teacher's level (Secondary or High School)."));
                user.TeacherLevel = normalizedLevel;
            }
            else
            {
                user.TeacherLevel = null;
            }

            user.FirstName = dto.FirstName.Trim();
            user.MiddleName = Clean(dto.MiddleName);
            user.LastName = dto.LastName.Trim();
            user.Email = email;
            user.Gender = dto.Gender.Trim();
            user.DateOfBirth = dto.DateOfBirth;
            user.PhoneNumber = Clean(dto.PhoneNumber);
            user.Photo = Clean(dto.Photo);
            user.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            var result = await BuildUserResponseAsync(user.Id);
            return Ok(ApiResponse<UserResponseDto>.Ok(result!, "User updated successfully."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update user {UserId}.", id);
            return StatusCode(500,
                ApiResponse<UserResponseDto>.Fail("An error occurred while updating the user."));
        }
    }

    /// <summary>
    /// PUT /api/users/{id}/status — activate/deactivate a user (no hard delete). The main
    /// administrator account can never be deactivated (prevents locking everyone out).
    /// </summary>
    // [Admin only] Turns an account on or off (activate/deactivate) instead of deleting it.
    // The main administrator account can never be switched off (that would lock everyone out).
    [HttpPut("{id:int}/status")]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateUserStatusDto dto)
    {
        try
        {
            var user = await _context.Users.FindAsync(id);
            if (user is null)
                return NotFound(ApiResponse<UserResponseDto>.Fail("User not found."));

            if (!dto.IsActive && string.Equals(user.Email, MainAdminEmail, StringComparison.OrdinalIgnoreCase))
                return BadRequest(ApiResponse<UserResponseDto>.Fail("The main administrator account cannot be deactivated."));

            user.IsActive = dto.IsActive;
            user.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            var result = await BuildUserResponseAsync(user.Id);
            var message = user.IsActive ? "User activated successfully." : "User deactivated successfully.";
            return Ok(ApiResponse<UserResponseDto>.Ok(result!, message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update status for user {UserId}.", id);
            return StatusCode(500,
                ApiResponse<UserResponseDto>.Fail("An error occurred while updating the user's status."));
        }
    }

    /// <summary>
    /// DELETE /api/users/{id} — permanently remove a user account. This is irreversible;
    /// deactivation (PUT .../status) is the softer alternative. Several tables reference
    /// Users with DeleteBehavior.Restrict, so the user's direct links are removed first
    /// (parent–student links on either side, teacher–subject assignments, and any events
    /// they created), then the user — all in one SaveChanges, which EF runs as a single
    /// transaction (dependents first), so it either fully succeeds or changes nothing.
    ///
    /// Guards: neither the main administrator account nor the caller's OWN account can be
    /// deleted (prevents locking everyone — or yourself — out).
    /// </summary>
    // [Admin only] Permanently deletes a user (deactivation is the safer choice). It first
    // removes the rows that point at this user, then the user, all in one all-or-nothing save.
    // Guards: the main admin account and your own account can never be deleted.
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            // Step 1: Find the user (stop if there's no such user).
            var user = await _context.Users.FindAsync(id);
            if (user is null)
                return NotFound(ApiResponse<object>.Fail("User not found."));

            // Step 2: Block deleting the main admin or your own account.
            if (string.Equals(user.Email, MainAdminEmail, StringComparison.OrdinalIgnoreCase))
                return BadRequest(ApiResponse<object>.Fail("The main administrator account cannot be deleted."));

            var callerEmail = GetCallerEmail();
            if (callerEmail is not null && string.Equals(user.Email, callerEmail, StringComparison.OrdinalIgnoreCase))
                return BadRequest(ApiResponse<object>.Fail("You cannot delete your own account."));

            // Step 3: Remove everything that points at this user first (their parent links,
            // teacher assignments, and events they created) so the delete doesn't get blocked.
            // Remove the rows that reference this user (all Restrict FKs) so the delete does
            // not hit a foreign-key violation. A student's ClassId needs no cleanup — that FK
            // is SetNull and lives on the user row itself, which is going away anyway.
            var parentLinks = await _context.ParentStudents
                .Where(ps => ps.ParentId == id || ps.StudentId == id)
                .ToListAsync();
            _context.ParentStudents.RemoveRange(parentLinks);

            var teacherAssignments = await _context.TeacherSubjects
                .Where(ts => ts.TeacherId == id)
                .ToListAsync();
            _context.TeacherSubjects.RemoveRange(teacherAssignments);

            var authoredEvents = await _context.Events
                .Where(e => e.CreatedByUserId == id)
                .ToListAsync();
            _context.Events.RemoveRange(authoredEvents);

            // Step 4: Now remove the user, and save all these deletions together.
            _context.Users.Remove(user);

            await _context.SaveChangesAsync();

            return Ok(ApiResponse<object>.Ok(new { id }, "User deleted successfully."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete user {UserId}.", id);
            return StatusCode(500,
                ApiResponse<object>.Fail("An error occurred while deleting the user."));
        }
    }

    // ================= Helpers =================

    // Helper type: a small holder for the safe user fields (no password) while we prepare the response.
    /// <summary>Lightweight row shape used to pull safe columns before mapping in memory.</summary>
    private sealed class UserRow
    {
        public int Id { get; init; }
        public string FirstName { get; init; } = string.Empty;
        public string? MiddleName { get; init; }
        public string LastName { get; init; } = string.Empty;
        public string Email { get; init; } = string.Empty;
        public UserRole Role { get; init; }
        public string? Gender { get; init; }
        public DateOnly? DateOfBirth { get; init; }
        public string? PhoneNumber { get; init; }
        public int? ClassId { get; init; }
        public string? ClassName { get; init; }
        public string? TeacherLevel { get; init; }
        public string? Photo { get; init; }
        public bool IsActive { get; init; }
        public DateTime CreatedAt { get; init; }
    }

    // Helper: copies the safe user fields into the shape sent back to the website.
    /// <summary>Maps a pulled row to the public DTO (Role.ToString() runs in memory here).</summary>
    private static UserResponseDto MapToDto(UserRow u) => new()
    {
        Id = u.Id,
        FirstName = u.FirstName,
        MiddleName = u.MiddleName,
        LastName = u.LastName,
        FullName = NameFormatter.FullName(u.FirstName, u.MiddleName, u.LastName),
        Email = u.Email,
        Role = u.Role.ToString(),
        Gender = u.Gender,
        DateOfBirth = u.DateOfBirth,
        PhoneNumber = u.PhoneNumber,
        ClassId = u.ClassId,
        ClassName = u.ClassName,
        TeacherLevel = u.TeacherLevel,
        Photo = u.Photo,
        IsActive = u.IsActive,
        CreatedAt = u.CreatedAt
    };

    // Helper: loads one user's safe fields from the database and returns them ready to send back.
    /// <summary>Reads one user (safe columns only) and maps it to the public DTO.</summary>
    private async Task<UserResponseDto?> BuildUserResponseAsync(int id)
    {
        var row = await _context.Users
            .Where(u => u.Id == id)
            .Select(u => new UserRow
            {
                Id = u.Id,
                FirstName = u.FirstName,
                MiddleName = u.MiddleName,
                LastName = u.LastName,
                Email = u.Email,
                Role = u.Role,
                Gender = u.Gender,
                DateOfBirth = u.DateOfBirth,
                PhoneNumber = u.PhoneNumber,
                ClassId = u.ClassId,
                ClassName = u.Class != null ? u.Class.Name : null,
                TeacherLevel = u.TeacherLevel,
                Photo = u.Photo,
                IsActive = u.IsActive,
                CreatedAt = u.CreatedAt
            })
            .FirstOrDefaultAsync();

        return row is null ? null : MapToDto(row);
    }

    // Helper: trims spaces; if nothing is left, stores nothing (null) instead of "".
    /// <summary>Trim to null: turns empty/whitespace into null, otherwise trims.</summary>
    private static string? Clean(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    // Helper: true for Teacher/Admin, who must use a school email; false for Students/Parents.
    /// <summary>Teacher and Admin accounts must use the school email domain; Students/Parents may use any email.</summary>
    private static bool RequiresSchoolDomain(UserRole role) =>
        role is UserRole.Teacher or UserRole.Admin;

    // Helper: true for Students/Parents, who must give a middle name.
    /// <summary>Students and parents must supply a middle name (their full name uses it).</summary>
    private static bool RequiresMiddleName(UserRole role) =>
        role is UserRole.Student or UserRole.Parent;

    // Helper: true if there is no photo, or the photo is within the size limit (about 1.5 MB).
    /// <summary>True if the base64 photo is absent or within the ~1.5 MB image cap (base64 adds ~33%).</summary>
    private static bool IsPhotoWithinLimit(string? photo) =>
        string.IsNullOrEmpty(photo) || photo.Length <= 2_400_000;

    /// <summary>
    /// Reads the caller's email from the validated JWT. TokenService writes it as
    /// ClaimTypes.Email; we also check the short "email" claim and Identity.Name
    /// (NameClaimType is Email) so the lookup is robust to claim-type remapping.
    /// </summary>
    // Helper: reads the logged-in admin's email out of their sign-in token (checks a few labels).
    private string? GetCallerEmail() =>
        User.FindFirstValue(ClaimTypes.Email)
        ?? User.FindFirstValue(JwtRegisteredClaimNames.Email)
        ?? User.Identity?.Name;

    /// <summary>
    /// Generates a random temporary password: 10 chars, guaranteed at least one letter
    /// and one digit, using a cryptographic RNG. Ambiguous characters (0/O/1/l/I) are
    /// excluded for readability. Meets the app's password policy so first login works.
    /// </summary>
    // Helper: makes a random 10-character temporary password. It guarantees at least one
    // letter and one digit, and skips look-alike characters (0/O/1/l/I) so it's easy to read.
    private static string GenerateTemporaryPassword()
    {
        const string letters = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnpqrstuvwxyz"; // no I, O, l, o
        const string digits = "23456789";                                          // no 0, 1
        const string all = letters + digits;
        const int length = 10;

        var chars = new char[length];
        // Step 1: force at least one letter and one digit, then fill the rest randomly.
        // Guarantee the policy: at least one letter and one digit.
        chars[0] = letters[RandomNumberGenerator.GetInt32(letters.Length)];
        chars[1] = digits[RandomNumberGenerator.GetInt32(digits.Length)];
        for (var i = 2; i < length; i++)
            chars[i] = all[RandomNumberGenerator.GetInt32(all.Length)];

        // Step 2: shuffle the characters so the guaranteed letter/digit aren't always at the start.
        // Fisher–Yates shuffle so the guaranteed letter/digit aren't always at the front.
        for (var i = chars.Length - 1; i > 0; i--)
        {
            var j = RandomNumberGenerator.GetInt32(i + 1);
            (chars[i], chars[j]) = (chars[j], chars[i]);
        }

        return new string(chars);
    }

    /// <summary>
    /// Branded HTML for the new-account email (same navy/orange treatment as the
    /// password-reset email). Presents the username (email) + temporary password and
    /// asks the user to change it after first login. Inline styles only, for email
    /// clients. The password is emailed to the user only — never returned by the API.
    /// </summary>
    // Helper: builds the styled welcome email (as HTML) that shows the new user their email
    // and temporary password, and asks them to change it after first login.
    private static string BuildAccountEmailHtml(string firstName, string email, string tempPassword)
    {
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
  <table role=""presentation"" width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""background-color:#f3f4f6; padding:24px 12px;"">
    <tr>
      <td align=""center"">
        <table role=""presentation"" width=""600"" cellpadding=""0"" cellspacing=""0"" style=""width:100%; max-width:600px; background-color:#ffffff; border-radius:12px; overflow:hidden; box-shadow:0 6px 24px rgba(0,0,0,0.08); font-family:'Segoe UI', Arial, sans-serif;"">
          <tr>
            <td align=""center"" style=""background-color:#1E4C9A; padding:28px 24px;"">
              <span style=""color:#ffffff; font-size:22px; font-weight:700; letter-spacing:0.3px;"">{SchoolName}</span>
            </td>
          </tr>
          <tr>
            <td style=""height:4px; line-height:4px; font-size:0; background-color:#F2A03D;"">&nbsp;</td>
          </tr>
          <tr>
            <td style=""padding:36px 40px 8px 40px; color:#1f2937;"">
              <p style=""margin:0 0 16px 0; font-size:16px;"">{greeting}</p>
              <p style=""margin:0 0 22px 0; font-size:15px; line-height:1.6; color:#4b5563;"">
                An account has been created for you on the {SchoolName} system. You can sign in with the details below:
              </p>
            </td>
          </tr>
          <tr>
            <td style=""padding:0 40px;"">
              <table role=""presentation"" width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""background-color:#eef3fb; border:1px solid #d6e2f4; border-radius:12px;"">
                <tr>
                  <td style=""padding:16px 20px; font-size:14px; color:#4b5563;"">Email (username)</td>
                  <td align=""right"" style=""padding:16px 20px; font-size:14px; font-weight:700; color:#1E4C9A;"">{email}</td>
                </tr>
                <tr>
                  <td style=""border-top:1px solid #d6e2f4; padding:16px 20px; font-size:14px; color:#4b5563;"">Temporary password</td>
                  <td align=""right"" style=""border-top:1px solid #d6e2f4; padding:16px 20px; font-size:16px; font-weight:700; letter-spacing:1px; color:#1E4C9A;"">{tempPassword}</td>
                </tr>
              </table>
            </td>
          </tr>
          <tr>
            <td style=""padding:22px 40px 0 40px;"">
              <p style=""margin:0; font-size:14px; line-height:1.6; color:#b45309; background-color:#fef3c7; border-radius:8px; padding:12px 16px;"">
                For security reasons, please change your password after first login.
              </p>
            </td>
          </tr>
          <tr>
            <td style=""padding:28px 40px 0 40px;"">
              <div style=""border-top:1px solid #e5e7eb; font-size:0; line-height:0;"">&nbsp;</div>
            </td>
          </tr>
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
