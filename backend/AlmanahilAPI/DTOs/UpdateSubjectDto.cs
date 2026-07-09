// ============================================
// UpdateSubjectDto — the edited details for an existing subject.
// A DTO is a simple 'data parcel' the website sends to the server.
// This one carries the changed subject name and its class.
// ============================================
using System.ComponentModel.DataAnnotations;

namespace AlmanahilAPI.DTOs;

// Travels: website -> server. The "edit subject" form the admin saves.
/// <summary>Payload the admin sends to update an existing subject.</summary>
public class UpdateSubjectDto
{
    // The subject's name (required).
    [Required(ErrorMessage = "Subject name is required.")]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    // The class this subject is taught in.
    /// <summary>The class this subject belongs to. Existence is checked in the controller.</summary>
    [Range(1, int.MaxValue, ErrorMessage = "Please select a class.")]
    public int ClassId { get; set; }
}
