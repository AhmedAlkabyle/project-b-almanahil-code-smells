// ============================================
// AttendanceRecord - one student's attendance for one subject on one day
// (Present, Absent, or Excused).
// A "model" is a plain C# class that maps to one database table. Each
// AttendanceRecord object is one row in the 'AttendanceRecords' table.
// ============================================
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AlmanahilAPI.Models;

/// <summary>
/// One student's attendance for one subject on one date. Attendance is PER SUBJECT:
/// a teacher records it for the students of the subject's class. The
/// (SubjectId, StudentId, Date) triple is unique (enforced in the DbContext) — re-saving
/// the same day updates the existing row instead of creating a duplicate.
/// </summary>
// One attendance mark. This class maps to the 'AttendanceRecords' table.
public class AttendanceRecord
{
    // The unique ID number for this record. Filled in automatically by the
    // database. This is the "primary key".
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    // The ID of the subject this attendance is for. A link (foreign key) to
    // the Subjects table.
    /// <summary>FK -> Subject (the subject attendance is taken for).</summary>
    [Required]
    public int SubjectId { get; set; }

    // The ID of the student. A link (foreign key) to the Users table.
    /// <summary>FK -> User (the student whose attendance this is).</summary>
    [Required]
    public int StudentId { get; set; }

    // The day this attendance is for (day only, no time). Required.
    /// <summary>The attendance date (day only, no time).</summary>
    [Required]
    public DateOnly Date { get; set; }

    // The attendance result: "Present", "Absent", or "Excused".
    /// <summary>'Present', 'Absent', or 'Excused' — the exact value is validated in the controller.</summary>
    [Required, MaxLength(20)]
    public string Status { get; set; } = "Present";

    // An optional note, e.g. the reason a student was excused.
    /// <summary>Optional note / reason (e.g. why a student is excused).</summary>
    [MaxLength(500)]
    public string? Note { get; set; }

    // The ID of the teacher (or admin) who saved this record. A link (foreign
    // key) to the Users table.
    /// <summary>FK -> User (the teacher — or admin — who recorded / last updated this record).</summary>
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
