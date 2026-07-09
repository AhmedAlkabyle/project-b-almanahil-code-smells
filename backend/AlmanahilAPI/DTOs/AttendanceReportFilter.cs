// ============================================
// AttendanceReportFilter — the optional filters for the admin attendance report.
// Job: bundle the five query-string filters (class, subject, student, from/to dates) that
// used to be five separate method parameters into a single parameter object.
// Used by: AttendanceController.Reports, bound with [FromQuery].
// ============================================
using Microsoft.AspNetCore.Mvc;

namespace AlmanahilAPI.DTOs;

// Travels: website -> server (as query-string values). All fields optional.
/// <summary>
/// The optional filters for GET /api/attendance/reports. ASP.NET binds each public property
/// from the query string; the date properties keep the original "from"/"to" query names via
/// <see cref="FromQueryAttribute"/> so existing API calls work unchanged.
/// </summary>
public class AttendanceReportFilter
{
    // Only records whose subject belongs to this class.
    public int? ClassId { get; set; }

    // Only records for this subject.
    public int? SubjectId { get; set; }

    // Only records for this student.
    public int? StudentId { get; set; }

    // On/after this date (query name stays "from").
    [FromQuery(Name = "from")]
    public DateOnly? FromDate { get; set; }

    // On/before this date (query name stays "to").
    [FromQuery(Name = "to")]
    public DateOnly? ToDate { get; set; }
}
