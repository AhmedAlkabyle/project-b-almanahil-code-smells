// ============================================
// AttendanceController — records who was present, absent, or excused.
// Job: teachers take attendance for their subject on a chosen date; students and
// parents can look back at the records. Admins can run reports across the school.
// It always trusts the logged-in user from their sign-in token, not the request body.
// Used by: the Vue attendance pages (teacher take-attendance, student/parent history).
// ============================================
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using AlmanahilAPI.Data;
using AlmanahilAPI.DTOs;
using AlmanahilAPI.Helpers;
using AlmanahilAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AlmanahilAPI.Controllers;

/// <summary>
/// Attendance (Module: Attendance). Attendance is recorded PER SUBJECT for the students
/// of that subject's class on a given date. EF Core + LINQ only, every response uses the
/// standard ApiResponse envelope.
///
/// RBAC:
///  • Load / Save a subject's sheet → the TEACHER assigned to that subject (checked via
///    TeacherSubjects) OR an Admin. A teacher not assigned → 403.
///  • Reports → Admin only.
/// The acting user is always taken from the validated JWT (never the request body).
/// </summary>
// This class manages all attendance records (present/absent/excused) for each subject.
// [Authorize] here means: you must be signed in to reach ANY endpoint below.
[ApiController]
[Route("api/attendance")]
[Authorize]
public class AttendanceController(AlmanahilDbContext context, ILogger<AttendanceController> logger) : ControllerBase
{
    private readonly AlmanahilDbContext _context = context;
    private readonly ILogger<AttendanceController> _logger = logger;

    /// <summary>The only Status values the UI offers.</summary>
    private static readonly string[] AllowedStatuses = ["Present", "Absent", "Excused"];

