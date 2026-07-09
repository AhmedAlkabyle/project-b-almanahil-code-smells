// ============================================
// ParentLinkResponseDto — a parent-child link sent back to the website.
// A DTO is a simple 'data parcel' the server sends to the website.
// This one carries which parent is connected to which student, for display.
// ============================================
namespace AlmanahilAPI.DTOs;

// Travels: server -> website. One "parent is linked to child" row for the list.
/// <summary>Shape returned to the client for a parent↔student link.</summary>
public class ParentLinkResponseDto
{
    // The link's database id.
    public int Id { get; set; }

    // The parent's id.
    public int ParentId { get; set; }

    // The parent's full name (looked up on the server).
    /// <summary>Parent's full name, resolved via a LINQ join to Users.</summary>
    public string ParentName { get; set; } = string.Empty;

    // The student's id.
    public int StudentId { get; set; }

    // The student's full name (looked up on the server).
    /// <summary>Student's full name, resolved via a LINQ join to Users.</summary>
    public string StudentName { get; set; } = string.Empty;

    // When this link was created.
    public DateTime CreatedAt { get; set; }
}
