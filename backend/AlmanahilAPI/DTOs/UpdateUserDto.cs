// ============================================
// UpdateUserDto — the edited details for an existing user.
// A DTO is a simple 'data parcel' the website sends to the server.
// This one carries the changed name, email, photo, etc. for one user.
// ============================================
using System.ComponentModel.DataAnnotations;

namespace AlmanahilAPI.DTOs;

// Travels: website -> server. The "edit user" form the admin saves.
/// <summary>
/// Payload the admin sends to update a user's personal info. Deliberately minimal:
/// no password change (that's the user's own first-login/forgot-password flow) and no
/// role change (roles wire up parent links, class enrolment, etc. at creation time).
/// </summary>
public class UpdateUserDto
{
    // The person's first name (required).
    [Required(ErrorMessage = "First name is required.")]
    [MaxLength(100)]
    public string FirstName { get; set; } = string.Empty;

    // Middle name (optional).
    [MaxLength(100)]
    public string? MiddleName { get; set; }

    // The person's last name (required).
    [Required(ErrorMessage = "Last name is required.")]
    [MaxLength(100)]
    public string LastName { get; set; } = string.Empty;

    // The person's email address (required).
    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
    [MaxLength(256)]
    public string Email { get; set; } = string.Empty;

    // Male or female (required).
    [Required(ErrorMessage = "Gender is required.")]
    [MaxLength(20)]
    public string Gender { get; set; } = string.Empty;

    // The person's date of birth (required).
    [Required(ErrorMessage = "Date of birth is required.")]
    public DateOnly? DateOfBirth { get; set; }

    // A contact phone number (optional here).
    /// <summary>Parent phone / student guardian contact. Optional here.</summary>
    [MaxLength(20)]
    public string? PhoneNumber { get; set; }

    // For students only: move them into a different class.
    /// <summary>Student only: re-enrol in a different class. Existence checked in the controller.</summary>
    public int? ClassId { get; set; }

    // For teachers only: which school level they teach (Secondary or HighSchool).
    /// <summary>
    /// Teacher only: the level they teach — "Secondary" or "HighSchool". Required for
    /// teachers (validated in the controller); ignored (stored null) for other roles.
    /// </summary>
    [MaxLength(20)]
    public string? TeacherLevel { get; set; }

    // The profile picture as text (base64). Sending it back unchanged keeps it; blank clears it.
    /// <summary>
    /// Personal photo as a base64 data URL. The form pre-loads the existing photo, so an
    /// untouched edit re-sends it (preserving it); an empty value clears it. Required for
    /// non-admins — enforced in the controller.
    /// </summary>
    public string? Photo { get; set; }
}
