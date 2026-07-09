// ============================================
// User - one person in the system (admin, teacher, student, or parent).
// A "model" is a plain C# class that maps to one database table. Each
// User object is one row in the 'Users' table.
// Holds their name, email, scrambled password, role, and links to their class.
// ============================================
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AlmanahilAPI.Models;

// A simple list of the four kinds of people the app supports.
// Each name is saved as a number in the database (Admin = 0, Teacher = 1, etc.).
public enum UserRole
{
    Admin = 0,
    Teacher = 1,
    Student = 2,
    Parent = 3
}

// One person in the school. This class maps to the 'Users' table.
public class User
{
    // The unique ID number for this user. The database fills it in
    // automatically (1, 2, 3, ...). This is the "primary key".
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    // The person's first name. Required, up to 100 letters.
    [Required, MaxLength(100)]
    public string FirstName { get; set; } = string.Empty;

    // The person's last name. Required, up to 100 letters.
    [Required, MaxLength(100)]
    public string LastName { get; set; } = string.Empty;

    // Middle name. The "?" means it can be empty (optional).
    /// <summary>Optional middle name (collected by the admin Add-User form).</summary>
    [MaxLength(100)]
    public string? MiddleName { get; set; }

    // The email address, used to log in. Required.
    [Required, MaxLength(256)]
    public string Email { get; set; } = string.Empty;

    // Scrambled (hashed) password - never the real one.
    // We only store a jumbled version so nobody can read the real password.
    [Required]
    public string PasswordHash { get; set; } = string.Empty;

    // What kind of user this is (Admin, Teacher, Student, or Parent).
    [Required]
    public UserRole Role { get; set; }

    // Phone number. Optional (can be empty).
    [MaxLength(20)]
    public string? PhoneNumber { get; set; }

    // ----- Personal info (Module 2 Add-User form) -----
    // Nullable on purpose: this migration must NOT break existing Module 1 rows,
    // which have no gender/DOB. New users created by the admin always supply these
    // (the API layer will enforce it later).

    // The person's gender. Optional (older records may not have it).
    [MaxLength(20)]
    public string? Gender { get; set; }

    // Date of birth (day only, no time). Optional.
    public DateOnly? DateOfBirth { get; set; }

    /// <summary>
    /// Personal photo as a base64 data URL (e.g. "data:image/png;base64,..."), uploaded by the
    /// admin on the Add/Edit-User form. Stored as unbounded text. Nullable: admin accounts (and
    /// any legacy rows) may have none — every other role is required to have one, enforced in the
    /// API layer (not the schema) so this migration never breaks existing data.
    /// </summary>
    // The person's photo, saved as text. Optional (admins may have none).
    public string? Photo { get; set; }

    /// <summary>
    /// The school level a Teacher works in: "Secondary" (إعدادي) or "HighSchool" (ثانوي).
    /// Nullable on purpose: only Teachers have it (null for every other role and for any
    /// legacy row). Required at the API layer for teachers — not the schema — so this
    /// migration never breaks existing data.
    /// </summary>
    // For teachers only: which school level they teach. Empty for other roles.
    [MaxLength(20)]
    public string? TeacherLevel { get; set; }

    // ----- Class link (students only) -----
    // The ID of the class this student is in. A link (foreign key) to the
    // Classes table. Empty for teachers/admins/parents.
    /// <summary>FK -> Class for a Student; null for non-students.</summary>
    public int? ClassId { get; set; }

    // The actual Class object this student belongs to. This is a "navigation
    // link": it lets us reach the whole class from a user. Empty for non-students.
    /// <summary>The class this student belongs to (null for non-students).</summary>
    public Class? Class { get; set; }

    // True if the account is switched on. New users start as active.
    public bool IsActive { get; set; } = true;

    // True until the user logs in for the first time (used to force a
    // password change on first login).
    public bool IsFirstLogin { get; set; } = true;

    // The date and time this user was created. Set automatically.
    public DateTime CreatedAt { get; set; }

    // The date and time this user was last edited. Empty until first edit.
    public DateTime? UpdatedAt { get; set; }

    // ----- Password reset (forgot-password flow) -----
    // These two fields are only populated while a password-reset request is
    // pending. They are set when a user requests a reset code and cleared once
    // the code has been used (or has expired). Both are null the rest of the time.

    /// <summary>
    /// The 6-digit reset code, stored HASHED (never in plain text) — same
    /// approach as PasswordHash. Null when no reset is in progress.
    /// </summary>
    // A one-time code for "forgot my password", also scrambled (hashed).
    // Empty when the user is not resetting their password.
    [MaxLength(256)]
    public string? ResetCode { get; set; }

    /// <summary>
    /// UTC timestamp after which <see cref="ResetCode"/> is no longer valid.
    /// Null when no reset is in progress.
    /// </summary>
    // The time when the reset code stops working (expires). Empty when
    // no reset is in progress.
    public DateTime? ResetCodeExpiresAt { get; set; }
}
