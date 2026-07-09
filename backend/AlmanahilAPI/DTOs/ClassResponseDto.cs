// ============================================
// ClassResponseDto — the class details the server sends back to the website.
// A DTO is a simple 'data parcel' the server sends to the website.
// This one carries a class's name, level, grade, section, and student count.
// ============================================
namespace AlmanahilAPI.DTOs;

// Travels: server -> website. One class's info for lists and screens.
/// <summary>Shape returned to the client for a class.</summary>
public class ClassResponseDto
{
    // The class's database id.
    public int Id { get; set; }

    // Short label built from grade + section, e.g. "1/A".
    /// <summary>Short label auto-composed from Grade + Section, e.g. "1/A".</summary>
    public string Name { get; set; } = string.Empty;

    // The school year, like "2025/2026".
    /// <summary>العام الدراسي — academic year, e.g. "2025/2026" (null on legacy rows).</summary>
    public string? AcademicYear { get; set; }

    // The school level: Secondary or HighSchool.
    /// <summary>المرحلة — "Secondary" or "HighSchool" (null on legacy rows).</summary>
    public string? Level { get; set; }

    // The year number inside that level: 1, 2, or 3.
    /// <summary>الصف — grade 1..3 (null on legacy rows).</summary>
    public int? Grade { get; set; }

    // The class letter/section: A to D.
    /// <summary>الشعبة — section "A".."D" (null on legacy rows).</summary>
    public string? Section { get; set; }

    // A longer, friendly label for showing on screen.
    /// <summary>
    /// Nicely composed label, e.g. "1/A — الأول إعدادي 2025/2026". Falls back to
    /// <see cref="Name"/> for legacy rows that don't have the structured parts yet.
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    // Extra notes about the class.
    public string? Description { get; set; }

    // How many students are in this class.
    /// <summary>Number of Users with role Student linked to this class (ClassId).</summary>
    public int StudentsCount { get; set; }

    // When this class was created.
    public DateTime CreatedAt { get; set; }
}
