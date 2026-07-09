// ============================================
// CreateSubjectDto — the form details for making a new subject.
// A DTO is a simple 'data parcel' the website sends to the server.
// This one carries the subject's name and which class it belongs to.
// ============================================
using System.ComponentModel.DataAnnotations;

namespace AlmanahilAPI.DTOs;

// Travels: website -> server. The "add subject" form the admin fills in.
/// <summary>Payload the admin sends to create a new subject.</summary>
public class CreateSubjectDto
{
    // The subject's name, e.g. "Mathematics" (required).
    [Required(ErrorMessage = "Subject name is required.")]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    // The class this subject is taught in.
    /// <summary>The class this subject belongs to. Existence is checked in the controller.</summary>
    [Range(1, int.MaxValue, ErrorMessage = "Please select a class.")]
    public int ClassId { get; set; }
}
