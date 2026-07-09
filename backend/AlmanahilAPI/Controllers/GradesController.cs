// ============================================
// GradesController — records and shows students' marks.
// Job: teachers enter marks for their subject (per assessment type like Quiz/Final);
// students and parents can view them; admins can run grade reports.
// It always trusts the logged-in user from their sign-in token, not the request body.
// Used by: the Vue grades pages (teacher enter-marks, student/parent grade views).
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
/// Grades (Module: Grades). Marks are recorded PER SUBJECT + assessment type for the
/// students of the subject's class. EF Core + LINQ only, every response uses the standard
/// ApiResponse envelope. Mirrors AttendanceController's conventions.
///
/// RBAC:
///  • Load / Save a subject's marks → the TEACHER assigned to that subject (via
///    TeacherSubjects) OR an Admin. A teacher not assigned → 403.
///  • Reports → Admin only.
///  • A student's own grades → Admin, the student themselves, or that student's parent.
/// The acting user is always taken from the validated JWT (never the request body).
/// </summary>
// This class manages students' marks (grades) for each subject and assessment type.
// [Authorize] means you must be signed in to reach ANY endpoint below.
[ApiController]
[Route("api/grades")]
[Authorize]
public class GradesController(AlmanahilDbContext context, ILogger<GradesController> logger) : ControllerBase
{
    private readonly AlmanahilDbContext _context = context;
    private readonly ILogger<GradesController> _logger = logger;

    /// <summary>The only AssessmentType values the UI offers.</summary>
    private static readonly string[] AllowedAssessments = ["Quiz", "Midterm", "Final", "Homework"];

