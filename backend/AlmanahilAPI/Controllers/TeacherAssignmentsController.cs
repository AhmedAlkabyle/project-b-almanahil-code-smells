// ============================================
// TeacherAssignmentsController — decides which teacher teaches which subject.
// Job: lets an admin view, create and delete "teacher teaches this subject" links.
// These links are what let a teacher take attendance, enter grades, and add materials.
// Used by: the Vue admin "Teacher Assignments" page. Admin-only.
// ============================================
using AlmanahilAPI.Data;
using AlmanahilAPI.DTOs;
using AlmanahilAPI.Helpers;
using AlmanahilAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AlmanahilAPI.Controllers;

/// <summary>
/// Admin-only management of teacher↔subject assignments (Module 2). Mirrors
/// ClassesController: the whole controller is gated to "Admin", EF Core + LINQ only,
/// and every response uses the standard ApiResponse envelope. Backed by the
/// TeacherSubject join table, where each (TeacherId, SubjectId) pair is unique.
/// </summary>
// This class manages teacher-to-subject assignments. Admin-only.
[ApiController]
[Route("api/assignments")]
[Authorize(Roles = "Admin")]
public class TeacherAssignmentsController(AlmanahilDbContext context, ILogger<TeacherAssignmentsController> logger) : ControllerBase
{
    private readonly AlmanahilDbContext _context = context;
    private readonly ILogger<TeacherAssignmentsController> _logger = logger;

    /// <summary>GET /api/assignments — every assignment with resolved teacher / subject / class names.</summary>
    // [Admin only] Lists every teacher-subject assignment (with teacher, subject and class names).
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        try
        {
            // Project the raw teacher-name parts in SQL, then build the full name in memory
            // (NameFormatter can't be translated inside an IQueryable.Select).
            var rows = await _context.TeacherSubjects
                .OrderBy(ts => ts.Subject!.Class!.Name)
                .ThenBy(ts => ts.Subject!.Name)
                .Select(ts => new
                {
                    ts.Id,
                    ts.TeacherId,
                    ts.Teacher!.FirstName,
                    ts.Teacher.MiddleName,
                    ts.Teacher.LastName,
                    ts.SubjectId,
                    SubjectName = ts.Subject!.Name,
                    ClassName = ts.Subject.Class!.Name,
                    ts.CreatedAt
                })
                .ToListAsync();

            var assignments = rows.Select(ts => new AssignmentResponseDto
            {
                Id = ts.Id,
                TeacherId = ts.TeacherId,
                TeacherName = NameFormatter.FullName(ts.FirstName, ts.MiddleName, ts.LastName),
                SubjectId = ts.SubjectId,
                SubjectName = ts.SubjectName,
                ClassName = ts.ClassName,
                CreatedAt = ts.CreatedAt
            }).ToList();

            return Ok(ApiResponse<List<AssignmentResponseDto>>.Ok(assignments, "Assignments retrieved successfully."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve assignments.");
            return StatusCode(500,
                ApiResponse<List<AssignmentResponseDto>>.Fail("An error occurred while retrieving assignments."));
        }
    }

    /// <summary>
    /// POST /api/assignments — assign a teacher to a subject. Validates that the teacher
    /// exists and really is a Teacher, the subject exists, and the pair isn't already assigned.
    /// </summary>
    // [Admin only] Assigns a teacher to a subject. Checks the teacher really is a Teacher,
    // the subject exists, and that this exact pair isn't already assigned.
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateAssignmentDto dto)
    {
        try
        {
            // The teacher must exist AND carry the Teacher role.
            var isTeacher = await _context.Users
                .AnyAsync(u => u.Id == dto.TeacherId && u.Role == UserRole.Teacher);
            if (!isTeacher)
                return BadRequest(ApiResponse<AssignmentResponseDto>.Fail("Teacher not found."));

            // The subject must exist.
            var subjectExists = await _context.Subjects.AnyAsync(s => s.Id == dto.SubjectId);
            if (!subjectExists)
                return BadRequest(ApiResponse<AssignmentResponseDto>.Fail("Subject not found."));

            // The (teacher, subject) pair must be unique (also enforced by a DB unique index).
            var duplicate = await _context.TeacherSubjects
                .AnyAsync(ts => ts.TeacherId == dto.TeacherId && ts.SubjectId == dto.SubjectId);
            if (duplicate)
                return BadRequest(ApiResponse<AssignmentResponseDto>.Fail("This teacher is already assigned to this subject."));

            var entity = new TeacherSubject
            {
                TeacherId = dto.TeacherId,
                SubjectId = dto.SubjectId,
                CreatedAt = DateTime.UtcNow
            };

            _context.TeacherSubjects.Add(entity);
            await _context.SaveChangesAsync();

            // Re-read with resolved names for a complete response body. Project the raw name
            // parts in SQL, then build the full name in memory (NameFormatter can't be
            // translated inside an IQueryable.Select).
            var row = await _context.TeacherSubjects
                .Where(ts => ts.Id == entity.Id)
                .Select(ts => new
                {
                    ts.Id,
                    ts.TeacherId,
                    ts.Teacher!.FirstName,
                    ts.Teacher.MiddleName,
                    ts.Teacher.LastName,
                    ts.SubjectId,
                    SubjectName = ts.Subject!.Name,
                    ClassName = ts.Subject.Class!.Name,
                    ts.CreatedAt
                })
                .FirstAsync();

            var result = new AssignmentResponseDto
            {
                Id = row.Id,
                TeacherId = row.TeacherId,
                TeacherName = NameFormatter.FullName(row.FirstName, row.MiddleName, row.LastName),
                SubjectId = row.SubjectId,
                SubjectName = row.SubjectName,
                ClassName = row.ClassName,
                CreatedAt = row.CreatedAt
            };

            return Ok(ApiResponse<AssignmentResponseDto>.Ok(result, "Teacher assigned to subject successfully."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create assignment.");
            return StatusCode(500,
                ApiResponse<AssignmentResponseDto>.Fail("An error occurred while creating the assignment."));
        }
    }

    // [Admin only] Removes one teacher-subject assignment by its id.
    /// <summary>DELETE /api/assignments/{id} — remove a teacher-subject assignment.</summary>
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            var entity = await _context.TeacherSubjects.FindAsync(id);
            if (entity is null)
                return NotFound(ApiResponse<object>.Fail("Assignment not found."));

            _context.TeacherSubjects.Remove(entity);
            await _context.SaveChangesAsync();

            return Ok(ApiResponse<object>.Ok(new { id }, "Assignment removed successfully."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete assignment {AssignmentId}.", id);
            return StatusCode(500,
                ApiResponse<object>.Fail("An error occurred while removing the assignment."));
        }
    }
}
