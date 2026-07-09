// ============================================
// ParentDtos — the data parcels used by the parent's view of their children.
// A DTO is a simple 'data parcel' the website and server pass back and forth.
// This file holds the parcel that lists a parent's own children.
// ============================================
namespace AlmanahilAPI.DTOs;

// ==== Parent module DTOs =============================================================
// A parent views THEIR CHILDREN's data (read-only). Children are resolved from the
// ParentStudents join, scoped to the logged-in parent via the JWT.

// Travels: server -> website. One of the logged-in parent's children (for the child picker).
/// <summary>
/// One child linked to the logged-in parent (via ParentStudents), for the parent's child
/// picker. Includes the child's class so a parent can tell siblings apart at a glance.
/// </summary>
public class ParentChildDto
{
    // The child's (student's) id.
    public int StudentId { get; set; }
    // The child's full name.
    public string StudentName { get; set; } = string.Empty;
    // The id of the child's class (empty if none).
    public int? ClassId { get; set; }
    // The name of the child's class (empty if none).
    public string? ClassName { get; set; }
}
