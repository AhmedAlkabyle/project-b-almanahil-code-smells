// ============================================
// TimetableController — the weekly class schedule (Sunday–Thursday).
// Job: loads a class's timetable grid, and saves changes to it. When saving, it also
// checks for clashes — the same subject can't be scheduled at the same time in two
// different classes of the same level (a teacher can't be in two rooms at once).
// Used by: the Vue admin "Timetable" page. Admin-only.
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
/// Admin-only read/save for a class's weekly timetable (Module 2). The grid runs
/// Sunday–Thursday; the number of periods per day comes from the class Level
/// (Secondary = 5, High School = 6). Period clock times are FIXED and computed by
/// <see cref="PeriodSchedule"/>, never stored.
///
/// EF Core + LINQ only; every response uses the standard ApiResponse envelope.
/// NOTE: this is the table + save/load ONLY — there is deliberately NO clash detection
/// here (that comes in the next prompt).
/// </summary>
// This class reads and saves each class's weekly timetable. Admin-only.
[ApiController]
[Route("api/timetable")]
[Authorize(Roles = "Admin")]
public class TimetableController(AlmanahilDbContext context, ILogger<TimetableController> logger) : ControllerBase
{
    private readonly AlmanahilDbContext _context = context;
    private readonly ILogger<TimetableController> _logger = logger;

    // The school week, Sunday (1) .. Thursday (5), with bilingual labels.
    private static readonly TimetableDayDto[] SchoolDays =
    [
        new() { Day = 1, NameEn = "Sunday",    NameAr = "الأحد" },
        new() { Day = 2, NameEn = "Monday",    NameAr = "الاثنين" },
        new() { Day = 3, NameEn = "Tuesday",   NameAr = "الثلاثاء" },
        new() { Day = 4, NameEn = "Wednesday", NameAr = "الأربعاء" },
        new() { Day = 5, NameEn = "Thursday",  NameAr = "الخميس" },
    ];
    private const int FirstDay = 1;
    private const int LastDay = 5;

    private static readonly string[] ArabicOrdinals = ["الأول", "الثاني", "الثالث"]; // grade 1..3

