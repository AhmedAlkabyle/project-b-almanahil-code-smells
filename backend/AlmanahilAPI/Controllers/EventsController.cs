// ============================================
// EventsController — the admin's tool to create announcements & events.
// Job: add, view, edit and delete news items, and choose who each one is aimed at
// (all users, all teachers, a single class, etc.).
// Used by: the Vue admin "Announcements/Events" page. Admin-only.
// (Reading your own feed is a different file: EventsFeedController.)
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
/// Admin-only CRUD for announcements &amp; events (Module 2). Mirrors ClassesController:
/// the whole controller is gated to "Admin", EF Core + LINQ only, and every response uses
/// the standard ApiResponse envelope. Each item records the admin who created it.
/// </summary>
// This class manages announcements and events (create/read/update/delete). Admin-only.
[ApiController]
[Route("api/events")]
[Authorize(Roles = "Admin")]
public class EventsController(AlmanahilDbContext context, ILogger<EventsController> logger) : ControllerBase
{
    private readonly AlmanahilDbContext _context = context;
    private readonly ILogger<EventsController> _logger = logger;

    /// <summary>The only Type values the UI offers.</summary>
    private static readonly string[] AllowedTypes = ["Announcement", "Event"];

    /// <summary>The nine audiences an event can target — exactly one per event.</summary>
    private static readonly string[] AllowedAudiences =
    [
        "AllUsers", "AllTeachers", "TeachersSecondary", "TeachersHighSchool",
        "AllParents", "AllStudents", "AllSecondary", "AllHighSchool", "SpecificClass"
    ];

    // ---------------------------------------------------------------------------------
    // NOTE (for the later per-role "my events" views — NOT built yet):
    // When a non-admin fetches their feed, filter events by AudienceType like so:
    //   • Secondary teacher   → AllUsers, AllTeachers, TeachersSecondary, AllSecondary
    //   • High-School teacher  → AllUsers, AllTeachers, TeachersHighSchool, AllHighSchool
    //   • Parent               → AllUsers, AllParents, (their child's level: AllSecondary/
    //                            AllHighSchool), and SpecificClass where TargetClassId == the
    //                            child's ClassId
    //   • Student              → AllUsers, AllStudents, (their level: AllSecondary/
    //                            AllHighSchool), and SpecificClass where TargetClassId == their
    //                            ClassId
    // Admins (this controller) always see every event regardless of audience.
    // ---------------------------------------------------------------------------------

