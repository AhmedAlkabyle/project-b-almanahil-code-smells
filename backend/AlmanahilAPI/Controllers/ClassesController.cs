// ============================================
// ClassesController — manages the school's classes (like "1/A").
// Job: create, view, edit and delete classes, and enroll/remove students in a class.
// A class is built from academic year + level + year (grade) + section.
// Used by: the Vue admin "Classes" and "Assign Students" pages.
// Note: the whole controller is Admin-only.
// ============================================
using System.Text.RegularExpressions;
using AlmanahilAPI.Data;
using AlmanahilAPI.DTOs;
using AlmanahilAPI.Helpers;
using AlmanahilAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AlmanahilAPI.Controllers;

/// <summary>
/// Admin-only CRUD for classes (Module 2). The whole controller is gated to the
/// "Admin" role, so a missing/invalid token → 401 and a non-admin token → 403.
/// EF Core + LINQ only; every response uses the standard ApiResponse envelope.
///
/// A class is described by AcademicYear + Level + Grade + Section. The short Name
/// (e.g. "1/A") is auto-composed from Grade + Section; DisplayName is a nicer label
/// (e.g. "1/A — الأول إعدادي 2025/2026").
/// </summary>
// This class manages classes and who is enrolled in them.
// [Authorize(Roles = "Admin")] means EVERY endpoint below is Admin-only.
[ApiController]
[Route("api/classes")]
[Authorize(Roles = "Admin")]
public class ClassesController(AlmanahilDbContext context, ILogger<ClassesController> logger) : ControllerBase
{
    private readonly AlmanahilDbContext _context = context;
    private readonly ILogger<ClassesController> _logger = logger;

    // Allowed values for the structured fields (validated server-side; the frontend mirrors these).
    private static readonly string[] AllowedLevels = ["Secondary", "HighSchool"];
    private static readonly string[] AllowedSections = ["A", "B", "C", "D"];
    private static readonly string[] ArabicOrdinals = ["الأول", "الثاني", "الثالث"]; // grade 1..3
    private static readonly Regex AcademicYearPattern = new(@"^\d{4}/\d{4}$", RegexOptions.Compiled);

