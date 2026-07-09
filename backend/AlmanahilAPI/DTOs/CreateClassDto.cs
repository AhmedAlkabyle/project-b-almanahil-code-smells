// ============================================
// CreateClassDto — the form details for making a new class.
// A DTO is a simple 'data parcel' the website sends to the server.
// This one carries the year, level, grade, and section of the new class.
// ============================================
using System.ComponentModel.DataAnnotations;

namespace AlmanahilAPI.DTOs;

// Travels: website -> server. The "add class" form the admin fills in.
/// <summary>
/// Payload the admin sends to create a new class. The class Name is NOT sent by the
/// client — the controller auto-composes it from Grade + Section (e.g. "1/A"). The
/// controller also validates Level / Grade / Section against the allowed sets.
/// </summary>
public class CreateClassDto
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
    /// <summary>الصف — grade within the level: 1, 2, or 3. Nullable + [Required] for a friendly "required" error.</summary>
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