    /// <summary>
    /// GET /api/attendance/my-subjects — the subjects assigned to the logged-in teacher
    /// (from TeacherSubjects), so the teacher can pick one to take attendance for. Admins
    /// have no TeacherSubjects, so they simply get an empty list here.
    /// </summary>
    // [Any signed-in user] Gives the logged-in teacher the list of subjects they teach,
    // so they can pick one to take attendance for. (Admins/others get an empty list.)
    [HttpGet("my-subjects")]
    public async Task<IActionResult> MySubjects()
    {
        try
        {
            if (!TryGetUserId(out var userId))
                return Unauthorized(ApiResponse<List<TeacherSubjectDto>>.Fail("Invalid or missing authentication token."));

            var subjects = await _context.TeacherSubjects
                .Where(ts => ts.TeacherId == userId)
                .OrderBy(ts => ts.Subject!.Class!.Name).ThenBy(ts => ts.Subject!.Name)
                .Select(ts => new TeacherSubjectDto
                {
                    SubjectId = ts.SubjectId,
                    SubjectName = ts.Subject!.Name,
                    ClassId = ts.Subject.ClassId,
                    ClassName = ts.Subject.Class!.Name
                })
                .ToListAsync();

            return Ok(ApiResponse<List<TeacherSubjectDto>>.Ok(subjects, "Your subjects retrieved successfully."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve the teacher's subjects.");
            return StatusCode(500, ApiResponse<List<TeacherSubjectDto>>.Fail("An error occurred while loading your subjects."));
        }
    }

    /// <summary>
    /// GET /api/attendance/my — the logged-in student's OWN attendance records across all
    /// subjects, newest first. Scoped strictly by the JWT id (a.StudentId == callerId), so a
    /// student can only ever see their own attendance — never anyone else's (RBAC by identity).
    /// Any authenticated user may call it; they simply receive the rows where they are the
    /// student (naturally empty for teachers/admins).
    /// </summary>
    // [Any signed-in user] Shows the logged-in student their OWN attendance history.
    // It only ever returns rows belonging to the caller, so no one sees another person's.
    [HttpGet("my")]
    public async Task<IActionResult> My()
    {
        try
        {
            if (!TryGetUserId(out var userId))
                return Unauthorized(ApiResponse<List<AttendanceRecordDto>>.Fail("Invalid or missing authentication token."));

            // Project the raw name parts in SQL, then build the full name in memory
            // (NameFormatter can't be translated inside an IQueryable.Select).
            var rows = await _context.AttendanceRecords
                .Where(a => a.StudentId == userId)
                .OrderByDescending(a => a.Date)
                .ThenBy(a => a.Subject!.Name)
                .Select(a => new
                {
                    a.Id,
                    a.SubjectId,
                    SubjectName = a.Subject!.Name,
                    ClassName = a.Subject.Class!.Name,
                    a.StudentId,
                    a.Student!.FirstName,
                    a.Student.MiddleName,
                    a.Student.LastName,
                    a.Date,
                    a.Status,
                    a.Note
                })
                .ToListAsync();

            var records = rows.Select(a => new AttendanceRecordDto
            {
                Id = a.Id,
                SubjectId = a.SubjectId,
                SubjectName = a.SubjectName,
                ClassName = a.ClassName,
                StudentId = a.StudentId,
                StudentName = NameFormatter.FullName(a.FirstName, a.MiddleName, a.LastName),
                Date = a.Date,
                Status = a.Status,
                Note = a.Note
            }).ToList();

            return Ok(ApiResponse<List<AttendanceRecordDto>>.Ok(records, "Your attendance retrieved successfully."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve the student's own attendance.");
            return StatusCode(500, ApiResponse<List<AttendanceRecordDto>>.Fail("An error occurred while loading your attendance."));
        }
    }

    /// <summary>
    /// GET /api/attendance/student/{studentId} — a student's attendance across all subjects,
    /// newest first. RBAC: Admin, the student themselves, or that student's parent (via
    /// ParentStudents). Mirrors GradesController.ForStudent — a parent can only ever pull
    /// their own child's records, never some other student's.
    /// </summary>
    // [Admin, the student themselves, or that student's parent] Shows one student's full
    // attendance history. A parent can only open their own child's records.
    [HttpGet("student/{studentId:int}")]
    public async Task<IActionResult> ForStudent(int studentId)
    {
        try
        {
            // RBAC: admin bypasses; otherwise caller must be the student or their parent.
            if (!User.IsInRole(nameof(UserRole.Admin)))
            {
                if (!TryGetUserId(out var callerId))
                    return Unauthorized(ApiResponse<List<AttendanceRecordDto>>.Fail("Invalid or missing authentication token."));

                if (callerId != studentId)
                {
                    var isParent = await _context.ParentStudents
                        .AnyAsync(ps => ps.ParentId == callerId && ps.StudentId == studentId);
                    if (!isParent)
                        return StatusCode(StatusCodes.Status403Forbidden,
                            ApiResponse<List<AttendanceRecordDto>>.Fail("You are not allowed to view this student's attendance."));
                }
            }

            // Project the raw name parts in SQL, then build the full name in memory
            // (NameFormatter can't be translated inside an IQueryable.Select).
            var rows = await _context.AttendanceRecords
                .Where(a => a.StudentId == studentId)
                .OrderByDescending(a => a.Date)
                .ThenBy(a => a.Subject!.Name)
                .Select(a => new
                {
                    a.Id,
                    a.SubjectId,
                    SubjectName = a.Subject!.Name,
                    ClassName = a.Subject.Class!.Name,
                    a.StudentId,
                    a.Student!.FirstName,
                    a.Student.MiddleName,
                    a.Student.LastName,
                    a.Date,
                    a.Status,
                    a.Note
                })
                .ToListAsync();

            var records = rows.Select(a => new AttendanceRecordDto
            {
                Id = a.Id,
                SubjectId = a.SubjectId,
                SubjectName = a.SubjectName,
                ClassName = a.ClassName,
                StudentId = a.StudentId,
                StudentName = NameFormatter.FullName(a.FirstName, a.MiddleName, a.LastName),
                Date = a.Date,
                Status = a.Status,
                Note = a.Note
            }).ToList();

            return Ok(ApiResponse<List<AttendanceRecordDto>>.Ok(records, "Student attendance retrieved successfully."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve attendance for student {StudentId}.", studentId);
            return StatusCode(500, ApiResponse<List<AttendanceRecordDto>>.Fail("An error occurred while retrieving the attendance."));
        }
    }

    /// <summary>
    /// GET /api/attendance/subject/{subjectId}?date=YYYY-MM-DD
    /// Returns the subject, its class's students, and each student's existing status+note
    /// for the date (Status null where attendance hasn't been taken yet). Defaults to today.
    /// </summary>
    // [Teacher assigned to this subject, or Admin] Builds the attendance sheet: the class's
    // students plus each one's already-saved status/note for the chosen date (blank if not
    // taken yet). Defaults to today's date when none is given.
    [HttpGet("subject/{subjectId:int}")]
    public async Task<IActionResult> GetSheet(int subjectId, [FromQuery] DateOnly? date)
    {
        try
        {
            var theDate = date ?? DateOnly.FromDateTime(DateTime.Today);

            var subject = await _context.Subjects
                .Where(s => s.Id == subjectId)
                .Select(s => new { s.Id, s.Name, s.ClassId, ClassName = s.Class!.Name })
                .FirstOrDefaultAsync();

            if (subject is null)
                return NotFound(ApiResponse<AttendanceSheetDto>.Fail("Subject not found."));

            // RBAC: the assigned teacher or an admin.
            var access = await AuthorizeSubjectAsync(subjectId);
            if (access is not null) return access;

            // The class's students, ordered by name. Pull the raw name parts, then build the
            // full name in memory (NameFormatter can't be translated inside an IQueryable.Select).
            var studentRows = await _context.Users
                .Where(u => u.ClassId == subject.ClassId && u.Role == UserRole.Student)
                .OrderBy(u => u.FirstName).ThenBy(u => u.LastName)
                .Select(u => new { u.Id, u.FirstName, u.MiddleName, u.LastName })
                .ToListAsync();

            var students = studentRows.Select(u => new
            {
                u.Id,
                Name = NameFormatter.FullName(u.FirstName, u.MiddleName, u.LastName)
            }).ToList();

            // Existing records for this subject + date, keyed by student.
            var existing = await _context.AttendanceRecords
                .Where(a => a.SubjectId == subjectId && a.Date == theDate)
                .ToListAsync();
            var byStudent = existing.ToDictionary(a => a.StudentId);

            var sheet = new AttendanceSheetDto
            {
                SubjectId = subject.Id,
                SubjectName = subject.Name,
                ClassId = subject.ClassId,
                ClassName = subject.ClassName,
                Date = theDate,
                Students = students.Select(s => new AttendanceSheetEntryDto
                {
                    StudentId = s.Id,
                    StudentName = s.Name,
                    Status = byStudent.TryGetValue(s.Id, out var rec) ? rec.Status : null,
                    Note = byStudent.TryGetValue(s.Id, out var rec2) ? rec2.Note : null
                }).ToList()
            };

            return Ok(ApiResponse<AttendanceSheetDto>.Ok(sheet, "Attendance sheet retrieved successfully."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load attendance sheet for subject {SubjectId}.", subjectId);
            return StatusCode(500, ApiResponse<AttendanceSheetDto>.Fail("An error occurred while loading attendance."));
        }
    }

    /// <summary>
    /// PUT /api/attendance/subject/{subjectId} — upsert attendance for each student for the
    /// given date (update if a row exists for subject+student+date, else create). Only the
    /// assigned teacher (or an admin) may save; RecordedByTeacherId comes from the JWT.
    /// </summary>
    // [Teacher assigned to this subject, or Admin] Saves the attendance sheet for one
    // subject on one date. For each student it updates the existing row or creates a new one
    // ("upsert"). The whole save runs as one all-or-nothing transaction.
    [HttpPut("subject/{subjectId:int}")]
    public async Task<IActionResult> Save(int subjectId, [FromBody] SaveAttendanceDto dto)
    {
        try
        {
            // Step 1: Make sure the subject exists.
            var subject = await _context.Subjects
                .Where(s => s.Id == subjectId)
                .Select(s => new { s.Id, s.ClassId })
                .FirstOrDefaultAsync();

            if (subject is null)
                return NotFound(ApiResponse<object>.Fail("Subject not found."));

            // RBAC: the assigned teacher or an admin.
            var access = await AuthorizeSubjectAsync(subjectId);
            if (access is not null) return access;

            // Step 2: Find out who is saving this (taken from their sign-in token, for trust).
            // The recorder is the logged-in user (from the JWT), never the request body.
            if (!TryGetUserId(out var recorderId))
                return Unauthorized(ApiResponse<object>.Fail("Invalid or missing authentication token."));

            var entries = dto.Entries ?? [];
            if (entries.Count == 0)
                return BadRequest(ApiResponse<object>.Fail("No attendance entries were provided."));

            // Step 3: Check every status is Present/Absent/Excused (fix the letter casing).
            // Validate every status is one of the three allowed values (normalize casing).
            foreach (var e in entries)
            {
                var status = NormalizeStatus(e.Status);
                if (status is null)
                    return BadRequest(ApiResponse<object>.Fail(
                        $"Status must be one of: {string.Join(", ", AllowedStatuses)}."));
                e.Status = status; // store the canonical form
            }

            // Step 4: Make sure every student in the sheet really belongs to this class.
            // Validate every studentId actually belongs to this subject's class.
            var studentIds = entries.Select(e => e.StudentId).Distinct().ToList();
            var validStudentIds = await _context.Users
                .Where(u => u.Role == UserRole.Student
                            && u.ClassId == subject.ClassId
                            && studentIds.Contains(u.Id))
                .Select(u => u.Id)
                .ToListAsync();

            var invalid = studentIds.Except(validStudentIds).ToList();
            if (invalid.Count > 0)
                return BadRequest(ApiResponse<object>.Fail(
                    "One or more students do not belong to this subject's class."));

            // Existing rows for this subject + date, keyed by student, for the upsert.
            var existing = await _context.AttendanceRecords
                .Where(a => a.SubjectId == subjectId && a.Date == dto.Date)
                .ToListAsync();
            var byStudent = existing.ToDictionary(a => a.StudentId);

            var now = DateTime.UtcNow;
            var saved = 0;

            // Step 5: Save each row (update if it exists, otherwise add a new one).
            // Group all upserts into ONE atomic transaction.
            await using var transaction = await _context.Database.BeginTransactionAsync();

            foreach (var e in entries)
            {
                if (byStudent.TryGetValue(e.StudentId, out var record))
                {
                    // Update existing.
                    record.Status = e.Status;
                    record.Note = string.IsNullOrWhiteSpace(e.Note) ? null : e.Note.Trim();
                    record.RecordedByTeacherId = recorderId;
                    record.UpdatedAt = now;
                }
                else
                {
                    // Create new.
                    _context.AttendanceRecords.Add(new AttendanceRecord
                    {
                        SubjectId = subjectId,
                        StudentId = e.StudentId,
                        Date = dto.Date,
                        Status = e.Status,
                        Note = string.IsNullOrWhiteSpace(e.Note) ? null : e.Note.Trim(),
                        RecordedByTeacherId = recorderId,
                        CreatedAt = now,
                        UpdatedAt = now
                    });
                }
                saved++;
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return Ok(ApiResponse<object>.Ok(new { count = saved },
                $"Attendance saved for {saved} student{(saved == 1 ? "" : "s")}."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save attendance for subject {SubjectId}.", subjectId);
            return StatusCode(500, ApiResponse<object>.Fail("An error occurred while saving attendance."));
        }
    }

    /// <summary>
    /// GET /api/attendance/reports — admin-only flexible report. Optional filters:
    /// classId, subjectId, studentId, and a from/to date range. Newest first.
    /// </summary>
    // [Admin only] A flexible attendance report. All the filters below are optional, so the
    // admin can narrow the results by class, subject, student, and/or a date range.
    [HttpGet("reports")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Reports([FromQuery] AttendanceReportFilter filter)
    {
        try
        {
            var query = _context.AttendanceRecords.AsQueryable();

            if (filter.ClassId.HasValue) query = query.Where(a => a.Subject!.ClassId == filter.ClassId.Value);
            if (filter.SubjectId.HasValue) query = query.Where(a => a.SubjectId == filter.SubjectId.Value);
            if (filter.StudentId.HasValue) query = query.Where(a => a.StudentId == filter.StudentId.Value);
            if (filter.FromDate.HasValue) query = query.Where(a => a.Date >= filter.FromDate.Value);
            if (filter.ToDate.HasValue) query = query.Where(a => a.Date <= filter.ToDate.Value);

            // Project the raw name parts in SQL, then build the full name in memory
            // (NameFormatter can't be translated inside an IQueryable.Select).
            var rows = await query
                .OrderByDescending(a => a.Date)
                .ThenBy(a => a.Student!.FirstName)
                .Select(a => new
                {
                    a.Id,
                    a.SubjectId,
                    SubjectName = a.Subject!.Name,
                    ClassName = a.Subject.Class!.Name,
                    a.StudentId,
                    a.Student!.FirstName,
                    a.Student.MiddleName,
                    a.Student.LastName,
                    a.Date,
                    a.Status,
                    a.Note
                })
                .ToListAsync();

            var records = rows.Select(a => new AttendanceRecordDto
            {
                Id = a.Id,
                SubjectId = a.SubjectId,
                SubjectName = a.SubjectName,
                ClassName = a.ClassName,
                StudentId = a.StudentId,
                StudentName = NameFormatter.FullName(a.FirstName, a.MiddleName, a.LastName),
                Date = a.Date,
                Status = a.Status,
                Note = a.Note
            }).ToList();

            return Ok(ApiResponse<List<AttendanceRecordDto>>.Ok(records, "Attendance report retrieved successfully."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve attendance report.");
            return StatusCode(500, ApiResponse<List<AttendanceRecordDto>>.Fail("An error occurred while retrieving the report."));
        }
    }

    // ---- Helpers ----

    /// <summary>
    /// Enforce that the caller may touch this subject's attendance: an Admin always may;
    /// a Teacher only if TeacherSubjects assigns them to it. Returns null when allowed,
    /// otherwise the IActionResult to return (401/403).
    /// </summary>
    // Helper: the permission check. Admins always pass; a teacher passes only if they are
    // assigned to this subject. Returns null when allowed, or an error result (401/403) when not.
    private async Task<IActionResult?> AuthorizeSubjectAsync(int subjectId)
    {
        if (User.IsInRole(nameof(UserRole.Admin)))
            return null;

        if (!TryGetUserId(out var userId))
            return Unauthorized(ApiResponse<object>.Fail("Invalid or missing authentication token."));

        var assigned = await _context.TeacherSubjects
            .AnyAsync(ts => ts.TeacherId == userId && ts.SubjectId == subjectId);

        if (!assigned)
            return StatusCode(StatusCodes.Status403Forbidden,
                ApiResponse<object>.Fail("You are not assigned to this subject, so you can't manage its attendance."));

        return null;
    }

    /// <summary>
    /// Validate/normalize a status. Returns the canonical 'Present' / 'Absent' / 'Excused'
    /// (case-insensitive), or null when the value is none of them.
    /// </summary>
    // Helper: tidies a status into the exact word Present/Absent/Excused, ignoring casing.
    // Returns null if the value isn't one of those three.
    private static string? NormalizeStatus(string? status)
    {
        if (string.IsNullOrWhiteSpace(status)) return null;
        return AllowedStatuses.FirstOrDefault(s => s.Equals(status.Trim(), StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Pull the current user's id from the validated JWT. Inbound claims aren't remapped,
    /// so the id may arrive under NameIdentifier, "nameid", or "sub" — check all three
    /// (same approach as EventsController).
    /// </summary>
    // Helper: reads the logged-in user's id out of their sign-in token (the "JWT").
    // It checks a few claim names because the id can arrive under different labels.
    private bool TryGetUserId(out int userId)
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier)
                    ?? User.FindFirstValue(JwtRegisteredClaimNames.NameId)
                    ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);
        return int.TryParse(claim, out userId);
    }
}
