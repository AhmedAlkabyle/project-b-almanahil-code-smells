// ============================================
// SubjectsController — manages the subjects taught in each class.
// Job: create, view, edit and delete subjects (e.g. "Mathematics"). Each subject
// belongs to exactly one class.
// Used by: the Vue admin "Subjects" page. Admin-only.
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
/// Admin-only CRUD for subjects (Module 2). Mirrors ClassesController: whole
/// controller gated to "Admin", EF Core + LINQ only, every response in the standard
/// ApiResponse envelope. Each subject belongs to exactly one class.
/// </summary>
// This class manages subjects, each tied to one class. Admin-only.
[ApiController]
[Route("api/subjects")]
[Authorize(Roles = "Admin")]
public class SubjectsController(AlmanahilDbContext context, ILogger<SubjectsController> logger) : ControllerBase
{
    private readonly AlmanahilDbContext _context = context;
    private readonly ILogger<SubjectsController> _logger = logger;

    /// <summary>
    /// GET /api/subjects — list all subjects (with their ClassName).
    /// GET /api/subjects?classId={id} — optionally return only one class's subjects.
    /// </summary>
    // [Admin only] Lists all subjects (each with its class name). Add ?classId= to see just
    // one class's subjects.
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int? classId)
    {
        try
        {
            var query = _context.Subjects.AsQueryable();

            // Optional filter: only the given class's subjects.
            if (classId.HasValue)
                query = query.Where(s => s.ClassId == classId.Value);

            var subjects = await query
                .OrderBy(s => s.Class!.Name)
                .ThenBy(s => s.Name)
                .Select(s => new SubjectResponseDto
                {
                    Id = s.Id,
                    Name = s.Name,
                    ClassId = s.ClassId,
                    // ClassName resolved via the Class navigation (LINQ join) — no raw SQL.
                    ClassName = s.Class!.Name,
                    CreatedAt = s.CreatedAt
                })
                .ToListAsync();

            return Ok(ApiResponse<List<SubjectResponseDto>>.Ok(subjects, "Subjects retrieved successfully."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve subjects.");
            return StatusCode(500,
                ApiResponse<List<SubjectResponseDto>>.Fail("An error occurred while retrieving subjects."));
        }
    }

    /// <summary>GET /api/subjects/{id} — one subject, or a friendly not-found.</summary>
    // [Admin only] Gets one subject by its id (or a friendly "not found").
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        try
        {
            var subject = await _context.Subjects
                .Where(s => s.Id == id)
                .Select(s => new SubjectResponseDto
                {
                    Id = s.Id,
                    Name = s.Name,
                    ClassId = s.ClassId,
                    ClassName = s.Class!.Name,
                    CreatedAt = s.CreatedAt
                })
                .FirstOrDefaultAsync();

            if (subject is null)
                return NotFound(ApiResponse<SubjectResponseDto>.Fail("Subject not found."));

            return Ok(ApiResponse<SubjectResponseDto>.Ok(subject, "Subject retrieved successfully."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve subject {SubjectId}.", id);
            return StatusCode(500,
                ApiResponse<SubjectResponseDto>.Fail("An error occurred while retrieving the subject."));
        }
    }

    /// <summary>POST /api/subjects — create a subject; the ClassId must exist.</summary>
    // [Admin only] Creates a subject. The chosen class must exist, and the class can't
    // already have another subject with the same name.
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateSubjectDto dto)
    {
        try
        {
            // The chosen class must exist (friendly error instead of an FK exception).
            var classExists = await _context.Classes.AnyAsync(c => c.Id == dto.ClassId);
            if (!classExists)
                return BadRequest(ApiResponse<SubjectResponseDto>.Fail("Selected class not found."));

            var name = dto.Name.Trim();

            // Friendly guard: no two subjects with the same name in the same class.
            var duplicate = await _context.Subjects
                .AnyAsync(s => s.ClassId == dto.ClassId && s.Name.ToLower() == name.ToLower());
            if (duplicate)
                return BadRequest(ApiResponse<SubjectResponseDto>.Fail("This class already has a subject with that name."));

            var entity = new Subject
            {
                Name = name,
                ClassId = dto.ClassId,
                CreatedAt = DateTime.UtcNow
            };

            _context.Subjects.Add(entity);
            await _context.SaveChangesAsync();

            var result = new SubjectResponseDto
            {
                Id = entity.Id,
                Name = entity.Name,
                ClassId = entity.ClassId,
                ClassName = await ClassNameAsync(entity.ClassId),
                CreatedAt = entity.CreatedAt
            };

            return CreatedAtAction(nameof(GetById), new { id = entity.Id },
                ApiResponse<SubjectResponseDto>.Ok(result, "Subject created successfully."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create subject.");
            return StatusCode(500,
                ApiResponse<SubjectResponseDto>.Fail("An error occurred while creating the subject."));
        }
    }

    /// <summary>PUT /api/subjects/{id} — update a subject; the ClassId must exist.</summary>
    // [Admin only] Edits a subject (its name and/or class). Same checks as create.
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateSubjectDto dto)
    {
        try
        {
            var entity = await _context.Subjects.FindAsync(id);
            if (entity is null)
                return NotFound(ApiResponse<SubjectResponseDto>.Fail("Subject not found."));

            var classExists = await _context.Classes.AnyAsync(c => c.Id == dto.ClassId);
            if (!classExists)
                return BadRequest(ApiResponse<SubjectResponseDto>.Fail("Selected class not found."));

            var name = dto.Name.Trim();

            var duplicate = await _context.Subjects
                .AnyAsync(s => s.Id != id && s.ClassId == dto.ClassId && s.Name.ToLower() == name.ToLower());
            if (duplicate)
                return BadRequest(ApiResponse<SubjectResponseDto>.Fail("This class already has a subject with that name."));

            entity.Name = name;
            entity.ClassId = dto.ClassId;
            await _context.SaveChangesAsync();

            var result = new SubjectResponseDto
            {
                Id = entity.Id,
                Name = entity.Name,
                ClassId = entity.ClassId,
                ClassName = await ClassNameAsync(entity.ClassId),
                CreatedAt = entity.CreatedAt
            };

            return Ok(ApiResponse<SubjectResponseDto>.Ok(result, "Subject updated successfully."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update subject {SubjectId}.", id);
            return StatusCode(500,
                ApiResponse<SubjectResponseDto>.Fail("An error occurred while updating the subject."));
        }
    }

    /// <summary>
    /// DELETE /api/subjects/{id} — remove a subject. Per the Module 2 migration this
    /// cascade-deletes its TeacherSubjects (teacher assignments for this subject).
    /// </summary>
    // [Admin only] Deletes a subject. Any teacher assignments for it are removed automatically.
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            var entity = await _context.Subjects.FindAsync(id);
            if (entity is null)
                return NotFound(ApiResponse<object>.Fail("Subject not found."));

            _context.Subjects.Remove(entity);
            await _context.SaveChangesAsync();

            return Ok(ApiResponse<object>.Ok(new { id }, "Subject deleted successfully."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete subject {SubjectId}.", id);
            return StatusCode(500,
                ApiResponse<object>.Fail("An error occurred while deleting the subject."));
        }
    }

    // Helper: looks up a class's name from its id (returns "" if the class is missing).
    /// <summary>Small helper: resolve a class name by id via LINQ (empty if missing).</summary>
    private async Task<string> ClassNameAsync(int classId) =>
        await _context.Classes
            .Where(c => c.Id == classId)
            .Select(c => c.Name)
            .FirstOrDefaultAsync() ?? string.Empty;
}
