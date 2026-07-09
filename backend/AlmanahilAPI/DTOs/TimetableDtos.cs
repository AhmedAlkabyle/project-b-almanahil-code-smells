// ============================================
// TimetableDtos — the data parcels for a class's weekly timetable.
// A DTO is a simple 'data parcel' the website and server pass back and forth.
// This file holds the parcels for the grid, its times, days, and saving edits.
// ============================================
namespace AlmanahilAPI.DTOs;

// =====================================================================================
// Timetable DTOs (Module 2 — class weekly timetable). All times are FIXED and computed
// by PeriodSchedule (never stored per row). Grouped in one file like AuthDtos.cs since
// they all belong to the same feature.
// =====================================================================================

// Travels: server -> website. The start/end clock time for one lesson period.
/// <summary>
/// The fixed clock time for one period, e.g. { period: 1, startTime: "08:00", endTime: "08:55" }.
/// Computed by <c>PeriodSchedule.For(level)</c>; not stored in the database.
/// </summary>
public class PeriodTimeDto
{
    public int Period { get; set; }
    public string StartTime { get; set; } = string.Empty; // "HH:mm", e.g. "08:00"
    public string EndTime { get; set; } = string.Empty;   // "HH:mm", e.g. "08:55"
}

// Travels: server -> website. The one daily breakfast break and when it happens.
/// <summary>
/// The single daily break (breakfast) that falls after <see cref="AfterPeriod"/>.
/// Bilingual label so the timetable UI can show it in either language.
/// </summary>
public class BreakTimeDto
{
    public string LabelEn { get; set; } = "Breakfast";
    public string LabelAr { get; set; } = "الإفطار";
    public string StartTime { get; set; } = string.Empty; // "10:55"
    public string EndTime { get; set; } = string.Empty;   // "11:25"
    public int AfterPeriod { get; set; }                  // the break sits right after this period (3)
}

// Travels: server -> website. One day column (Sunday to Thursday) in the timetable.
/// <summary>A school day column (Sunday–Thursday), with bilingual labels.</summary>
public class TimetableDayDto
{
    public int Day { get; set; }                       // 1 = Sunday … 5 = Thursday
    public string NameEn { get; set; } = string.Empty; // "Sunday"
    public string NameAr { get; set; } = string.Empty; // "الأحد"
}

// Travels: server -> website. One box in the timetable grid (a day + period), maybe with a subject.
/// <summary>
/// One cell of the grid returned to the client. <see cref="SubjectName"/> is resolved via a
/// LINQ join to Subjects and is null when the cell is empty.
/// </summary>
public class TimetableSlotDto
{
    public int Day { get; set; }             // 1 = Sunday … 5 = Thursday
    public int Period { get; set; }          // 1..5 (Secondary) or 1..6 (High School)
    public int? SubjectId { get; set; }      // null = empty cell
    public string? SubjectName { get; set; } // null when empty
}

// Travels: server -> website. The whole timetable for one class (days, times, and every cell).
/// <summary>The full timetable payload returned by GET /api/timetable/{classId}.</summary>
public class TimetableResponseDto
{
    public int ClassId { get; set; }
    public string ClassName { get; set; } = string.Empty;        // e.g. "1/A"
    public string ClassDisplayName { get; set; } = string.Empty; // e.g. "1/A — الأول إعدادي 2025/2026"
    public string? Level { get; set; }                           // "Secondary" | "HighSchool"
    public int PeriodsPerDay { get; set; }                       // 5 or 6

    public List<TimetableDayDto> Days { get; set; } = [];        // Sun..Thu
    public List<PeriodTimeDto> Periods { get; set; } = [];       // period-time list for the level
    public BreakTimeDto Break { get; set; } = new();             // breakfast break info

    /// <summary>Every cell of the grid (5 days × PeriodsPerDay), empty cells included.</summary>
    public List<TimetableSlotDto> Slots { get; set; } = [];
}

// Travels: website -> server. The timetable cells the admin wants to save.
/// <summary>
/// Body for PUT /api/timetable/{classId} — the grid cells the admin wants to save.
/// Only the cells sent are touched; unsent cells are left as-is.
/// </summary>
public class UpdateTimetableDto
{
    public List<UpdateTimetableSlotDto> Slots { get; set; } = [];
}

// Travels: website -> server. One cell to save; leaving the subject empty clears that box.
/// <summary>One cell in the save payload. A null <see cref="SubjectId"/> clears the cell.</summary>
public class UpdateTimetableSlotDto
{
    public int Day { get; set; }        // 1 = Sunday … 5 = Thursday
    public int Period { get; set; }     // 1..5 or 1..6 depending on the class level
    public int? SubjectId { get; set; } // null = empty this cell
}

// Travels: server -> website. A warning that the same subject already sits at this day/period elsewhere.
/// <summary>
/// One detected timetable clash: the same subject (by NAME) already scheduled at the same
/// day + period in another class OF THE SAME LEVEL. Returned (as a list) in the failed save
/// response so the frontend can highlight the offending cells.
/// </summary>
public class TimetableClashDto
{
    public string SubjectName { get; set; } = string.Empty;
    public int Day { get; set; }                          // 1 = Sunday … 5 = Thursday
    public int Period { get; set; }
    public string OtherClassName { get; set; } = string.Empty; // the class it already sits in
}