    /// <summary>GET /api/events — all items, newest first by date.</summary>
    // [Admin only] Lists every announcement/event, newest first.
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        try
        {
            // Project the raw creator-name parts in SQL, then build the full name in memory
            // (NameFormatter can't be translated inside an IQueryable.Select).
            var rows = await _context.Events
                .OrderByDescending(e => e.Date)
                .ThenByDescending(e => e.CreatedAt)
                .Select(e => new
                {
                    e.Id,
                    e.Title,
                    e.Description,
                    e.Date,
                    e.Type,
                    e.AudienceType,
                    e.TargetClassId,
                    // Resolve the targeted class's short name via the nav — null unless SpecificClass.
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

            return Ok(ApiResponse<List<EventResponseDto>>.Ok(events, "Events retrieved successfully."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve events.");
            return StatusCode(500,
                ApiResponse<List<EventResponseDto>>.Fail("An error occurred while retrieving events."));
        }
    }

    /// <summary>GET /api/events/{id} — one item, or a friendly not-found.</summary>
    // [Admin only] Gets one announcement/event by its id (or a friendly "not found").
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        try
        {
            // Project the raw creator-name parts in SQL, then build the full name in memory
            // (NameFormatter can't be translated inside an IQueryable.Select).
            var row = await _context.Events
                .Where(e => e.Id == id)
                .Select(e => new
                {
                    e.Id,
                    e.Title,
                    e.Description,
                    e.Date,
                    e.Type,
                    e.AudienceType,
                    e.TargetClassId,
                    // Resolve the targeted class's short name via the nav — null unless SpecificClass.
                    TargetClassName = e.TargetClass != null ? e.TargetClass.Name : null,
                    e.CreatedByUserId,
                    e.CreatedBy!.FirstName,
                    e.CreatedBy.MiddleName,
                    e.CreatedBy.LastName,
                    e.CreatedAt
                })
                .FirstOrDefaultAsync();

            if (row is null)
                return NotFound(ApiResponse<EventResponseDto>.Fail("Event not found."));

            var ev = new EventResponseDto
            {
                Id = row.Id,
                Title = row.Title,
                Description = row.Description,
                Date = row.Date,
                Type = row.Type,
                AudienceType = row.AudienceType,
                TargetClassId = row.TargetClassId,
                TargetClassName = row.TargetClassName,
                CreatedByUserId = row.CreatedByUserId,
                CreatedByName = NameFormatter.FullName(row.FirstName, row.MiddleName, row.LastName),
                CreatedAt = row.CreatedAt
            };

            return Ok(ApiResponse<EventResponseDto>.Ok(ev, "Event retrieved successfully."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve event {EventId}.", id);
            return StatusCode(500,
                ApiResponse<EventResponseDto>.Fail("An error occurred while retrieving the event."));
        }
    }

    /// <summary>
    /// POST /api/events — create an announcement/event. CreatedByUserId is taken from the
    /// logged-in admin's JWT (never trusted from the request body).
    /// </summary>
    // [Admin only] Creates a new announcement/event. The author is taken from the logged-in
    // admin's sign-in token (never trusted from the request body).
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateEventDto dto)
    {
        try
        {
            // Step 1: Check the type is "Announcement" or "Event".
            var type = NormalizeType(dto.Type);
            if (type is null)
                return BadRequest(ApiResponse<EventResponseDto>.Fail("Type must be either 'Announcement' or 'Event'."));

            // Step 2: Check who the event is aimed at (and, if it's one class, that the class exists).
            // Validate the audience and (only for SpecificClass) its target class.
            string? audienceError = null;
            var resolved = await ResolveAudienceAsync(dto.AudienceType, dto.TargetClassId, e => audienceError = e);
            if (resolved is null)
                return BadRequest(ApiResponse<EventResponseDto>.Fail(audienceError!));

            // Step 3: Find out who is creating this (from their sign-in token).
            // Resolve the creator from the validated JWT — not from the client payload.
            if (!TryGetUserId(out var userId))
                return Unauthorized(ApiResponse<EventResponseDto>.Fail("Invalid or missing authentication token."));

            // Step 4: Build and save the new event.
            var entity = new Event
            {
                Title = dto.Title.Trim(),
                Description = dto.Description.Trim(),
                Date = dto.Date,
                Type = type,
                AudienceType = resolved.Value.Audience,
                TargetClassId = resolved.Value.TargetClassId,
                CreatedByUserId = userId,
                CreatedAt = DateTime.UtcNow
            };

            _context.Events.Add(entity);
            await _context.SaveChangesAsync();

            // Re-read with the resolved creator name for a complete response body. Project the
            // raw name parts in SQL, then build the full name in memory (NameFormatter can't be
            // translated inside an IQueryable.Select).
            var row = await _context.Events
                .Where(e => e.Id == entity.Id)
                .Select(e => new
                {
                    e.Id,
                    e.Title,
                    e.Description,
                    e.Date,
                    e.Type,
                    e.AudienceType,
                    e.TargetClassId,
                    // Resolve the targeted class's short name via the nav — null unless SpecificClass.
                    TargetClassName = e.TargetClass != null ? e.TargetClass.Name : null,
                    e.CreatedByUserId,
                    e.CreatedBy!.FirstName,
                    e.CreatedBy.MiddleName,
                    e.CreatedBy.LastName,
                    e.CreatedAt
                })
                .FirstAsync();

            var result = new EventResponseDto
            {
                Id = row.Id,
                Title = row.Title,
                Description = row.Description,
                Date = row.Date,
                Type = row.Type,
                AudienceType = row.AudienceType,
                TargetClassId = row.TargetClassId,
                TargetClassName = row.TargetClassName,
                CreatedByUserId = row.CreatedByUserId,
                CreatedByName = NameFormatter.FullName(row.FirstName, row.MiddleName, row.LastName),
                CreatedAt = row.CreatedAt
            };

            return CreatedAtAction(nameof(GetById), new { id = entity.Id },
                ApiResponse<EventResponseDto>.Ok(result, "Event created successfully."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create event.");
            return StatusCode(500,
                ApiResponse<EventResponseDto>.Fail("An error occurred while creating the event."));
        }
    }

    /// <summary>PUT /api/events/{id} — update an announcement/event (original author is preserved).</summary>
    // [Admin only] Edits an existing announcement/event. The original author stays the same.
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateEventDto dto)
    {
        try
        {
            var entity = await _context.Events.FindAsync(id);
            if (entity is null)
                return NotFound(ApiResponse<EventResponseDto>.Fail("Event not found."));

            var type = NormalizeType(dto.Type);
            if (type is null)
                return BadRequest(ApiResponse<EventResponseDto>.Fail("Type must be either 'Announcement' or 'Event'."));

            // Validate the audience and (only for SpecificClass) its target class.
            string? audienceError = null;
            var resolved = await ResolveAudienceAsync(dto.AudienceType, dto.TargetClassId, e => audienceError = e);
            if (resolved is null)
                return BadRequest(ApiResponse<EventResponseDto>.Fail(audienceError!));

            entity.Title = dto.Title.Trim();
            entity.Description = dto.Description.Trim();
            entity.Date = dto.Date;
            entity.Type = type;
            entity.AudienceType = resolved.Value.Audience;
            entity.TargetClassId = resolved.Value.TargetClassId; // forced null unless SpecificClass
            // CreatedByUserId is intentionally left unchanged — the original author is preserved.

            await _context.SaveChangesAsync();

            // Project the raw creator-name parts in SQL, then build the full name in memory
            // (NameFormatter can't be translated inside an IQueryable.Select).
            var row = await _context.Events
                .Where(e => e.Id == entity.Id)
                .Select(e => new
                {
                    e.Id,
                    e.Title,
                    e.Description,
                    e.Date,
                    e.Type,
                    e.AudienceType,
                    e.TargetClassId,
                    // Resolve the targeted class's short name via the nav — null unless SpecificClass.
                    TargetClassName = e.TargetClass != null ? e.TargetClass.Name : null,
                    e.CreatedByUserId,
                    e.CreatedBy!.FirstName,
                    e.CreatedBy.MiddleName,
                    e.CreatedBy.LastName,
                    e.CreatedAt
                })
                .FirstAsync();

            var result = new EventResponseDto
            {
                Id = row.Id,
                Title = row.Title,
                Description = row.Description,
                Date = row.Date,
                Type = row.Type,
                AudienceType = row.AudienceType,
                TargetClassId = row.TargetClassId,
                TargetClassName = row.TargetClassName,
                CreatedByUserId = row.CreatedByUserId,
                CreatedByName = NameFormatter.FullName(row.FirstName, row.MiddleName, row.LastName),
                CreatedAt = row.CreatedAt
            };

            return Ok(ApiResponse<EventResponseDto>.Ok(result, "Event updated successfully."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update event {EventId}.", id);
            return StatusCode(500,
                ApiResponse<EventResponseDto>.Fail("An error occurred while updating the event."));
        }
    }

    /// <summary>DELETE /api/events/{id} — remove an announcement/event.</summary>
    // [Admin only] Deletes an announcement/event by its id.
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            var entity = await _context.Events.FindAsync(id);
            if (entity is null)
                return NotFound(ApiResponse<object>.Fail("Event not found."));

            _context.Events.Remove(entity);
            await _context.SaveChangesAsync();

            return Ok(ApiResponse<object>.Ok(new { id }, "Event deleted successfully."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete event {EventId}.", id);
            return StatusCode(500,
                ApiResponse<object>.Fail("An error occurred while deleting the event."));
        }
    }

    // ---- Helpers ----

    /// <summary>
    /// Validate/normalize the Type. Returns the canonical 'Announcement' or 'Event'
    /// (case-insensitive match), or null when the value is neither.
    /// </summary>
    // Helper: tidies the type into exactly "Announcement" or "Event" (ignoring casing);
    // returns null if it's neither.
    private static string? NormalizeType(string? type)
    {
        if (string.IsNullOrWhiteSpace(type)) return null;
        return AllowedTypes.FirstOrDefault(t => t.Equals(type.Trim(), StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Validate/normalize the AudienceType. Returns the canonical value (case-insensitive
    /// match), or null when the value isn't one of the nine allowed audiences.
    /// </summary>
    // Helper: tidies the audience into one of the nine allowed values; returns null if unknown.
    private static string? NormalizeAudience(string? audience)
    {
        if (string.IsNullOrWhiteSpace(audience)) return null;
        return AllowedAudiences.FirstOrDefault(a => a.Equals(audience.Trim(), StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Resolve the audience + target class for a create/update. Enforces: SpecificClass
    /// requires a real class; every other audience forces TargetClassId to null. Returns a
    /// BadRequest via <paramref name="error"/> on any problem (else null).
    /// </summary>
    // Helper: works out the final audience + target class. If the audience is one specific
    // class, that class must exist; every other audience means "no single class" (null).
    private async Task<(string Audience, int? TargetClassId)?> ResolveAudienceAsync(
        string? rawAudience, int? rawTargetClassId, Action<string> setError)
    {
        var audience = NormalizeAudience(rawAudience);
        if (audience is null)
        {
            setError("Please choose a valid audience for this event.");
            return null;
        }

        if (audience == "SpecificClass")
        {
            if (rawTargetClassId is null or <= 0)
            {
                setError("Please choose the class this event is for.");
                return null;
            }
            var exists = await _context.Classes.AnyAsync(c => c.Id == rawTargetClassId);
            if (!exists)
            {
                setError("The selected class no longer exists.");
                return null;
            }
            return (audience, rawTargetClassId);
        }

        // Every non-SpecificClass audience ignores any class the client may have sent.
        return (audience, null);
    }

    /// <summary>
    /// Pull the current user's id from the validated JWT. Inbound claims aren't remapped,
    /// so the id may arrive under NameIdentifier, "nameid", or "sub" — check all three
    /// (same approach as AuthController.ChangePassword).
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
