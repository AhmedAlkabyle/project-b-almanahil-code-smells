// ============================================
// CreateAssignmentDto — the parcel to give a teacher a subject to teach.
// A DTO is a simple 'data parcel' the website sends to the server.
// This one carries which teacher is being paired with which subject.
// ============================================
using System.ComponentModel.DataAnnotations;

namespace AlmanahilAPI.DTOs;

// Travels: website -> server. The admin's "assign teacher to subject" choice.
/// <summary>Payload the admin sends to assign a teacher to a subject.</summary>
public class CreateAssignmentDto
{
    // The teacher being assigned.
    /// <summary>The teacher (a User with role Teacher). Existence/role is checked in the controller.</summary>
    [Range(1, int.MaxValue, ErrorMessage = "Please select a teacher.")]
    public int TeacherId { get; set; }

    // The subject to give that teacher.
    /// <summary>The subject to assign. Existence is checked in the controller.</summary>
    [Range(1, int.MaxValue, ErrorMessage = "Please select a subject.")]
    public int SubjectId { get; set; }
}
