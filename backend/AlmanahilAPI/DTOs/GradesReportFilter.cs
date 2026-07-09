// ============================================
// GradesReportFilter — the optional filters for the admin grade report.
// Job: bundle the four query-string filters (class, subject, student, assessment type) that
// used to be four separate method parameters into a single parameter object.
// Used by: GradesController.Reports, bound with [FromQuery].
// ============================================
namespace AlmanahilAPI.DTOs;

// Travels: website -> server (as query-string values). All fields optional.
/// <summary>
/// The optional filters for GET /api/grades/reports. ASP.NET binds each public property from
/// the query string by its name (classId, subjectId, studentId, assessmentType), so existing
/// API calls work unchanged.
/// </summary>
public class GradesReportFilter
{
    // Only records whose subject belongs to this class.
    public int? ClassId { get; set; }

    // Only records for this subject.
    public int? SubjectId { get; set; }

    // Only records for this student.
    public int? StudentId { get; set; }

    // Only records of this assessment type (Quiz/Midterm/Final/Homework).
    public string? AssessmentType { get; set; }
}
