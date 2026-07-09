// ============================================
// ClassStudentDto — one student's short details inside a class.
// A DTO is a simple 'data parcel' the server sends to the website.
// This one carries just the id, name, and email of a student in a class.
// ============================================
namespace AlmanahilAPI.DTOs;

// Travels: server -> website. One row in the "students in this class" list.
/// <summary>
/// A single student enrolled in a class — the lightweight shape returned by
/// GET /api/classes/{classId}/students (id + full name + email only).
/// </summary>
public class ClassStudentDto
{
    // The student's database id.
    public int Id { get; set; }

    // The student's full name.
    public string FullName { get; set; } = string.Empty;

    // The student's email address.
    public string Email { get; set; } = string.Empty;
}