    /// <summary>
    /// GET /api/grades/my-subjects — the subjects assigned to the logged-in teacher (from
    /// TeacherSubjects), for the subject picker. Admins have none → empty list.
    /// </summary>
    // [Any signed-in user] Gives the logged-in teacher the subjects they teach, for the
    // subject picker. (Admins/others get an empty list.)
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
    /// GET /api/grades/subject/{subjectId}?assessmentType=Quiz
    /// Returns the subject, its class's students, and each student's current mark+note for
    /// the assessment type (Mark null where not entered yet). Defaults to Quiz.
    /// </summary>
    // [Teacher assigned to this subject, or Admin] Builds the marks sheet: the class's
    // students plus each one's current mark/note for the chosen assessment type (blank if
    // not entered yet). Defaults to "Quiz".
    [HttpGet("subject/{subjectId:int}")]
    public async Task<IActionResult> GetSheet(int subjectId, [FromQuery] string? assessmentType)
    {
        try
        {
            // Default to Quiz when omitted; reject an explicitly-invalid value.
            string assessment;
            if (string.IsNullOrWhiteSpace(assessmentType))
            {
                assessment = "Quiz";
            }
            else
            {
                var norm = NormalizeAssessment(assessmentType);
                if (norm is null)
                    return BadRequest(ApiResponse<GradeSheetDto>.Fail(
                        $"Assessment type must be one of: {string.Join(", ", AllowedAssessments)}."));
                assessment = norm;
            }

            var subject = await _context.Subjects
                .Where(s => s.Id == subjectId)
                .Select(s => new { s.Id, s.Name, s.ClassId, ClassName = s.Class!.Name })
                .FirstOrDefaultAsync();

            if (subject is null)
                return NotFound(ApiResponse<GradeSheetDto>.Fail("Subject not found."));

            // RBAC: the assigned teacher or an admin.
            var access = await AuthorizeSubjectAsync(subjectId);
            if (access is not null) return access;

            // Pull the raw name parts, then build the full name in memory (NameFormatter
            // can't be translated inside an IQueryable.Select).
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

            var existing = await _context.GradeRecords
                .Where(g => g.SubjectId == subjectId && g.AssessmentType == assessment)
                .ToListAsync();
            var byStudent = existing.ToDictionary(g => g.StudentId);

            var sheet = new GradeSheetDto
            {
                SubjectId = subject.Id,
                SubjectName = subject.Name,
                ClassId = subject.ClassId,
                ClassName = subject.ClassName,
                AssessmentType = assessment,
                // If marks already exist for this assessment, reflect their MaxMark; else 100.
                MaxMark = existing.Count > 0 ? existing[0].MaxMark : 100m,
                Students = students.Select(s => new GradeSheetEntryDto
                {
                    StudentId = s.Id,
                    StudentName = s.Name,
                    Mark = byStudent.TryGetValue(s.Id, out var rec) ? rec.Mark : null,
                    Note = byStudent.TryGetValue(s.Id, out var rec2) ? rec2.Note : null
                }).ToList()
            };

            return Ok(ApiResponse<GradeSheetDto>.Ok(sheet, "Grade sheet retrieved successfully."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load grade sheet for subject {SubjectId}.", subjectId);
            return StatusCode(500, ApiResponse<GradeSheetDto>.Fail("An error occurred while loading grades."));
        }
    }

    /// <summary>
    /// PUT /api/grades/subject/{subjectId} — upsert each student's mark for the given
    /// subject + assessment type. Only the assigned teacher (or an admin) may save;
    /// RecordedByTeacherId comes from the JWT.
    /// </summary>
    // [Teacher assigned to this subject, or Admin] Saves the marks for one subject +
    // assessment type. For each student it updates the existing mark or adds a new one
    // ("upsert"). The whole save runs as one all-or-nothing transaction.
    [HttpPut("subject/{subjectId:int}")]
    public async Task<IActionResult> Save(int subjectId, [FromBody] SaveGradesDto dto)
    {
        try
        {
            // Step 1: Check the assessment type is valid (Quiz/Midterm/Final/Homework).
            var assessment = NormalizeAssessment(dto.AssessmentType);
            if (assessment is null)
                return BadRequest(ApiResponse<object>.Fail(
                    $"Assessment type must be one of: {string.Join(", ", AllowedAssessments)}."));

            // The highest possible mark; defaults to 100 if not given.
            var maxMark = dto.MaxMark <= 0 ? 100m : dto.MaxMark;

            // Step 2: Make sure the subject exists.
            var subject = await _context.Subjects
                .Where(s => s.Id == subjectId)
                .Select(s => new { s.Id, s.ClassId })
                .FirstOrDefaultAsync();

            if (subject is null)
                return NotFound(ApiResponse<object>.Fail("Subject not found."));

            // RBAC: the assigned teacher or an admin.
            var access = await AuthorizeSubjectAsync(subjectId);
            if (access is not null) return access;

            if (!TryGetUserId(out var recorderId))
                return Unauthorized(ApiResponse<object>.Fail("Invalid or missing authentication token."));

            var entries = dto.Entries ?? [];
            if (entries.Count == 0)
                return BadRequest(ApiResponse<object>.Fail("No grade entries were provided."));

            // Step 3: Check each mark is a number between 0 and the maximum.
            // Validate each mark is within 0..MaxMark.
            foreach (var e in entries)
            {
                if (e.Mark < 0 || e.Mark > maxMark)
                    return BadRequest(ApiResponse<object>.Fail(
                        $"Each mark must be a number between 0 and {maxMark:0.##}."));
            }

            // Step 4: Make sure every student in the sheet really belongs to this class.
            // Validate every studentId belongs to this subject's class.
            var studentIds = entries.Select(e => e.StudentId).Distinct().ToList();
            var validStudentIds = await _context.Users
                .Where(u => u.Role == UserRole.Student
                            && u.ClassId == subject.ClassId
                            && studentIds.Contains(u.Id))
                .Select(u => u.Id)
                .ToListAsync();

            if (studentIds.Except(validStudentIds).Any())
                return BadRequest(ApiResponse<object>.Fail(
                    "One or more students do not belong to this subject's class."));

            var existing = await _context.GradeRecords
                .Where(g => g.SubjectId == subjectId && g.AssessmentType == assessment)
                .ToListAsync();
            var byStudent = existing.ToDictionary(g => g.StudentId);

            var now = DateTime.UtcNow;
            var saved = 0;

            // Step 5: Save each mark (update if it exists, otherwise add a new one).
            // Group all upserts into ONE atomic transaction.
            await using var transaction = await _context.Database.BeginTransactionAsync();

            foreach (var e in entries)
            {
                if (byStudent.TryGetValue(e.StudentId, out var record))
                {
                    record.Mark = e.Mark;
                    record.MaxMark = maxMark;
                    record.Note = string.IsNullOrWhiteSpace(e.Note) ? null : e.Note.Trim();
                    record.RecordedByTeacherId = recorderId;
                    record.UpdatedAt = now;
                }
                else
                {
                    _context.GradeRecords.Add(new GradeRecord
                    {
                        SubjectId = subjectId,
                        StudentId = e.StudentId,
                        AssessmentType = assessment,
                        Mark = e.Mark,
                        MaxMark = maxMark,
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
                $"Grades saved for {saved} student{(saved == 1 ? "" : "s")}."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save grades for subject {SubjectId}.", subjectId);
            return StatusCode(500, ApiResponse<object>.Fail("An error occurred while saving grades."));
        }
    }

    /// <summary>
    /// GET /api/grades/reports — admin-only. Optional filters: classId, subjectId,
    /// studentId, assessmentType.
    /// </summary>
    // [Admin only] A flexible grade report. All the filters below are optional, so the admin
    // can narrow the results by class, subject, student, and/or assessment type.
    [HttpGet("reports")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Reports([FromQuery] GradesReportFilter filter)
    {
        try
        {
            var query = _context.GradeRecords.AsQueryable();

            if (filter.ClassId.HasValue) query = query.Where(g => g.Subject!.ClassId == filter.ClassId.Value);
            if (filter.SubjectId.HasValue) query = query.Where(g => g.SubjectId == filter.SubjectId.Value);
            if (filter.StudentId.HasValue) query = query.Where(g => g.StudentId == filter.StudentId.Value);

            if (!string.IsNullOrWhiteSpace(filter.AssessmentType))
            {
                var norm = NormalizeAssessment(filter.AssessmentType);
                if (norm is null)
                    return BadRequest(ApiResponse<List<GradeRecordDto>>.Fail(
                        $"Assessment type must be one of: {string.Join(", ", AllowedAssessments)}."));
                query = query.Where(g => g.AssessmentType == norm);
            }

            // Project the raw name parts in SQL, then build the full name in memory
            // (NameFormatter can't be translated inside an IQueryable.Select).
            var rows = await query
                .OrderBy(g => g.Subject!.Class!.Name)
                .ThenBy(g => g.Student!.FirstName)
                .Select(g => new
                {
                    g.Id,
                    g.SubjectId,
                    SubjectName = g.Subject!.Name,
                    ClassName = g.Subject.Class!.Name,
                    g.StudentId,
                    g.Student!.FirstName,
                    g.Student.MiddleName,
                    g.Student.LastName,
                    g.AssessmentType,
                    g.Mark,
                    g.MaxMark,
                    g.Note
                })
                .ToListAsync();

            var records = rows.Select(g => new GradeRecordDto
            {
                Id = g.Id,
                SubjectId = g.SubjectId,
                SubjectName = g.SubjectName,
                ClassName = g.ClassName,
                StudentId = g.StudentId,
                StudentName = NameFormatter.FullName(g.FirstName, g.MiddleName, g.LastName),
                AssessmentType = g.AssessmentType,
                Mark = g.Mark,
                MaxMark = g.MaxMark,
                Note = g.Note
            }).ToList();

            return Ok(ApiResponse<List<GradeRecordDto>>.Ok(records, "Grade report retrieved successfully."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve grade report.");
            return StatusCode(500, ApiResponse<List<GradeRecordDto>>.Fail("An error occurred while retrieving the report."));
        }
    }

    /// <summary>
    /// GET /api/grades/student/{studentId} — a student's grades across all subjects.
    /// RBAC: Admin, the student themselves, or that student's parent (via ParentStudents).
    /// </summary>
    // [Admin, the student themselves, or that student's parent] Shows one student's grades
    // across all subjects. A parent can only open their own child's grades.
    [HttpGet("student/{studentId:int}")]
    public async Task<IActionResult> ForStudent(int studentId)
    {
        try
        {
            // RBAC: admin bypasses; otherwise caller must be the student or their parent.
            if (!User.IsInRole(nameof(UserRole.Admin)))
            {
                if (!TryGetUserId(out var callerId))
                    return Unauthorized(ApiResponse<List<GradeRecordDto>>.Fail("Invalid or missing authentication token."));

                if (callerId != studentId)
                {
                    var isParent = await _context.ParentStudents
                        .AnyAsync(ps => ps.ParentId == callerId && ps.StudentId == studentId);
                    if (!isParent)
                        return StatusCode(StatusCodes.Status403Forbidden,
                            ApiResponse<List<GradeRecordDto>>.Fail("You are not allowed to view this student's grades."));
                }
            }

            // Project the raw name parts in SQL, then build the full name in memory
            // (NameFormatter can't be translated inside an IQueryable.Select).
            var rows = await _context.GradeRecords
                .Where(g => g.StudentId == studentId)
                .OrderBy(g => g.Subject!.Name).ThenBy(g => g.AssessmentType)
                .Select(g => new
                {
                    g.Id,
                    g.SubjectId,
                    SubjectName = g.Subject!.Name,
                    ClassName = g.Subject.Class!.Name,
                    g.StudentId,
                    g.Student!.FirstName,
                    g.Student.MiddleName,
                    g.Student.LastName,
                    g.AssessmentType,
                    g.Mark,
                    g.MaxMark,
                    g.Note
                })
                .ToListAsync();

            var records = rows.Select(g => new GradeRecordDto
            {
                Id = g.Id,
                SubjectId = g.SubjectId,
                SubjectName = g.SubjectName,
                ClassName = g.ClassName,
                StudentId = g.StudentId,
                StudentName = NameFormatter.FullName(g.FirstName, g.MiddleName, g.LastName),
                AssessmentType = g.AssessmentType,
                Mark = g.Mark,
                MaxMark = g.MaxMark,
                Note = g.Note
            }).ToList();

            return Ok(ApiResponse<List<GradeRecordDto>>.Ok(records, "Student grades retrieved successfully."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve grades for student {StudentId}.", studentId);
            return StatusCode(500, ApiResponse<List<GradeRecordDto>>.Fail("An error occurred while retrieving the grades."));
        }
    }

    // ---- Helpers ----

    /// <summary>
    /// Enforce that the caller may touch this subject's grades: an Admin always may; a
    /// Teacher only if TeacherSubjects assigns them to it. Returns null when allowed,
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
                ApiResponse<object>.Fail("You are not assigned to this subject, so you can't manage its grades."));

        return null;
    }

    /// <summary>
    /// Validate/normalize an assessment type. Returns the canonical value (case-insensitive),
    /// or null when it isn't one of the four allowed types.
    /// </summary>
    // Helper: tidies an assessment type into one of the four allowed words
    // (Quiz/Midterm/Final/Homework); returns null if it's none of them.
    private static string? NormalizeAssessment(string? type)
    {
        if (string.IsNullOrWhiteSpace(type)) return null;
        return AllowedAssessments.FirstOrDefault(a => a.Equals(type.Trim(), StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Pull the current user's id from the validated JWT (NameIdentifier / "nameid" / "sub").
    /// Same approach as AttendanceController / EventsController.
    /// </summary>
    // Helper: reads the logged-in user's id out of their sign-in token (checks a few labels).
    private bool TryGetUserId(out int userId)
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier)
                    ?? User.FindFirstValue(JwtRegisteredClaimNames.NameId)
                    ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);
        return int.TryParse(claim, out userId);
    }
}
