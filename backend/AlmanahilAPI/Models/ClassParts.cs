// ============================================
// ClassParts — the four fields that describe a class, travelling together.
// Job: replace the (academicYear, level, grade, section) quartet that used to be passed
// around as loose parameters (and 4 `out` params) with a single small value object.
// Used by: ClassesController.NormalizeClassParts and its Create/Update call sites.
// ============================================
namespace AlmanahilAPI.Models;

/// <summary>
/// The validated, normalized parts that make up a class: academic year (e.g. "2025/2026"),
/// level ("Secondary"/"HighSchool"), grade (1–3) and section ("A"–"D"). Bundled together
/// because they always travel as a group (Fowler: Introduce Parameter Object / Extract Class).
/// </summary>
public record ClassParts(string AcademicYear, string Level, int Grade, string Section);
