// ============================================
// EnrollStudentsDto — the list of students to add to a class.
// A DTO is a simple 'data parcel' the website sends to the server.
// This one carries the ids of the students to move into a class.
// ============================================
using System.ComponentModel.DataAnnotations;

namespace AlmanahilAPI.DTOs;

// Travels: website -> server. Which students to place into a class (class id is in the web address).
/// <summary>
/// Payload for enrolling one or more students into a class:
/// POST /api/classes/{classId}/students. The target class id comes from the route,
/// so the body only carries the ids of the students to move into that class.
/// </summary>
public class EnrollStudentsDto
{
    // The list of student ids to add; at least one must be chosen.
    /// <summary>Ids of the students to enroll. Must contain at least one id.</summary>
    [Required(ErrorMessage = "Please select at least one student.")]
    [MinLength(1, ErrorMessage = "Please select at least one student.")]
    public List<int> StudentIds { get; set; } = [];
}
