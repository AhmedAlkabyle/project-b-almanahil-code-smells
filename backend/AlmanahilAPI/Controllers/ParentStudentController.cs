// ============================================
// ParentStudentController — connects parents to their children.
// Job: lets an admin view, create and delete the links that say "this parent belongs to
// this student". These links are what let a parent see their child's data.
// Used by: the Vue admin "Parent Links" page. Admin-only.
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
/// Admin-only management of parent↔student links (Module 2). Mirrors ClassesController:
/// the whole controller is gated to "Admin", EF Core + LINQ only, and every response uses
/// the standard ApiResponse envelope. Backed by the ParentStudent join table, where each
/// (ParentId, StudentId) pair is unique.
/// </summary>
// This class manages parent-to-student links. Admin-only.
[ApiController]
[Route("api/parent-links")]
[Authorize(Roles = "Admin")]
public class ParentStudentController(AlmanahilDbContext context, ILogger<ParentStudentController> logger) : ControllerBase
{
    private readonly AlmanahilDbContext _context = context;
    private readonly ILogger<ParentStudentController> _logger = logger;

    /// <summary>
    /// GET /api/parent-links — every parent↔student link with resolved names.
    /// GET /api/parent-links?parentId={id} — optionally only a single parent's linked students.
    /// </summary>
    // [Admin only] Lists every parent-student link (with names). Add ?parentId= to see just
    // one parent's linked students.
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int? parentId)
    {
        try
        {
            var query = _context.ParentStudents.AsQueryable();

            // Optional filter: only the given parent's linked students.
            if (parentId.HasValue)
                query = query.Where(ps => ps.ParentId == parentId.Value);

            // Project the raw name parts in SQL, then build the full names in memory
            // (NameFormatter can't be translated inside an IQueryable.Select).
            var rows = await query
                .OrderBy(ps => ps.Student!.FirstName)
                .ThenBy(ps => ps.Student!.LastName)
                .Select(ps => new
                {
                    ps.Id,
                    ps.ParentId,
                    ParentFirstName = ps.Parent!.FirstName,
                    ParentMiddleName = ps.Parent.MiddleName,
                    ParentLastName = ps.Parent.LastName,
                    ps.StudentId,
                    StudentFirstName = ps.Student!.FirstName,
                    StudentMiddleName = ps.Student.MiddleName,
                    StudentLastName = ps.Student.LastName,
                    ps.CreatedAt
                })
                .ToListAsync();

            var links = rows.Select(ps => new ParentLinkResponseDto
            {
                Id = ps.Id,
                ParentId = ps.ParentId,
                ParentName = NameFormatter.FullName(ps.ParentFirstName, ps.ParentMiddleName, ps.ParentLastName),
                StudentId = ps.StudentId,
                StudentName = NameFormatter.FullName(ps.StudentFirstName, ps.StudentMiddleName, ps.StudentLastName),
                CreatedAt = ps.CreatedAt
            }).ToList();

            return Ok(ApiResponse<List<ParentLinkResponseDto>>.Ok(links, "Parent links retrieved successfully."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve parent links.");
            return StatusCode(500,
                ApiResponse<List<ParentLinkResponseDto>>.Fail("An error occurred while retrieving parent links."));
        }
    }

    /// <summary>
    /// POST /api/parent-links — link a parent to a student. Validates that the parent
    /// really is a Parent, the student really is a Student, and the pair isn't already linked.
    /// </summary>
    // [Admin only] Links a parent to a student. Checks the parent really is a Parent, the
    // student really is a Student, and that this exact pair isn't already linked.
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateParentLinkDto dto)
    {
        try
        {
            // The parent must exist AND carry the Parent role.
            var isParent = await _context.Users
                .AnyAsync(u => u.Id == dto.ParentId && u.Role == UserRole.Parent);
            if (!isParent)
                return BadRequest(ApiResponse<ParentLinkResponseDto>.Fail("Parent not found."));

            // The student must exist AND carry the Student role.
            var isStudent = await _context.Users
                .AnyAsync(u => u.Id == dto.StudentId && u.Role == UserRole.Student);
            if (!isStudent)
                return BadRequest(ApiResponse<ParentLinkResponseDto>.Fail("Student not found."));

            // The (parent, student) pair must be unique (also enforced by a DB unique index).
            var duplicate = await _context.ParentStudents
                .AnyAsync(ps => ps.ParentId == dto.ParentId && ps.StudentId == dto.StudentId);
            if (duplicate)
                return BadRequest(ApiResponse<ParentLinkResponseDto>.Fail("This parent is already linked to this student."));

            var entity = new ParentStudent
            {
                ParentId = dto.ParentId,
                StudentId = dto.StudentId,
                CreatedAt = DateTime.UtcNow
            };

            _context.ParentStudents.Add(entity);
            await _context.SaveChangesAsync();

            // Re-read with resolved names for a complete response body. Project the raw name
            // parts in SQL, then build the full names in memory (NameFormatter can't be
            // translated inside an IQueryable.Select).
            var row = await _context.ParentStudents
                .Where(ps => ps.Id == entity.Id)
                .Select(ps => new
                {
                    ps.Id,
                    ps.ParentId,
                    ParentFirstName = ps.Parent!.FirstName,
                    ParentMiddleName = ps.Parent.MiddleName,
                    ParentLastName = ps.Parent.LastName,
                    ps.StudentId,
                    StudentFirstName = ps.Student!.FirstName,
                    StudentMiddleName = ps.Student.MiddleName,
                    StudentLastName = ps.Student.LastName,
                    ps.CreatedAt
                })
                .FirstAsync();

            var result = new ParentLinkResponseDto
            {
                Id = row.Id,
                ParentId = row.ParentId,
                ParentName = NameFormatter.FullName(row.ParentFirstName, row.ParentMiddleName, row.ParentLastName),
                StudentId = row.StudentId,
                StudentName = NameFormatter.FullName(row.StudentFirstName, row.StudentMiddleName, row.StudentLastName),
                CreatedAt = row.CreatedAt
            };

            return Ok(ApiResponse<ParentLinkResponseDto>.Ok(result, "Parent linked to student successfully."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create parent link.");
            return StatusCode(500,
                ApiResponse<ParentLinkResponseDto>.Fail("An error occurred while creating the parent link."));
        }
    }

    // [Admin only] Removes one parent-student link by its id.
    /// <summary>DELETE /api/parent-links/{id} — remove a parent↔student link.</summary>
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            var entity = await _context.ParentStudents.FindAsync(id);
            if (entity is null)
                return NotFound(ApiResponse<object>.Fail("Parent link not found."));

            _context.ParentStudents.Remove(entity);
            await _context.SaveChangesAsync();

            return Ok(ApiResponse<object>.Ok(new { id }, "Parent link removed successfully."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete parent link {LinkId}.", id);
            return StatusCode(500,
                ApiResponse<object>.Fail("An error occurred while removing the parent link."));
        }
    }
}
