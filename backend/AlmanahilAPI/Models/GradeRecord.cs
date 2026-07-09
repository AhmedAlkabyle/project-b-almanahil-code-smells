// ============================================
// GradeRecord - one student's mark for one subject on one assessment
// (e.g. a Quiz, Midterm, Final, or Homework score).
// A "model" is a plain C# class that maps to one database table. Each
// GradeRecord object is one row in the 'GradeRecords' table.
// ============================================
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AlmanahilAPI.Models;

/// <summary>
/// One student's mark for one subject on one assessment type. Grades are PER SUBJECT:
/// a teacher enters a mark for the students of the subject's class. The
/// (SubjectId, StudentId, AssessmentType) triple is unique (enforced in the DbContext) —
/// re-saving the same assessment updates the existing row instead of duplicating.
/// Mirrors the Attendance module (AttendanceRecord).
/// </summary>
// One grade/mark. This class maps to the 'GradeRecords' table.
public class GradeRecord
{
    // The unique ID number for this record. Filled in automatically by the
    // database. This is the "primary key".
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    // The ID of the subject this mark is for. A link (foreign key) to the
    // Subjects table.
    /// <summary>FK -> Subject (the subject this mark is for).</summary>
    [Required]
    public int SubjectId { get; set; }

    // The ID of the student. A link (foreign key) to the Users table.
    /// <summary>FK -> User (the student whose mark this is).</summary>
    [Required]
    public int StudentId { get; set; }

    // The kind of assessment: "Quiz", "Midterm", "Final", or "Homework".
    /// <summary>'Quiz', 'Midterm', 'Final', or 'Homework' — validated in the controller.</summary>
    [Required, MaxLength(20)]
    public string AssessmentType { get; set; } = string.Empty;

    // The score the student got (a number, e.g. 85).
    /// <summary>The score obtained (0..<see cref="MaxMark"/>).</summary>
    [Required]
    public decimal Mark { get; set; }

    // The highest possible score (defaults to 100), so a mark can show as 85/100.
    /// <summary>The maximum possible score (defaults to 100), so marks can display like 85/100.</summary>
    public decimal MaxMark { get; set; } = 100m;

    // An optional note or comment about the mark.
    /// <summary>Optional note / comment on the mark.</summary>
    [MaxLength(500)]
    public string? Note { get; set; }

    // The ID of the teacher (or admin) who saved this mark. A link (foreign
    // key) to the Users table.
    /// <summary>FK -> User (the teacher — or admin — who recorded / last updated this mark).</summary>
    [Required]
    public int RecordedByTeacherId { get; set; }

    // The date and time this record was created. Set automatically.
    public DateTime CreatedAt { get; set; }

    // The date and time this record was last changed. Set automatically.
    public DateTime UpdatedAt { get; set; }

    // ----- Navigation -----
    // "Navigation" links to the full related objects.
    public Subject? Subject { get; set; }

    public User? Student { get; set; }

    public User? RecordedByTeacher { get; set; }
}
