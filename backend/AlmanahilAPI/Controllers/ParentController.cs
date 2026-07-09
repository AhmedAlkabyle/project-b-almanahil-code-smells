// ============================================
// ParentController — the parent's starting point (read-only).
// Job: lets a signed-in parent see the list of their own children, so they can then
// open each child's attendance, grades and materials (served by other controllers).
// It finds the children strictly from the logged-in parent's sign-in token.
// Used by: the Vue parent portal (child picker).
// ============================================
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using AlmanahilAPI.Data;
using AlmanahilAPI.DTOs;
using AlmanahilAPI.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AlmanahilAPI.Controllers;

/// <summary>
/// Parent (Module: Parent portal). A parent views their CHILDREN's data (read-only).
/// EF Core + LINQ only, every response uses the standard ApiResponse envelope.
///
/// RBAC: children are resolved from ParentStudents strictly by the caller's JWT id, so a
/// parent can only ever see students linked to their own account. The child's actual
/// attendance/grades are served by the Attendance/Grades controllers, which independently
/// re-verify the parent↔child link — a parent can never pass some other child's id.
/// </summary>
// This class lets a signed-in parent look up their own children.
// [Authorize] means any signed-in user can call it (only parents actually have children).
[ApiController]
[Route("api/parent")]
[Authorize]
public class ParentController(AlmanahilDbContext context, ILogger<ParentController> logger) : ControllerBase
{
    private readonly AlmanahilDbContext _context = context;
    private readonly ILogger<ParentController> _logger = logger;

    /// <summary>
    /// GET /api/parent/my-children — the students linked to the logged-in parent (from
    /// ParentStudents), for the parent's child picker. Scoped by the JWT id, so a parent
    /// only ever sees their own children. Non-parents simply get an empty list.
    /// </summary>
    // [Any signed-in user] Lists the children linked to the logged-in parent (name + class).
    // Found from the parent's own sign-in token, so a parent only ever sees their own children.
    [HttpGet("my-children")]
    public async Task<IActionResult> MyChildren()
    {
        try
        {
            if (!TryGetUserId(out var parentId))
                return Unauthorized(ApiResponse<List<ParentChildDto>>.Fail("Invalid or missing authentication token."));

            // Project the raw name parts in SQL, then build the full name in memory
            // (NameFormatter can't be translated inside an IQueryable.Select).
            var rows = await _context.ParentStudents
                .Where(ps => ps.ParentId == parentId)
                .OrderBy(ps => ps.Student!.FirstName).ThenBy(ps => ps.Student!.LastName)
                .Select(ps => new
                {
                    ps.StudentId,
                    ps.Student!.FirstName,
                    ps.Student.MiddleName,
                    ps.Student.LastName,
                    ps.Student.ClassId,
                    ClassName = ps.Student.Class != null ? ps.Student.Class.Name : null
                })
                .ToListAsync();

            var children = rows.Select(ps => new ParentChildDto
            {
                StudentId = ps.StudentId,
                StudentName = NameFormatter.FullName(ps.FirstName, ps.MiddleName, ps.LastName),
                ClassId = ps.ClassId,
                ClassName = ps.ClassName
            }).ToList();

            return Ok(ApiResponse<List<ParentChildDto>>.Ok(children, "Your children retrieved successfully."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve the parent's children.");
            return StatusCode(500, ApiResponse<List<ParentChildDto>>.Fail("An error occurred while loading your children."));
        }
    }

    // ---- Helpers ----

    /// <summary>
    /// Pull the current user's id from the validated JWT (NameIdentifier / "nameid" / "sub").
    /// Same approach as the Attendance / Grades / Events controllers.
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
