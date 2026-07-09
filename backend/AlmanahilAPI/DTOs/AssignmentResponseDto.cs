// ============================================
// AssignmentResponseDto — a teacher-subject pairing sent back to the website.
// A DTO is a simple 'data parcel' the server sends to the website.
// This one carries who teaches what, plus the class name, for display.
// ============================================
namespace AlmanahilAPI.DTOs;

// Travels: server -> website. One "teacher teaches this subject" row for the list.
/// <summary>Shape returned to the client for a teacher-subject assignment.</summary>
public class AssignmentResponseDto
{
    // The assignment's database id.
    public int Id { get; set; }

    // The teacher's id.
    public int TeacherId { get; set; }

    // The teacher's full name (looked up on the server).
    /// <summary>Teacher's full name, resolved via a LINQ join to Users.</summary>
    public string TeacherName { get; set; } = string.Empty;

    // The subject's id.
    public int SubjectId { get; set; }

    // The subject's name (looked up on the server).
    /// <summary>Subject name, resolved via a LINQ join to Subjects.</summary>
    public string SubjectName { get; set; } = string.Empty;

    // The name of the class that subject belongs to (looked up on the server).
    /// <summary>Owning class name, resolved via Subject → Class.</summary>
    public string ClassName { get; set; } = string.Empty;

    // When this assignment was created.
    public DateTime CreatedAt { get; set; }
}