    /// <summary>
    /// GET /api/timetable/{classId} — the class (incl. its Level), the period-time list for
    /// its level (with the breakfast break), the days (Sun–Thu), and the FULL grid of slots
    /// (every day+period cell, empty ones included). Filled cells carry the subject name.
    /// </summary>
    // [Admin only] Loads one class's full weekly timetable: the days (Sun–Thu), the period
    // times for its level, and every day+period cell (empty cells included, filled ones
    // showing the subject name).
    [HttpGet("{classId:int}")]
    public async Task<IActionResult> GetTimetable(int classId)
    {
        try
        {
            // Load the class (just the fields we need to describe it + pick its schedule).
            var cls = await _context.Classes
                .Where(c => c.Id == classId)
                .Select(c => new { c.Id, c.Name, c.Level, c.Grade, c.Section, c.AcademicYear })
                .FirstOrDefaultAsync();
            if (cls is null)
                return NotFound(ApiResponse<TimetableResponseDto>.Fail("Class not found."));

            int periodsPerDay = PeriodSchedule.PeriodsForLevel(cls.Level);
            var (periods, breakInfo) = PeriodSchedule.For(cls.Level);

            // Stored slots for this class, with the subject name resolved via LINQ (LEFT JOIN).
            var stored = await _context.TimetableSlots
                .Where(s => s.ClassId == classId)
                .Select(s => new
                {
                    s.Day,
                    s.Period,
                    s.SubjectId,
                    SubjectName = s.Subject != null ? s.Subject.Name : null
                })
                .ToListAsync();

            // Index stored cells by (day, period) so we can fill the full grid quickly.
            var byCell = stored.ToDictionary(s => (s.Day, s.Period), s => s);

            // Build the complete grid: every day (Sun–Thu) × every period for this level.
            var slots = new List<TimetableSlotDto>(LastDay * periodsPerDay);
            for (int day = FirstDay; day <= LastDay; day++)
            {
                for (int period = 1; period <= periodsPerDay; period++)
                {
                    byCell.TryGetValue((day, period), out var found);
                    slots.Add(new TimetableSlotDto
                    {
                        Day = day,
                        Period = period,
                        SubjectId = found?.SubjectId,
                        SubjectName = found?.SubjectName
                    });
                }
            }

            var dto = new TimetableResponseDto
            {
                ClassId = cls.Id,
                ClassName = cls.Name,
                ClassDisplayName = BuildDisplayName(cls.Name, cls.Level, cls.Grade, cls.Section, cls.AcademicYear),
                Level = cls.Level,
                PeriodsPerDay = periodsPerDay,
                Days = [.. SchoolDays],
                Periods = periods,
                Break = breakInfo,
                Slots = slots
            };

            return Ok(ApiResponse<TimetableResponseDto>.Ok(dto, "Timetable retrieved successfully."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve the timetable for class {ClassId}.", classId);
            return StatusCode(500,
                ApiResponse<TimetableResponseDto>.Fail("An error occurred while retrieving the timetable."));
        }
    }

    /// <summary>
    /// PUT /api/timetable/{classId} — upsert the grid cells the admin sends (update the
    /// existing class+day+period slot, else create one). Each non-null SubjectId must exist
    /// AND belong to THIS class; periods beyond the level's max (e.g. period 6 for Secondary)
    /// are rejected. A null SubjectId clears the cell. Saved in ONE transaction.
    /// (No clash detection here — that is the next prompt.)
    /// </summary>
    // [Admin only] Saves the timetable cells the admin sends. It validates them, checks for
    // clashes with other classes of the same level, and only then saves — all as one
    // all-or-nothing transaction. A cell with no subject clears that slot.
    [HttpPut("{classId:int}")]
    public async Task<IActionResult> SaveTimetable(int classId, [FromBody] UpdateTimetableDto dto)
    {
        try
        {
            // Make sure the class exists (we also need its level to know how many periods a day has).
            var cls = await _context.Classes
                .Where(c => c.Id == classId)
                .Select(c => new { c.Id, c.Level })
                .FirstOrDefaultAsync();
            if (cls is null)
                return NotFound(ApiResponse<object>.Fail("Class not found."));

            var incoming = dto?.Slots ?? [];
            int maxPeriods = PeriodSchedule.PeriodsForLevel(cls.Level);

            // ---- 1) Validate the shape of every cell (day + period ranges) ----
            foreach (var slot in incoming)
            {
                if (slot.Day is < FirstDay or > LastDay)
                    return BadRequest(ApiResponse<object>.Fail(
                        $"Day must be between {FirstDay} (Sunday) and {LastDay} (Thursday)."));

                if (slot.Period < 1 || slot.Period > maxPeriods)
                    return BadRequest(ApiResponse<object>.Fail(
                        $"Period {slot.Period} is not valid for this class — it allows periods 1 to {maxPeriods}."));
            }

            // ---- 2) Validate subjects: each non-null SubjectId must belong to THIS class ----
            // We also grab each subject's NAME here — the clash rule (step 3) compares by
            // subject NAME, because every class owns its own separate subject rows.
            var requestedSubjectIds = incoming
                .Where(s => s.SubjectId.HasValue)
                .Select(s => s.SubjectId!.Value)
                .Distinct()
                .ToList();

            var nameById = new Dictionary<int, string>();
            if (requestedSubjectIds.Count > 0)
            {
                var classSubjects = await _context.Subjects
                    .Where(sub => sub.ClassId == classId && requestedSubjectIds.Contains(sub.Id))
                    .Select(sub => new { sub.Id, sub.Name })
                    .ToListAsync();

                // Anything missing here either doesn't exist or belongs to another class.
                var invalid = requestedSubjectIds.Except(classSubjects.Select(s => s.Id)).ToList();
                if (invalid.Count > 0)
                    return BadRequest(ApiResponse<object>.Fail(
                        $"These subjects don't belong to this class: {string.Join(", ", invalid)}."));

                nameById = classSubjects.ToDictionary(s => s.Id, s => s.Name);
            }

            // Work out the final wanted value for each cell (if the same cell is sent twice,
            // the last one wins).
            // Collapse the payload to the desired state per cell (last one wins on duplicates).
            var desired = new Dictionary<(int Day, int Period), int?>();
            foreach (var slot in incoming)
                desired[(slot.Day, slot.Period)] = slot.SubjectId;

            // ---- 3) CLASH DETECTION -------------------------------------------------------
            // CLASH RULE: the SAME SUBJECT (by NAME) must not sit at the SAME day + period in
            // two DIFFERENT classes OF THE SAME LEVEL (that would need one teacher in two rooms
            // at once).
            //   • "same subject" → same Subject.Name (each class owns its own subject rows, so
            //                       we compare by name, not id).
            //   • "same level"   → both Secondary, or both High School. A Secondary subject and
            //                       a High-School subject at the same time do NOT clash
            //                       (different teachers per level).
            // An intra-grid clash is structurally impossible here: each (Day, Period) is a
            // single cell for this class (unique index + the desired-dict collapse above), so
            // one class cannot place the same subject twice at the same time. The operative
            // check is therefore ACROSS other classes of the same level.
            var thisLevel = cls.Level;

            // Clash step A: build the list of (day, period, subject-name) we're about to set.
            // The (day, period, subjectName) triples we're about to place a subject into.
            var placing = new List<(int Day, int Period, string Name)>();
            foreach (var ((day, period), subjectId) in desired)
            {
                if (subjectId.HasValue && nameById.TryGetValue(subjectId.Value, out var subjectName))
                    placing.Add((day, period, subjectName));
            }

            if (placing.Count > 0)
            {
                var days = placing.Select(p => p.Day).Distinct().ToList();
                var periods = placing.Select(p => p.Period).Distinct().ToList();
                var names = placing.Select(p => p.Name).Distinct().ToList();

                // Clash step B: ask the database for lessons in OTHER classes of the same level
                // that fall on one of our days, one of our periods, and use one of our subject names.
                // Candidate slots in OTHER classes of the SAME level that share one of our
                // cells AND one of our subject names. The broad IN-filters can over-match
                // (right name, but a cell we're not touching), so we confirm the exact
                // (day, period, name) triple in memory just below.
                var candidates = await (
                    from slot in _context.TimetableSlots
                    join sub in _context.Subjects on slot.SubjectId equals sub.Id
                    join cl in _context.Classes on slot.ClassId equals cl.Id
                    where cl.Id != classId
                          && cl.Level == thisLevel
                          && days.Contains(slot.Day)
                          && periods.Contains(slot.Period)
                          && names.Contains(sub.Name)
                    select new { slot.Day, slot.Period, SubjectName = sub.Name, OtherClassName = cl.Name }
                ).ToListAsync();

                // Clash step C: keep only the real clashes — where the exact day+period+subject
                // matches a cell we're actually setting — then tidy them into a clean list.
                var placingSet = placing.Select(p => (p.Day, p.Period, p.Name)).ToHashSet();
                var clashes = candidates
                    .Where(c => placingSet.Contains((c.Day, c.Period, c.SubjectName)))
                    .Select(c => new TimetableClashDto
                    {
                        SubjectName = c.SubjectName,
                        Day = c.Day,
                        Period = c.Period,
                        OtherClassName = c.OtherClassName
                    })
                    .GroupBy(c => (c.SubjectName, c.Day, c.Period, c.OtherClassName))
                    .Select(g => g.First())
                    .OrderBy(c => c.Day).ThenBy(c => c.Period).ThenBy(c => c.SubjectName)
                    .ToList();

                // Clash step D: if any clashes were found, stop and tell the admin — save nothing.
                if (clashes.Count > 0)
                {
                    // Nothing is saved. Build a friendly, specific message and also return the
                    // structured clash list so the frontend can highlight the offending cells.
                    var details = clashes.Select(c =>
                        $"{c.SubjectName} is already scheduled on {DayName(c.Day)} period {c.Period} for class {c.OtherClassName} (same level)");
                    var message = "Cannot save: " + string.Join("; ", details) + ". Please choose a different time.";

                    return BadRequest(new ApiResponse<List<TimetableClashDto>>
                    {
                        Success = false,
                        Message = message,
                        Data = clashes
                    });
                }
            }

            // ---- 4) Upsert in a single transaction ----
            await using var transaction = await _context.Database.BeginTransactionAsync();

            var existing = await _context.TimetableSlots
                .Where(s => s.ClassId == classId)
                .ToListAsync();
            var existingByCell = existing.ToDictionary(s => (s.Day, s.Period));

            var now = DateTime.UtcNow;
            foreach (var ((day, period), subjectId) in desired)
            {
                if (existingByCell.TryGetValue((day, period), out var row))
                {
                    // Update in place (this also clears the cell when subjectId is null).
                    row.SubjectId = subjectId;
                    row.UpdatedAt = now;
                }
                else if (subjectId.HasValue)
                {
                    // Create a row only for a real assignment — no need to persist empty cells.
                    _context.TimetableSlots.Add(new TimetableSlot
                    {
                        ClassId = classId,
                        Day = day,
                        Period = period,
                        SubjectId = subjectId,
                        CreatedAt = now,
                        UpdatedAt = now
                    });
                }
                // (not existing + null subject) → nothing to do; the cell stays empty.
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return Ok(ApiResponse<object>.Ok(new { classId, saved = desired.Count },
                "Timetable saved successfully."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save the timetable for class {ClassId}.", classId);
            return StatusCode(500,
                ApiResponse<object>.Fail("An error occurred while saving the timetable."));
        }
    }

    // ================= Helpers =================

    // Helper: turns a day number (1=Sunday .. 5=Thursday) into its English name, for messages.
    /// <summary>The English day name for a day number (1=Sunday..5=Thursday), for messages.</summary>
    private static string DayName(int day) =>
        SchoolDays.FirstOrDefault(d => d.Day == day)?.NameEn ?? $"day {day}";

    /// <summary>
    /// Human-readable class label, e.g. "1/A — الأول إعدادي 2025/2026". Legacy rows missing
    /// the structured parts fall back to the short Name.
    /// </summary>
    // Helper: builds the nice class label, e.g. "1/A — الأول إعدادي 2025/2026". Older classes
    // missing the newer fields just show their short name.
    private static string BuildDisplayName(string name, string? level, int? grade, string? section, string? academicYear)
    {
        if (string.IsNullOrWhiteSpace(level) || grade is null || string.IsNullOrWhiteSpace(section))
            return name;

        var ordinal = grade is >= 1 and <= 3 ? ArabicOrdinals[grade.Value - 1] : grade.Value.ToString();
        var levelAr = level.Equals("HighSchool", StringComparison.OrdinalIgnoreCase) ? "ثانوي" : "إعدادي";
        var year = string.IsNullOrWhiteSpace(academicYear) ? string.Empty : $" {academicYear}";
        return $"{name} — {ordinal} {levelAr}{year}";
    }
}
