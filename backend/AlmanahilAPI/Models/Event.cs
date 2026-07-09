// ============================================
// Event - one announcement or event created by an admin and shown to users.
// A "model" is a plain C# class that maps to one database table. Each Event
// object is one row in the 'Events' table.
// ============================================
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AlmanahilAPI.Models;

/// <summary>
/// An announcement or event published by an admin and shown to users.
/// <see cref="Type"/> is either 'Announcement' or 'Event'.
/// </summary>
// One announcement/event. This class maps to the 'Events' table.
public class Event
{
    // The unique ID number for this item. Filled in automatically by the
    // database. This is the "primary key".
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    // The title/headline of the announcement or event. Required.
    [Required, MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    // The full text/details. Required.
    [Required]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Scheduled date AND time of the event / announcement. Stored as a plain wall-clock
    /// value ('timestamp without time zone', configured in the DbContext) — an event at
    /// 14:30 stays 14:30 with no timezone conversion.
    /// </summary>
    // The date and time the event happens. Required.
    [Required]
    public DateTime Date { get; set; }

    // Which kind of item this is: "Announcement" or "Event".
    /// <summary>'Announcement' or 'Event'.</summary>
    [Required, MaxLength(20)]
    public string Type { get; set; } = "Event";

    /// <summary>
    /// Who this item targets — exactly ONE audience. One of:
    /// AllUsers, AllTeachers, TeachersSecondary, TeachersHighSchool, AllParents,
    /// AllStudents, AllSecondary, AllHighSchool, SpecificClass.
    /// The exact value is validated in the controller; existing rows default to AllUsers.
    /// </summary>
    // Who should see this item (e.g. all users, all teachers, one class).
    [Required, MaxLength(40)]
    public string AudienceType { get; set; } = "AllUsers";

    /// <summary>
    /// FK -> Class. Set ONLY when <see cref="AudienceType"/> == "SpecificClass"
    /// (e.g. an exam announcement for class 1/A); null for every other audience.
    /// </summary>
    // The ID of one specific class this item is for. A link (foreign key) to
    // the Classes table. Empty unless the audience is a single class.
    public int? TargetClassId { get; set; }

    // The ID of the admin who created this item. A link (foreign key) to the
    // Users table.
    /// <summary>FK -> User (the admin who created this item).</summary>
    [Required]
    public int CreatedByUserId { get; set; }

    // The date and time this item was created. Set automatically.
    public DateTime CreatedAt { get; set; }

    // ----- Navigation -----
    // A "navigation" link to the full admin user who created this item.
    public User? CreatedBy { get; set; }

    // A "navigation" link to the targeted class (only used for a single class).
    /// <summary>The targeted class (only when AudienceType == SpecificClass).</summary>
    public Class? TargetClass { get; set; }
}
