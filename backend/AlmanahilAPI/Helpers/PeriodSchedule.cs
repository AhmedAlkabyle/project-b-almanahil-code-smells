// ============================================
// PeriodSchedule — works out the daily class-period times.
// Job: the school day always follows fixed rules (start 08:00, 55-min periods,
// 5-min gaps, and a 30-min breakfast after period 3). This file calculates the
// start and end time of each period from those rules, so we don't store them.
// Used by: the timetable API when it returns period times to the website.
// ============================================
using AlmanahilAPI.DTOs;

namespace AlmanahilAPI.Helpers;

/// <summary>
/// The school's FIXED daily period schedule. These times are computed here and returned
/// by the API — they are NEVER stored per timetable row.
///
/// Rules: the day starts at 08:00, every period is 55 minutes, there is a 5-minute gap
/// between periods, and a 30-minute breakfast break falls after period 3. Secondary
/// (إعدادي) classes have 5 periods/day; High School (ثانوي) classes have 6.
///
/// Resulting times:
///   P1 08:00–08:55, P2 09:00–09:55, P3 10:00–10:55, [Breakfast 10:55–11:25],
///   P4 11:25–12:20, P5 12:25–13:20, P6 13:25–14:20 (P6 High School only).
/// </summary>
// Helper class holding the school's fixed timing rules and the math to apply them.
public static class PeriodSchedule
{
    // The fixed rules as simple numbers (in minutes) used by the calculation below.
    // Each lesson is 55 minutes long.
    public const int PeriodMinutes = 55;
    // 5-minute gap between lessons.
    public const int GapMinutes = 5;
    // Breakfast break happens right after period 3.
    public const int BreakAfterPeriod = 3;
    // The breakfast break lasts 30 minutes.
    public const int BreakMinutes = 30;

    // The school day always begins at 08:00.
    private static readonly TimeOnly DayStart = new(8, 0);

    /// <summary>Periods per day for a level: High School = 6, otherwise (Secondary) = 5.</summary>
    // Tells how many periods a class has: High School gets 6, everyone else gets 5.
    public static int PeriodsForLevel(string? level) =>
        string.Equals(level, "HighSchool", StringComparison.OrdinalIgnoreCase) ? 6 : 5;

    /// <summary>
    /// The period-time list for a level plus the breakfast break. The 30-minute break is
    /// inserted right after period 3, so periods 4+ start later. Returns 5 periods for
    /// Secondary, 6 for High School.
    /// </summary>
    // Builds the full list of period times (plus the breakfast time) for a class level.
    // Receives the level ("HighSchool" or not); returns the periods list and the break info.
    public static (List<PeriodTimeDto> Periods, BreakTimeDto Break) For(string? level)
    {
        // Step 1: Decide how many periods this level has, and prepare empty lists to fill.
        int count = PeriodsForLevel(level);
        var periods = new List<PeriodTimeDto>(count);
        var breakInfo = new BreakTimeDto { AfterPeriod = BreakAfterPeriod };

        // Step 2: Start a 'cursor' clock at 08:00. We move it forward as we add periods.
        var cursor = DayStart;
        for (int p = 1; p <= count; p++)
        {
            // Step 3: This period ends 55 minutes after it starts. Save its start and end.
            var end = cursor.AddMinutes(PeriodMinutes);
            periods.Add(new PeriodTimeDto
            {
                Period = p,
                StartTime = cursor.ToString("HH:mm"),
                EndTime = end.ToString("HH:mm")
            });

            // Step 4: If we just finished period 3, breakfast happens now (30 minutes),
            // and the next period starts straight after breakfast.
            if (p == BreakAfterPeriod)
            {
                // Breakfast starts the moment period 3 ends (no 5-minute gap before it).
                var breakEnd = end.AddMinutes(BreakMinutes);
                breakInfo.StartTime = end.ToString("HH:mm");
                breakInfo.EndTime = breakEnd.ToString("HH:mm");
                cursor = breakEnd; // the next period starts right after breakfast
            }
            else
            {
                // Step 5: Otherwise, add the normal 5-minute gap before the next period.
                cursor = end.AddMinutes(GapMinutes);
            }
        }

        // Step 6: Hand back the finished list of period times and the breakfast times.
        return (periods, breakInfo);
    }
}
