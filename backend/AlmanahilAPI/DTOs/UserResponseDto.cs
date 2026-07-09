// ============================================
// UserResponseDto — the user details the server sends back to the website.
// A DTO is a simple 'data parcel' the server sends to the website.
// This one carries safe, show-on-screen fields (never the password).
// ============================================
namespace AlmanahilAPI.DTOs;

// Travels: server -> website. One user's info for lists and the edit form.
/// <summary>
/// Shape returned to the client for a user. Intentionally NEVER carries the password
/// hash (or any password) — only safe, displayable fields.
/// </summary>
public class UserResponseDto
{
    // The user's database id.
    public int Id { get; set; }

    // Name parts are exposed (in addition to FullName) so the admin edit form can
    // hydrate exactly, without lossy re-splitting of the full name.
    public string FirstName { get; set; } = string.Empty;

    // Middle name (may be empty).
    public string? MiddleName { get; set; }

    public string LastName { get; set; } = string.Empty;

    // The whole name joined together, handy for showing in tables.
    /// <summary>First (+ Middle) + Last, assembled server-side (convenience for tables).</summary>
    public string FullName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    // Which kind of user this is.
    /// <summary>Role name, e.g. "Admin", "Teacher", "Student", "Parent".</summary>
    public string Role { get; set; } = string.Empty;

    // Male or female.
    public string? Gender { get; set; }

    // The user's date of birth.
    public DateOnly? DateOfBirth { get; set; }

    // A contact phone number.
    public string? PhoneNumber { get; set; }

    // The id of the class this student belongs to (empty for teachers/admins).
    public int? ClassId { get; set; }

    // The name of the class this student belongs to (looked up on the server).
    /// <summary>Owning class name (students only), resolved via a LINQ join to Classes.</summary>
    public string? ClassName { get; set; }

    // For teachers: the school level they teach (empty for other roles).
    /// <summary>Teacher only: the level they teach — "Secondary" or "HighSchool" (null for other roles).</summary>
    public string? TeacherLevel { get; set; }

    // A one-time starter password, only sent right after creating a user (so the admin can share it).
    /// <summary>
    /// The plain temporary password — populated ONLY in the create-user response, and ONLY
    /// when the caller is the main administrator (for account setup/testing). Null in every
    /// other response and for every other admin. Never persisted or logged.
    /// </summary>
    public string? TemporaryPassword { get; set; }

    // The profile picture as text (base64); empty if none.
    /// <summary>Personal photo as a base64 data URL (null if none). Drives the avatar in the users list + edit form.</summary>
    public string? Photo { get; set; }

    // True if the account is active; false if it has been switched off.
    public bool IsActive { get; set; }

    // When this user account was created.
    public DateTime CreatedAt { get; set; }
}
