// ============================================
// EventResponseDto — an announcement or event sent back to the website.
// A DTO is a simple 'data parcel' the server sends to the website.
// This one carries the title, message, date, audience, and who posted it.
// ============================================
namespace AlmanahilAPI.DTOs;

// Travels: server -> website. One announcement/event for lists and screens.
/// <summary>Shape returned to the client for an announcement or event.</summary>
public class EventResponseDto
{
    // The item's database id.
    public int Id { get; set; }

    // The short headline/title.
    public string Title { get; set; } = string.Empty;

    // The full message text.
    public string Description { get; set; } = string.Empty;

    // The day and time it happens.
    /// <summary>Scheduled date AND time (wall-clock, e.g. "2026-07-05T14:30:00").</summary>
    public DateTime Date { get; set; }

    // Whether this is an "Announcement" or an "Event".
    public string Type { get; set; } = string.Empty;

    // Who this is meant for (everyone, a role, a class, etc.).
    /// <summary>The single audience this item targets (see the allowed set in the controller).</summary>
    public string AudienceType { get; set; } = "AllUsers";

    // The id of the targeted class, only when the audience is one specific class.
    /// <summary>The targeted class id — only set when AudienceType == SpecificClass.</summary>
    public int? TargetClassId { get; set; }

    // The name of the targeted class (looked up on the server); empty otherwise.
    /// <summary>Short name of the targeted class (e.g. "1/A"), resolved via a LINQ join; null otherwise.</summary>
    public string? TargetClassName { get; set; }

    // The id of the admin who posted this.
    public int CreatedByUserId { get; set; }

    // The name of the admin who posted this (looked up on the server).
    /// <summary>Full name of the admin who created this item, resolved via a LINQ join to Users.</summary>
    public string CreatedByName { get; set; } = string.Empty;

    // When this item was created.
    public DateTime CreatedAt { get; set; }
}
