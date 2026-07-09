// ============================================
// AttendanceDtos — the data parcels for taking and viewing attendance.
// A DTO is a simple 'data parcel' the website and server pass back and forth.
// This file groups the subject picker, the daily sheet, saving it, and reports.
// ============================================
using System.ComponentModel.DataAnnotations;

namespace AlmanahilAPI.DTOs;

// ==== Attendance module DTOs (Module: Attendance) ====================================
// Attendance is PER SUBJECT: a teacher records status for the students of the subject's
// class on a given date. These DTOs cover loading a sheet, saving it, and admin reports.

// Travels: server -> website. One subject in the teacher's "pick a subject" dropdown.
/// <summary>A subject the logged-in teacher is assigned to (for the attendance subject picker).</summary>
public class TeacherSubjectDto
{
    // The subject's id.
    public int SubjectId { get; set; }
    // The subject's name.
    public string SubjectName { get; set; } = string.Empty;
    // The id of the class that subject is taught in.
    public int ClassId { get; set; }
    // The name of that class.
    public string ClassName { get; set; } = string.Empty;
}

// Travels: server -> website. One saved attendance mark, used in admin reports.
/// <summary>A single stored attendance record (used by the admin reports endpoint).</summary>
public class AttendanceRecordDto
{
    // The record's id.
    public int Id { get; set; }
    // The subject it belongs to.
    public int SubjectId { get; set; }
    public string SubjectName { get; set; } = string.Empty;
    public string ClassName { get; set; } = string.Empty;
    // Which student this mark is for.
    public int StudentId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    // The day this mark is for.
    public DateOnly Date { get; set; }
    // Present, Absent, or Excused.
    public string Status { get; set; } = string.Empty;
    // Optional extra note.
    public string? Note { get; set; }
}

// Travels: website -> server. The whole filled-in attendance sheet the teacher saves.
/// <summary>
/// The "take attendance" payload: the date + one entry per student. The subject comes
/// from the route ({subjectId}); the SubjectId here is accepted but the route wins.
/// </summary>
public class SaveAttendanceDto
{
    // The subject id (the one in the web address is the one actually used).
    public int SubjectId { get; set; }

    // The day this attendance is for.
    [Required(ErrorMessage = "Date is required.")]
    public DateOnly Date { get; set; }

    // One entry per student (their status).
    [Required]
    public List<SaveAttendanceEntryDto> Entries { get; set; } = [];
}

// Travels: website -> server. One student's status inside the saved sheet above.
/// <summary>One student's status (+ optional note) within a <see cref="SaveAttendanceDto"/>.</summary>
public class SaveAttendanceEntryDto
{
    // Which student this row is for.
    [Required]
    public int StudentId { get; set; }

    // Present, Absent, or Excused.
    /// <summary>'Present', 'Absent', or 'Excused' — validated in the controller.</summary>
    [Required, MaxLength(20)]
    public string Status { get; set; } = "Present";

    // Optional extra note for this student.
    [MaxLength(500)]
    public string? Note { get; set; }
}

// Travels: server -> website. The blank/filled sheet the teacher sees for a chosen day.
/// <summary>
/// The attendance "sheet" returned when loading a subject for a date: the subject + its
/// class, the date, and every student in that class with their current status/note
/// (Status is null when attendance hasn't been taken for that student on that date).
/// </summary>
public class AttendanceSheetDto
{
    // The subject this sheet is for.
    public int SubjectId { get; set; }
    public string SubjectName { get; set; } = string.Empty;
    // The class this subject is taught in.
    public int ClassId { get; set; }
    public string ClassName { get; set; } = string.Empty;
    // The day this sheet is for.
    public DateOnly Date { get; set; }
    // One row per student in the class.
    public List<AttendanceSheetEntryDto> Students { get; set; } = [];
}

// Travels: server -> website. One student's row inside the sheet above.
/// <summary>One student row within an <see cref="AttendanceSheetDto"/>.</summary>
public class AttendanceSheetEntryDto
{
    // Which student this row is for.
    public int StudentId { get; set; }
    public string StudentName { get; set; } = string.Empty;

    // Their status for the day, or empty if not marked yet.
    /// <summary>Current status for this date, or null if attendance hasn't been taken yet.</summary>
    public string? Status { get; set; }

    // Optional extra note for this student.
    public string? Note { get; set; }
}
