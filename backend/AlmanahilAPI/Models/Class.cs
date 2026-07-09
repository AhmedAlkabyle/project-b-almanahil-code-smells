// ============================================
// Class - one class/grade group in the school (e.g. "Grade 1 - Section A").
// A "model" is a plain C# class that maps to one database table. Each Class
// object is one row in the 'Classes' table.
// A class has many students and many subjects.
// ============================================
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AlmanahilAPI.Models;

/// <summary>
/// A class / grade group in the school, uniquely described by its academic year +
/// level + grade + section (e.g. Secondary, Grade 1, Section A, 2025/2026).
/// <see cref="Name"/> is the human-readable label composed from those parts. A class
/// has many students (Users whose <see cref="User.ClassId"/> points here) and many
/// Subjects.
/// </summary>
// One class in the school. This class maps to the 'Classes' table.
public class Class
{
    // The unique ID number for this class. The database fills it in
    // automatically. This is the "primary key".
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    /// <summary>
    /// Human-readable label (e.g. "Secondary - Grade 1 - A (2025/2026)"). Going forward this
    /// is auto-composed by the API from Level + Grade + Section + AcademicYear; kept as a
    /// stored column so existing rows and name-based lookups keep working.
    /// </summary>
    // The readable name of the class, e.g. "Secondary - Grade 1 - A (2025/2026)".
    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    // ----- Real class structure (Module 2 upgrade) -----
    // These four fields describe a class the way the school actually does. They are
    // NULLABLE on purpose so this migration can ADD them WITHOUT breaking any Class rows
    // that already exist (older rows simply get NULLs). The API layer enforces them as
    // REQUIRED when creating/editing a class — the same approach already used for
    // User.Gender / User.DateOfBirth.

    // The school year, e.g. "2025/2026". Optional in the database.
    /// <summary>العام الدراسي — academic year, e.g. "2025/2026". Required at the API layer.</summary>
    [MaxLength(9)]
    public string? AcademicYear { get; set; }

    /// <summary>
    /// المرحلة — school level. One of: "Secondary" (إعدادي) or "HighSchool" (ثانوي).
    /// Stored as a string key; the UI shows the localized (AR/EN) label. Required at the API layer.
    /// </summary>
    // The school level: "Secondary" or "HighSchool". Optional in the database.
    [MaxLength(20)]
    public string? Level { get; set; }

    // The grade number within the level (1, 2, or 3). Optional in the database.
    /// <summary>الصف — grade within the level: 1 (الأول), 2 (الثاني), or 3 (الثالث). Required at the API layer.</summary>
    public int? Grade { get; set; }

    // The section letter (A, B, C, or D). Optional in the database.
    /// <summary>الشعبة — section: "A" (أ), "B" (ب), "C" (ج), or "D" (د). Required at the API layer.</summary>
    [MaxLength(1)]
    public string? Section { get; set; }

    // An optional short description of the class.
    [MaxLength(500)]
    public string? Description { get; set; }

    // The date and time this class was created. Set automatically.
    public DateTime CreatedAt { get; set; }

    // ----- Navigation -----
    // These "navigation" links let us reach related rows in other tables.

    // All the students in this class. This is the "many" side: one class
    // has many students. Not a real column - it is filled from the Users table.
    /// <summary>Students assigned to this class (User.ClassId -> Class.Id).</summary>
    public ICollection<User> Students { get; set; } = new List<User>();

    // All the subjects taught in this class. One class has many subjects.
    /// <summary>Subjects taught in this class.</summary>
    public ICollection<Subject> Subjects { get; set; } = new List<Subject>();
}
