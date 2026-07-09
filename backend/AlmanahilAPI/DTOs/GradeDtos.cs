// ============================================
// GradeDtos — the data parcels for entering and viewing marks.
// A DTO is a simple 'data parcel' the website and server pass back and forth.
// This file groups the marks sheet, saving it, and grade reports.
// ============================================
using System.ComponentModel.DataAnnotations;

namespace AlmanahilAPI.DTOs;

// ==== Grades module DTOs =============================================================
// Grades are PER SUBJECT + assessment type: a teacher enters a mark for the students of
// the subject's class. Mirrors the Attendance DTOs. (The teacher's subject list reuses
// TeacherSubjectDto from AttendanceDtos.cs — same namespace.)

// Travels: server -> website. One saved mark, used in reports and student screens.
/// <summary>A single stored grade (used by the admin reports + student endpoints).</summary>
public class GradeRecordDto
{
    // The mark's id.
    public int Id { get; set; }
    // The subject it belongs to.
    public int SubjectId { get; set; }
    public string SubjectName { get; set; } = string.Empty;
    public string ClassName { get; set; } = string.Empty;
    // Which student this mark is for.
    public int StudentId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    // The kind of assessment (Quiz, Midterm, Final, Homework).
    public string AssessmentType { get; set; } = string.Empty;
    // The score the student got.
    public decimal Mark { get; set; }
    // The score the assessment is out of.
    public decimal MaxMark { get; set; }
    // Optional extra note.
    public string? Note { get; set; }
}

// Travels: website -> server. The whole filled-in marks sheet the teacher saves.
/// <summary>
/// The "enter marks" payload: an assessment type + one entry per student. The subject
/// comes from the route ({subjectId}); the SubjectId here is accepted but the route wins.
/// </summary>
public class SaveGradesDto
{
    // The subject id (the one in the web address is the one actually used).
    public int SubjectId { get; set; }

    // The kind of assessment these marks are for.
    /// <summary>'Quiz', 'Midterm', 'Final', or 'Homework' — validated in the controller.</summary>
    [Required, MaxLength(20)]
    public string AssessmentType { get; set; } = string.Empty;

    // The score everyone is marked out of (defaults to 100).
    /// <summary>The maximum score this assessment is out of (defaults to 100).</summary>
    public decimal MaxMark { get; set; } = 100m;

    // One entry per student (their mark).
    [Required]
    public List<SaveGradeEntryDto> Entries { get; set; } = [];
}

// Travels: website -> server. One student's mark inside the saved sheet above.
/// <summary>One student's mark (+ optional note) within a <see cref="SaveGradesDto"/>.</summary>
public class SaveGradeEntryDto
{
    // Which student this row is for.
    [Required]
    public int StudentId { get; set; }

    // The score this student got.
    public decimal Mark { get; set; }

    // Optional extra note for this student.
    [MaxLength(500)]
    public string? Note { get; set; }
}

// Travels: server -> website. The blank/filled marks sheet the teacher sees.
/// <summary>
/// The grade "sheet" returned when loading a subject for an assessment type: the subject +
/// its class, the assessment type, and every student with their current mark/note (Mark is
/// null when it hasn't been entered yet for that student).
/// </summary>
public class GradeSheetDto
{
    // The subject this sheet is for.
    public int SubjectId { get; set; }
    public string SubjectName { get; set; } = string.Empty;
    // The class this subject is taught in.
    public int ClassId { get; set; }
    public string ClassName { get; set; } = string.Empty;
    // The kind of assessment these marks are for.
    public string AssessmentType { get; set; } = string.Empty;
    // The score everyone is marked out of.
    public decimal MaxMark { get; set; }
    // One row per student in the class.
    public List<GradeSheetEntryDto> Students { get; set; } = [];
}

// Travels: server -> website. One student's row inside the marks sheet above.
/// <summary>One student row within a <see cref="GradeSheetDto"/>.</summary>
public class GradeSheetEntryDto
{
    // Which student this row is for.
    public int StudentId { get; set; }
    public string StudentName { get; set; } = string.Empty;

    // Their mark, or empty if not entered yet.
    /// <summary>Current mark for this assessment, or null if it hasn't been entered yet.</summary>
    public decimal? Mark { get; set; }

    // Optional extra note for this student.
    public string? Note { get; set; }
}
