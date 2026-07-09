// ============================================
// UpdateEventDto — the edited details for an announcement or event.
// A DTO is a simple 'data parcel' the website sends to the server.
// This one carries the changed title, message, date, type, and audience.
// ============================================
using System.ComponentModel.DataAnnotations;

namespace AlmanahilAPI.DTOs;

// Travels: website -> server. The "edit announcement/event" form the admin saves.
/// <summary>Payload the admin sends to update an existing announcement or event.</summary>
public class UpdateEventDto
{
    // The short headline/title (required).
    [Required(ErrorMessage = "Title is required.")]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    // The full message text (required).
    [Required(ErrorMessage = "Description is required.")]
    public string Description { get; set; } = string.Empty;

    // The day and time it happens.
    /// <summary>Scheduled date AND time (e.g. "2026-07-05T14:30").</summary>
    [Required(ErrorMessage = "Date is required.")]
    public DateTime Date { get; set; }

    // Whether this is an "Announcement" or an "Event".
    /// <summary>'Announcement' or 'Event' — the exact value is validated in the controller.</summary>
    [Required(ErrorMessage = "Type is required.")]
    [MaxLength(20)]
    public string Type { get; set; } = string.Empty;

    // Who should see this (e.g. everyone, all teachers, one class, etc.).
    /// <summary>
    /// The single audience this item targets (AllUsers, AllTeachers, TeachersSecondary,
    /// TeachersHighSchool, AllParents, AllStudents, AllSecondary, AllHighSchool,
    /// SpecificClass). Validated in the controller.
    /// </summary>
    [Required(ErrorMessage = "Audience is required.")]
    [MaxLength(40)]
    public string AudienceType { get; set; } = "AllUsers";

    // The one class to target, only used when the audience is a specific class.
    /// <summary>Required only when AudienceType == SpecificClass; ignored (forced null) otherwise.</summary>
    public int? TargetClassId { get; set; }
}
