// ============================================
// DashboardController — the numbers for the admin home page.
// Job: counts things (users, students, teachers, parents, admins, classes, subjects)
// so the dashboard can show them on summary cards.
// Used by: the Vue admin dashboard. Admin-only.
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
/// Admin dashboard summary counts (Module 2). Admin only, EF Core + LINQ only, every
/// response in the standard ApiResponse envelope.
/// </summary>
// This class provides the summary counts for the admin dashboard. Admin-only.
[ApiController]
[Route("api/dashboard")]
[Authorize(Roles = "Admin")]
public class DashboardController(AlmanahilDbContext context, ILogger<DashboardController> logger) : ControllerBase
{
    private readonly AlmanahilDbContext _context = context;
    private readonly ILogger<DashboardController> _logger = logger;

    /// <summary>GET /api/dashboard/stats — headline counts for the dashboard cards.</summary>
    // [Admin only] Counts everything the dashboard cards show (total users, students,
    // teachers, parents, admins, classes, subjects) and returns them together.
    [HttpGet("stats")]
    public async Task<IActionResult> GetStats()
    {
        try
        {
            var stats = new DashboardStatsDto
            {
                TotalUsers = await _context.Users.CountAsync(),
                TotalStudents = await _context.Users.CountAsync(u => u.Role == UserRole.Student),
                TotalTeachers = await _context.Users.CountAsync(u => u.Role == UserRole.Teacher),
                TotalParents = await _context.Users.CountAsync(u => u.Role == UserRole.Parent),
                TotalAdmins = await _context.Users.CountAsync(u => u.Role == UserRole.Admin),
                TotalClasses = await _context.Classes.CountAsync(),
                TotalSubjects = await _context.Subjects.CountAsync()
            };

            return Ok(ApiResponse<DashboardStatsDto>.Ok(stats, "Dashboard stats retrieved successfully."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve dashboard stats.");
            return StatusCode(500,
                ApiResponse<DashboardStatsDto>.Fail("An error occurred while retrieving dashboard stats."));
        }
    }
}
