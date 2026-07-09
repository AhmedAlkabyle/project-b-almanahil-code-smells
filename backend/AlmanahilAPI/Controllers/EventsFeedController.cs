// ============================================
// EventsFeedController — shows each person the news meant for them.
// Job: builds one user's personal list of announcements/events based on their role
// (teacher/student/parent/admin), their level, and their class(es).
// Used by: the Vue "My Events / Home feed" for every signed-in user.
// (Admins creating/editing events use the other file: EventsController.)
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
/// The per-user events feed (Module 2). Separate from the Admin-only <see cref="EventsController"/>
/// so it can be reached by ANY authenticated user while the CRUD controller stays gated to Admin.
/// EF Core + LINQ only; the caller is always resolved from the JWT — never from the client.
///
/// A user sees an event when its AudienceType matches them:
///  • Teacher  → AllUsers, AllTeachers, + (Secondary → TeachersSecondary, AllSecondary) /
///               (HighSchool → TeachersHighSchool, AllHighSchool).
///  • Student  → AllUsers, AllStudents, + their class's level (AllSecondary/AllHighSchool),
///               + SpecificClass where TargetClassId == their ClassId.
///  • Parent   → AllUsers, AllParents, + for EACH child: the child's level and SpecificClass
///               where TargetClassId == that child's ClassId.
///  • Admin    → every event.
/// </summary>
// This class builds the personal events feed for whoever is signed in.
// [Authorize] means any signed-in user can use it (each only sees their own relevant events).
[ApiController]
[Route("api/events")]
[Authorize]
public class EventsFeedController(AlmanahilDbContext context, ILogger<EventsFeedController> logger) : ControllerBase
{
    private readonly AlmanahilDbContext _context = context;
    private readonly ILogger<EventsFeedController> _logger = logger;

    /// <summary>
    /// GET /api/events/my — the events targeted at the logged-in user. Upcoming first
    /// (soonest → latest), then past (most recent → oldest). Caller comes from the JWT.
    /// </summary>
    // [Any signed-in user] Returns the events that apply to the logged-in user. Upcoming
    // events come first (soonest first), then past events (most recent first).
    [HttpGet("my")]
    public async Task<IActionResult> My()
    {
        try
        {
            // Step 1: Find out who is asking (from their sign-in token).
            if (!TryGetUserId(out var userId))
                return Unauthorized(ApiResponse<List<EventResponseDto>>.Fail("Invalid or missing authentication token."));

            // Step 2: Load the user's role, level and class (these decide what they may see).
            var user = await _context.Users
                .Where(u => u.Id == userId)
                .Select(u => new { u.Id, u.Role, u.TeacherLevel, u.ClassId })
                .FirstOrDefaultAsync();

            if (user is null)
                return Unauthorized(ApiResponse<List<EventResponseDto>>.Fail("Invalid or missing authentication token."));

            // Step 3: Work out which "audience" groups this user belongs to, and which class
            // ids matter to them (used to match class-specific events).
            // Build the set of broadcast audiences this user qualifies for, plus the class ids
            // that make a SpecificClass event relevant to them.
            var audiences = new HashSet<string> { "AllUsers" };
            var classIds = new List<int>();
            var adminSeesAll = false;

            switch (user.Role)
            {
                case UserRole.Admin:
                    adminSeesAll = true;
                    break;

                case UserRole.Teacher:
                    audiences.Add("AllTeachers");
                    if (user.TeacherLevel == "Secondary")
                    {
                        audiences.Add("TeachersSecondary");
                        audiences.Add("AllSecondary");
                    }
                    else if (user.TeacherLevel == "HighSchool")
                    {
                        audiences.Add("TeachersHighSchool");
                        audiences.Add("AllHighSchool");
                    }
                    break;

                case UserRole.Student:
                    audiences.Add("AllStudents");
                    if (user.ClassId is int studentClassId)
                    {
                        classIds.Add(studentClassId);
                        var level = await _context.Classes
                            .Where(c => c.Id == studentClassId)
                            .Select(c => c.Level)
                            .FirstOrDefaultAsync();
                        AddLevelAudience(audiences, level);
                    }
                    break;

                case UserRole.Parent:
                    audiences.Add("AllParents");
                    var children = await _context.ParentStudents
                        .Where(ps => ps.ParentId == userId)
                        .Select(ps => new { ps.Student!.ClassId, Level = ps.Student.Class != null ? ps.Student.Class.Level : null })
                        .ToListAsync();
                    foreach (var child in children)
                    {
                        if (child.ClassId is int childClassId) classIds.Add(childClassId);
                        AddLevelAudience(audiences, child.Level);
                    }
                    break;
            }

            // Step 4: Fetch the matching events (admins skip the filter and get everything).
            // Base query — admins bypass the audience filter and see everything.
            var query = _context.Events.AsQueryable();
            if (!adminSeesAll)
            {
                var audienceList = audiences.ToList();
                var classIdList = classIds.Distinct().ToList();
                query = query.Where(e =>
                    audienceList.Contains(e.AudienceType)
                    || (e.AudienceType == "SpecificClass"
                        && e.TargetClassId != null
                        && classIdList.Contains(e.TargetClassId.Value)));
            }

            // Project the raw creator-name parts in SQL, then build the full name in memory
            // (NameFormatter can't be translated inside an IQueryable.Select).
            var rows = await query
                .Select(e => new
                {
                    e.Id,
                    e.Title,
                    e.Description,
                    e.Date,
                    e.Type,
                    e.AudienceType,
                    e.TargetClassId,
                    TargetClassName = e.TargetClass != null ? e.TargetClass.Name : null,
                    e.CreatedByUserId,
                    e.CreatedBy!.FirstName,
                    e.CreatedBy.MiddleName,
                    e.CreatedBy.LastName,
                    e.CreatedAt
                })
                .ToListAsync();

            var events = rows.Select(e => new EventResponseDto
            {
                Id = e.Id,
                Title = e.Title,
                Description = e.Description,
                Date = e.Date,
                Type = e.Type,
                AudienceType = e.AudienceType,
                TargetClassId = e.TargetClassId,
                TargetClassName = e.TargetClassName,
                CreatedByUserId = e.CreatedByUserId,
                CreatedByName = NameFormatter.FullName(e.FirstName, e.MiddleName, e.LastName),
                CreatedAt = e.CreatedAt
            }).ToList();

            // Step 5: Sort for a nice reading order — upcoming soonest first, then past.
            // Order for a reader's feed: upcoming soonest-first, then past most-recent-first.
            var now = DateTime.Now;
            var ordered = events.Where(e => e.Date >= now).OrderBy(e => e.Date)
                .Concat(events.Where(e => e.Date < now).OrderByDescending(e => e.Date))
                .ToList();

            return Ok(ApiResponse<List<EventResponseDto>>.Ok(ordered, "Your events retrieved successfully."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve the user's events feed.");
            return StatusCode(500, ApiResponse<List<EventResponseDto>>.Fail("An error occurred while loading your events."));
        }
    }

    // ---- Helpers ----

    // Helper: adds the "all of this level" audience (Secondary or High School) to the set.
    /// <summary>Map a class/teacher level string to its "all-level" audience and add it to the set.</summary>
    private static void AddLevelAudience(HashSet<string> audiences, string? level)
    {
        if (level == "Secondary") audiences.Add("AllSecondary");
        else if (level == "HighSchool") audiences.Add("AllHighSchool");
    }

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
