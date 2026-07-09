// ============================================
// CreateUserDto — the form details for making a new user.
// A DTO is a simple 'data parcel' the website sends to the server.
// This one carries the new person's name, email, role, photo, etc.
// ============================================
using System.ComponentModel.DataAnnotations;

namespace AlmanahilAPI.DTOs;

// Travels: website -> server. The "add new user" form the admin fills in.
/// <summary>
/// Payload the admin sends to create a new user. Always-required personal fields are
/// validated by data annotations; the role-specific fields (ClassId / ParentId /
/// PhoneNumber) are validated in the controller based on <see cref="Role"/>.
/// </summary>
public class CreateUserDto
{
    // The person's first name (required).
    [Required(ErrorMessage = "First name is required.")]
    [MaxLength(100)]
    public string FirstName { get; set; } = string.Empty;

    // Middle name (optional — can be left blank).
    /// <summary>Optional middle name.</summary>
    [MaxLength(100)]
    public string? MiddleName { get; set; }

    // The person's last name (required).
    [Required(ErrorMessage = "Last name is required.")]
    [MaxLength(100)]
    public string LastName { get; set; } = string.Empty;

    // The person's email address; also used to log in (required).
    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
    [MaxLength(256)]
    public string Email { get; set; } = string.Empty;

    // Male or female (required).
    [Required(ErrorMessage = "Gender is required.")]
    [MaxLength(20)]
    public string Gender { get; set; } = string.Empty;

    // The person's date of birth (required).
    /// <summary>Nullable + [Required] so a missing value gives a friendly error (not a silent default).</summary>
    [Required(ErrorMessage = "Date of birth is required.")]
    public DateOnly? DateOfBirth { get; set; }

    // Which kind of user this is: Teacher, Student, Parent, or Admin.
    /// <summary>One of: Teacher | Student | Parent | Admin. Parsed/validated in the controller.</summary>
    [Required(ErrorMessage = "Role is required.")]
    public string Role { get; set; } = string.Empty;

    // The person's profile picture, sent as text (base64). Needed for everyone except Admin.
    /// <summary>
    /// Personal photo as a base64 data URL. Required for every role EXCEPT Admin — that rule
    /// (and a size cap) is enforced in the controller so the message stays friendly and role-aware.
    /// </summary>
    public string? Photo { get; set; }

    // ----- Role-specific (validated in the controller based on Role) -----

    // The class to put this student in (used only for students).
    /// <summary>Student only: the class to enrol them in. Required for students.</summary>
    public int? ClassId { get; set; }

    // The parent account to connect this student to (used only for students).
    /// <summary>Student only: the existing Parent to link to this student. Required for students.</summary>
    public int? ParentId { get; set; }

    // A contact phone number (required for parents, optional for students).
    /// <summary>
    /// Parent: the parent's own phone number (required). Student: the parent/guardian
    /// contact number (optional). Ignored for Teacher/Admin.
    /// </summary>
    [MaxLength(20)]
    public string? PhoneNumber { get; set; }

    // For teachers only: which school level they teach (Secondary or HighSchool).
    /// <summary>
    /// Teacher only: the level they teach — "Secondary" or "HighSchool". Required for
    /// teachers (validated in the controller); ignored (stored null) for other roles.
    /// </summary>
    [MaxLength(20)]
    public string? TeacherLevel { get; set; }
}
