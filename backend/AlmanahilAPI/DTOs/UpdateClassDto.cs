// ============================================
// UpdateClassDto — the edited details for an existing class.
// A DTO is a simple 'data parcel' the website sends to the server.
// This one carries the changed year, level, grade, and section.
// ============================================
using System.ComponentModel.DataAnnotations;

namespace AlmanahilAPI.DTOs;

// Travels: website -> server. The "edit class" form the admin saves.
/// <summary>
/// Payload the admin sends to update an existing class. Same structured fields as
/// <see cref="CreateClassDto"/>; the Name is re-composed by the controller from
/// Grade + Section.
/// </summary>
public class UpdateClassDto
{
    // The school year, like "2025/2026".
    /// <summary>العام الدراسي — academic year, e.g. "2025/2026".</summary>
    [Required(ErrorMessage = "Academic year is required.")]
    [MaxLength(9)]
    public string AcademicYear { get; set; } = string.Empty;

    // The school level: Secondary or HighSchool.
    /// <summary>المرحلة — "Secondary" (إعدادي) or "HighSchool" (ثانوي).</summary>
    [Required(ErrorMessage = "Level is required.")]
    [MaxLength(20)]
    public string Level { get; set; } = string.Empty;

    // The year number inside that level: 1, 2, or 3.
    /// <summary>الصف — grade within the level: 1, 2, or 3.</summary>
    [Required(ErrorMessage = "Year is required.")]
    [Range(1, 3, ErrorMessage = "Year must be 1, 2, or 3.")]
    public int? Grade { get; set; }

    // The class letter/section: A, B, C, or D.
    /// <summary>الشعبة — section: "A", "B", "C", or "D".</summary>
    [Required(ErrorMessage = "Section is required.")]
    [MaxLength(1)]
    public string Section { get; set; } = string.Empty;

    // Extra notes about the class (optional).
    /// <summary>Optional free-text description.</summary>
    [MaxLength(500)]
    public string? Description { get; set; }
}