    /// <summary>GET /api/classes — list every class with its student count.</summary>
    // [Admin only] Lists every class, each with a count of how many students are in it.
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        try
        {
            var classes = await _context.Classes
                .OrderBy(c => c.AcademicYear)
                .ThenBy(c => c.Level)
                .ThenBy(c => c.Grade)
                .ThenBy(c => c.Section)
                .ThenBy(c => c.Name)
                .Select(c => new ClassResponseDto
                {
                    Id = c.Id,
                    Name = c.Name,
                    AcademicYear = c.AcademicYear,
                    Level = c.Level,
                    Grade = c.Grade,
                    Section = c.Section,
                    Description = c.Description,
                    // StudentsCount is computed in-query (correlated subquery) — no raw SQL.
                    StudentsCount = c.Students.Count(u => u.Role == UserRole.Student),
                    CreatedAt = c.CreatedAt
                })
                .ToListAsync();

            // DisplayName mixes conditional + Arabic text → compose it in memory.
            foreach (var c in classes) c.DisplayName = BuildDisplayName(c);

            return Ok(ApiResponse<List<ClassResponseDto>>.Ok(classes, "Classes retrieved successfully."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve classes.");
            return StatusCode(500,
                ApiResponse<List<ClassResponseDto>>.Fail("An error occurred while retrieving classes."));
        }
    }

    /// <summary>GET /api/classes/{id} — one class, or a friendly not-found.</summary>
    // [Admin only] Gets the details of one class by its id (or a friendly "not found").
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        try
        {
            var cls = await _context.Classes
                .Where(c => c.Id == id)
                .Select(c => new ClassResponseDto
                {
                    Id = c.Id,
                    Name = c.Name,
                    AcademicYear = c.AcademicYear,
                    Level = c.Level,
                    Grade = c.Grade,
                    Section = c.Section,
                    Description = c.Description,
                    StudentsCount = c.Students.Count(u => u.Role == UserRole.Student),
                    CreatedAt = c.CreatedAt
                })
                .FirstOrDefaultAsync();

            if (cls is null)
                return NotFound(ApiResponse<ClassResponseDto>.Fail("Class not found."));

            cls.DisplayName = BuildDisplayName(cls);

            return Ok(ApiResponse<ClassResponseDto>.Ok(cls, "Class retrieved successfully."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve class {ClassId}.", id);
            return StatusCode(500,
                ApiResponse<ClassResponseDto>.Fail("An error occurred while retrieving the class."));
        }
    }

    /// <summary>POST /api/classes — create a class from CreateClassDto.</summary>
    // [Admin only] Creates a new class. Checks the details are valid and that the same
    // year/level/grade/section combination doesn't already exist.
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateClassDto dto)
    {
        try
        {
            var (parts, error) = NormalizeClassParts(dto.AcademicYear, dto.Level, dto.Grade, dto.Section);
            if (error is not null)
                return BadRequest(ApiResponse<ClassResponseDto>.Fail(error));

            var (academicYear, level, grade, section) = parts!;

            // Uniqueness is on the whole combination (so "1/A" can exist in a different
            // level or academic year without clashing).
            var exists = await _context.Classes.AnyAsync(c =>
                c.AcademicYear == academicYear && c.Level == level && c.Grade == grade && c.Section == section);
            if (exists)
                return BadRequest(ApiResponse<ClassResponseDto>.Fail(
                    "A class with this academic year, level, grade and section already exists."));

            var entity = new Class
            {
                Name = $"{grade}/{section}", // e.g. "1/A"
                AcademicYear = academicYear,
                Level = level,
                Grade = grade,
                Section = section,
                Description = Clean(dto.Description),
                CreatedAt = DateTime.UtcNow
            };

            _context.Classes.Add(entity);
            await _context.SaveChangesAsync();

            var result = MapToDto(entity, 0); // brand-new class has no students yet

            // 201 Created + Location header pointing at GET /api/classes/{id}.
            return CreatedAtAction(nameof(GetById), new { id = entity.Id },
                ApiResponse<ClassResponseDto>.Ok(result, "Class created successfully."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create class.");
            return StatusCode(500,
                ApiResponse<ClassResponseDto>.Fail("An error occurred while creating the class."));
        }
    }

    /// <summary>PUT /api/classes/{id} — update a class from UpdateClassDto.</summary>
    // [Admin only] Edits an existing class, making sure another class isn't already using
    // the same year/level/grade/section combination.
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateClassDto dto)
    {
        try
        {
            var entity = await _context.Classes.FindAsync(id);
            if (entity is null)
                return NotFound(ApiResponse<ClassResponseDto>.Fail("Class not found."));

            var (parts, error) = NormalizeClassParts(dto.AcademicYear, dto.Level, dto.Grade, dto.Section);
            if (error is not null)
                return BadRequest(ApiResponse<ClassResponseDto>.Fail(error));

            var (academicYear, level, grade, section) = parts!;

            // Another class must not already use this same combination.
            var clash = await _context.Classes.AnyAsync(c => c.Id != id &&
                c.AcademicYear == academicYear && c.Level == level && c.Grade == grade && c.Section == section);
            if (clash)
                return BadRequest(ApiResponse<ClassResponseDto>.Fail(
                    "A class with this academic year, level, grade and section already exists."));

            entity.Name = $"{grade}/{section}";
            entity.AcademicYear = academicYear;
            entity.Level = level;
            entity.Grade = grade;
            entity.Section = section;
            entity.Description = Clean(dto.Description);

            await _context.SaveChangesAsync();

            var studentsCount = await _context.Users
                .CountAsync(u => u.ClassId == id && u.Role == UserRole.Student);

            var result = MapToDto(entity, studentsCount);

            return Ok(ApiResponse<ClassResponseDto>.Ok(result, "Class updated successfully."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update class {ClassId}.", id);
            return StatusCode(500,
                ApiResponse<ClassResponseDto>.Fail("An error occurred while updating the class."));
        }
    }

    /// <summary>
    /// DELETE /api/classes/{id} — remove a class. Per the Module 2 migration this
    /// SetNulls its students' ClassId (students are kept) and cascade-deletes its
    /// subjects (and their teacher assignments).
    /// </summary>
    // [Admin only] Deletes a class. Its students are kept but lose their class link, and the
    // class's subjects (and their teacher assignments) are removed automatically.
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            var entity = await _context.Classes.FindAsync(id);
            if (entity is null)
                return NotFound(ApiResponse<object>.Fail("Class not found."));

            _context.Classes.Remove(entity);
            await _context.SaveChangesAsync();

            return Ok(ApiResponse<object>.Ok(new { id }, "Class deleted successfully."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete class {ClassId}.", id);
            return StatusCode(500,
                ApiResponse<object>.Fail("An error occurred while deleting the class."));
        }
    }

    /// <summary>
    /// POST /api/classes/{classId}/students — enroll one or more students into a class.
    /// The body is { studentIds: [ ... ] }. Only ids that belong to real Student accounts
    /// are moved (anything else is silently skipped); each matched student's ClassId is set
    /// to this class. EF Core + LINQ only, no raw SQL.
    /// </summary>
    // [Admin only] Enrolls one or more students into a class. Any id that isn't a real
    // student is quietly skipped; matched students get moved into this class.
    [HttpPost("{classId:int}/students")]
    public async Task<IActionResult> EnrollStudents(int classId, [FromBody] EnrollStudentsDto dto)
    {
        try
        {
            // The class must exist before we can move anyone into it.
            var classExists = await _context.Classes.AnyAsync(c => c.Id == classId);
            if (!classExists)
                return NotFound(ApiResponse<object>.Fail("Class not found."));

            // De-duplicate the incoming ids, then load only those that are real Students.
            var ids = dto.StudentIds.Distinct().ToList();
            if (ids.Count == 0)
                return BadRequest(ApiResponse<object>.Fail("Please select at least one student."));

            var students = await _context.Users
                .Where(u => ids.Contains(u.Id) && u.Role == UserRole.Student)
                .ToListAsync();

            if (students.Count == 0)
                return BadRequest(ApiResponse<object>.Fail("None of the selected users are students."));

            foreach (var student in students)
                student.ClassId = classId;

            await _context.SaveChangesAsync();

            var count = students.Count;
            var message = count == 1 ? "1 student enrolled." : $"{count} students enrolled.";

            return Ok(ApiResponse<object>.Ok(new { enrolled = count, classId }, message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to enroll students into class {ClassId}.", classId);
            return StatusCode(500,
                ApiResponse<object>.Fail("An error occurred while enrolling students."));
        }
    }

    /// <summary>
    /// GET /api/classes/{classId}/students — the students currently enrolled in a class
    /// (ClassId == classId, Role == Student), as { id, fullName, email }. EF Core + LINQ only.
    /// </summary>
    // [Admin only] Lists the students currently enrolled in one class (name + email).
    [HttpGet("{classId:int}/students")]
    public async Task<IActionResult> GetClassStudents(int classId)
    {
        try
        {
            var classExists = await _context.Classes.AnyAsync(c => c.Id == classId);
            if (!classExists)
                return NotFound(ApiResponse<List<ClassStudentDto>>.Fail("Class not found."));

            // Pull the raw name parts, then compose FullName in memory (First (+ Middle) + Last)
            // so the null-handling doesn't need to be translated to SQL.
            var rows = await _context.Users
                .Where(u => u.ClassId == classId && u.Role == UserRole.Student)
                .OrderBy(u => u.FirstName)
                .ThenBy(u => u.LastName)
                .Select(u => new { u.Id, u.FirstName, u.MiddleName, u.LastName, u.Email })
                .ToListAsync();

            var students = rows.Select(u => new ClassStudentDto
            {
                Id = u.Id,
                FullName = NameFormatter.FullName(u.FirstName, u.MiddleName, u.LastName),
                Email = u.Email
            }).ToList();

            return Ok(ApiResponse<List<ClassStudentDto>>.Ok(students, "Students retrieved successfully."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve students for class {ClassId}.", classId);
            return StatusCode(500,
                ApiResponse<List<ClassStudentDto>>.Fail("An error occurred while retrieving the class students."));
        }
    }

    /// <summary>
    /// DELETE /api/classes/{classId}/students/{studentId} — unenroll a student from a class by
    /// setting their ClassId back to null. The student account itself is NOT deleted. Validates
    /// that the student exists and is actually in this class.
    /// </summary>
    // [Admin only] Removes a student from a class (just clears their class link — the
    // student account itself is kept). Checks the student is really in this class first.
    [HttpDelete("{classId:int}/students/{studentId:int}")]
    public async Task<IActionResult> RemoveStudentFromClass(int classId, int studentId)
    {
        try
        {
            var student = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == studentId && u.Role == UserRole.Student);
            if (student is null)
                return NotFound(ApiResponse<object>.Fail("Student not found."));

            if (student.ClassId != classId)
                return BadRequest(ApiResponse<object>.Fail("This student is not enrolled in this class."));

            // Unenroll only — keep the student account, just clear the class link.
            student.ClassId = null;
            await _context.SaveChangesAsync();

            return Ok(ApiResponse<object>.Ok(new { studentId, classId }, "Student removed from class."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to remove student {StudentId} from class {ClassId}.", studentId, classId);
            return StatusCode(500,
                ApiResponse<object>.Fail("An error occurred while removing the student from the class."));
        }
    }

    // ================= Helpers =================

    /// <summary>
    /// Validates + normalizes the structured class fields. Returns (Parts, null) with the
    /// cleaned values bundled into a <see cref="ClassParts"/> record when valid, or
    /// (null, Error) with a friendly message (for ApiResponse.Fail) on any problem.
    /// </summary>
    // Helper: checks and tidies the class fields (year, level, grade, section). Returns a
    // friendly error message if something is wrong, or null when everything is valid.
    private static (ClassParts? Parts, string? Error) NormalizeClassParts(
        string? academicYearRaw, string? levelRaw, int? gradeRaw, string? sectionRaw)
    {
        var academicYear = academicYearRaw?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(academicYear))
            return (null, "Academic year is required.");
        if (!AcademicYearPattern.IsMatch(academicYear))
            return (null, "Academic year must look like 2025/2026.");

        var canonicalLevel = AllowedLevels
            .FirstOrDefault(l => l.Equals(levelRaw?.Trim(), StringComparison.OrdinalIgnoreCase));
        if (canonicalLevel is null)
            return (null, "Please choose a valid level (Secondary or High School).");

        if (gradeRaw is null or < 1 or > 3)
            return (null, "Year must be 1, 2, or 3.");
        var grade = gradeRaw.Value;

        var sec = sectionRaw?.Trim().ToUpperInvariant() ?? string.Empty;
        if (!AllowedSections.Contains(sec))
            return (null, "Section must be A, B, C, or D.");

        return (new ClassParts(academicYear, canonicalLevel, grade, sec), null); // valid
    }

    /// <summary>Maps a saved Class entity + its student count to the response DTO (with DisplayName).</summary>
    // Helper: copies a saved class (plus its student count) into the shape sent back to the website.
    private static ClassResponseDto MapToDto(Class c, int studentsCount)
    {
        var dto = new ClassResponseDto
        {
            Id = c.Id,
            Name = c.Name,
            AcademicYear = c.AcademicYear,
            Level = c.Level,
            Grade = c.Grade,
            Section = c.Section,
            Description = c.Description,
            StudentsCount = studentsCount,
            CreatedAt = c.CreatedAt
        };
        dto.DisplayName = BuildDisplayName(dto);
        return dto;
    }

    /// <summary>
    /// Builds a human-readable label, e.g. "1/A — الأول إعدادي 2025/2026". Legacy rows
    /// (created before this upgrade) may lack the structured parts — they fall back to Name.
    /// </summary>
    // Helper: builds the nice label shown to people, e.g. "1/A — الأول إعدادي 2025/2026".
    // Older classes that lack the newer fields just show their short name.
    private static string BuildDisplayName(ClassResponseDto c)
    {
        if (string.IsNullOrWhiteSpace(c.Level) || c.Grade is null || string.IsNullOrWhiteSpace(c.Section))
            return c.Name;

        var label = ArabicGradeLevel(c.Level, c.Grade.Value);
        var year = string.IsNullOrWhiteSpace(c.AcademicYear) ? string.Empty : $" {c.AcademicYear}";
        return $"{c.Name} — {label}{year}";
    }

    /// <summary>Maps (level, grade) to its Arabic label, e.g. ("Secondary", 1) → "الأول إعدادي".</summary>
    // Helper: turns a level + grade into its Arabic wording, e.g. ("Secondary", 1) → "الأول إعدادي".
    private static string ArabicGradeLevel(string level, int grade)
    {
        var ordinal = grade is >= 1 and <= 3 ? ArabicOrdinals[grade - 1] : grade.ToString();
        var levelAr = level.Equals("HighSchool", StringComparison.OrdinalIgnoreCase) ? "ثانوي" : "إعدادي";
        return $"{ordinal} {levelAr}";
    }

    // Helper: trims spaces; if nothing is left, stores nothing (null) instead of "".
    /// <summary>Trim to null: empty/whitespace becomes null, otherwise trimmed.</summary>
    private static string? Clean(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
