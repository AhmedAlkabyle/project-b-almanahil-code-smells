// ============================================
// NameFormatter — the ONE place that builds a person's full name.
// Job: join first + (optional) middle + last into a single display name, so the exact
// format lives in a single spot instead of being copy-pasted across many controllers.
// Used by: every place that shows a user's full name (students, teachers, parents, ...).
// EF NOTE: this is a normal C# method, so it CANNOT be called inside an IQueryable.Select()
// (EF can't translate it to SQL). Those queries must project the raw First/Middle/Last
// columns, ToListAsync(), then call this in memory.
// ============================================
namespace AlmanahilAPI.Helpers;

/// <summary>
/// Formats a person's full name from its parts. The middle name is included only when it
/// is present (null/blank is skipped), producing "First Last" or "First Middle Last".
/// </summary>
public static class NameFormatter
{
    /// <summary>Joins the name parts into one full name (middle name included only when present).</summary>
    public static string FullName(string firstName, string? middleName, string lastName) =>
        string.IsNullOrWhiteSpace(middleName)
            ? $"{firstName} {lastName}"
            : $"{firstName} {middleName} {lastName}";
}
