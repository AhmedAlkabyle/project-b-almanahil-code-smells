// PART B — Question 6 (BONUS): Performance Comparison
// Benchmarks the ORIGINAL code patterns vs the REFACTORED patterns.
// Usage: dotnet run -c Release   (no database required — isolates the
// structural overhead introduced by the refactoring techniques)

using System.Diagnostics;

const int Iterations = 100_000;
long guard = 0; // prevents the compiler from optimizing the loops away

Console.WriteLine("==============================================================");
Console.WriteLine(" PART B — Performance Comparison: Original vs Refactored Code");
Console.WriteLine("==============================================================");
Console.WriteLine($"Running {Iterations:N0} iterations per benchmark...\n");

var names = new (string First, string? Middle, string Last)[]
{
    ("Ahmed", "Mohamed", "Ali"),
    ("Sara", null, "Hassan"),
    ("Omar", "Khalid", "Saleh"),
};

// ---- BENCHMARK 1: Full-name formatting (Smell #2, Duplicated Code) ----
// Original: inline concatenation (as it was duplicated in 19 sites)
var sw = Stopwatch.StartNew();
for (int i = 0; i < Iterations; i++)
{
    var n = names[i % names.Length];
    string s = n.First + (n.Middle != null ? " " + n.Middle : "") + " " + n.Last;
    guard += s.Length;
}
sw.Stop();
double original1 = sw.Elapsed.TotalMilliseconds;

// Refactored: one shared helper (NameFormatter.FullName)
sw.Restart();
for (int i = 0; i < Iterations; i++)
{
    var n = names[i % names.Length];
    string s = NameFormatter.FullName(n.First, n.Middle, n.Last);
    guard += s.Length;
}
sw.Stop();
double refactored1 = sw.Elapsed.TotalMilliseconds;
Report("1. Duplicated Code (name formatting)", original1, refactored1);

// ---- BENCHMARK 2: 5 parameters vs parameter object (Smell #3) ----
sw.Restart();
for (int i = 0; i < Iterations; i++)
    guard += ReportsOriginal(i, i + 1, i + 2,
        DateOnly.FromDayNumber(700000 + (i % 300)),
        DateOnly.FromDayNumber(700100 + (i % 300)));
sw.Stop();
double original2 = sw.Elapsed.TotalMilliseconds;

sw.Restart();
for (int i = 0; i < Iterations; i++)
{
    var filter = new AttendanceReportFilter
    {
        ClassId = i, SubjectId = i + 1, StudentId = i + 2,
        FromDate = DateOnly.FromDayNumber(700000 + (i % 300)),
        ToDate = DateOnly.FromDayNumber(700100 + (i % 300)),
    };
    guard += ReportsRefactored(filter);
}
sw.Stop();
double refactored2 = sw.Elapsed.TotalMilliseconds;
Report("2. Long Parameter List (report filter)", original2, refactored2);

// ---- BENCHMARK 3: inline pipeline vs extracted methods (Smell #1) ----
sw.Restart();
for (int i = 0; i < Iterations; i++)
{
    // Original shape: all validation logic inline in one method body
    string email = "user" + (i % 50) + "@almanahilschool.com";
    bool ok = true;
    if (email.Length < 5) ok = false;
    if (!email.Contains('@')) ok = false;
    if (!email.EndsWith("@almanahilschool.com", StringComparison.OrdinalIgnoreCase)) ok = false;
    string middle = (i % 2 == 0) ? "Mohamed" : "";
    if (string.IsNullOrWhiteSpace(middle) && (i % 3 == 0)) ok = false;
    guard += ok ? 1 : 0;
}
sw.Stop();
double original3 = sw.Elapsed.TotalMilliseconds;

sw.Restart();
for (int i = 0; i < Iterations; i++)
{
    // Refactored shape: the same logic behind extracted method calls
    string email = "user" + (i % 50) + "@almanahilschool.com";
    string middle = (i % 2 == 0) ? "Mohamed" : "";
    guard += ValidateBasics(email, middle, i) ? 1 : 0;
}
sw.Stop();
double refactored3 = sw.Elapsed.TotalMilliseconds;
Report("3. Long Method (validate pipeline)", original3, refactored3);

// ---- Summary ----
Console.WriteLine("\n==============================================================");
Console.WriteLine(" OVERALL SUMMARY");
Console.WriteLine("==============================================================");
Console.WriteLine($"{"Benchmark",-42}{"Original",12}{"Refactored",12}{"Diff",10}");
Console.WriteLine(new string('-', 76));
PrintRow("1. Duplicated Code (name formatting)", original1, refactored1);
PrintRow("2. Long Parameter List (report filter)", original2, refactored2);
PrintRow("3. Long Method (validate pipeline)", original3, refactored3);
Console.WriteLine(new string('-', 76));
PrintRow("TOTAL", original1 + original2 + original3, refactored1 + refactored2 + refactored3);
Console.WriteLine($"\n(guard={guard} — ignore; it only prevents dead-code elimination)");

static void Report(string name, double orig, double refac)
{
    Console.WriteLine($"--- {name} ---");
    Console.WriteLine($"  Original:    {orig,8:F2} ms");
    Console.WriteLine($"  Refactored:  {refac,8:F2} ms");
    Console.WriteLine($"  Difference:  {refac - orig,8:F2} ms ({(refac - orig) / orig * 100:+0.00;-0.00}%)\n");
}

static void PrintRow(string name, double orig, double refac) =>
    Console.WriteLine($"{name,-42}{orig,10:F2}ms{refac,10:F2}ms{(refac - orig) / orig * 100,9:+0.0;-0.0}%");

static long ReportsOriginal(int? classId, int? subjectId, int? studentId, DateOnly? from, DateOnly? to) =>
    (classId ?? 0) + (subjectId ?? 0) + (studentId ?? 0) + (from?.DayNumber ?? 0) + (to?.DayNumber ?? 0);

static long ReportsRefactored(AttendanceReportFilter f) =>
    (f.ClassId ?? 0) + (f.SubjectId ?? 0) + (f.StudentId ?? 0) + (f.FromDate?.DayNumber ?? 0) + (f.ToDate?.DayNumber ?? 0);

static bool ValidateBasics(string email, string? middle, int i)
{
    if (email.Length < 5) return false;
    if (!email.Contains('@')) return false;
    if (!email.EndsWith("@almanahilschool.com", StringComparison.OrdinalIgnoreCase)) return false;
    if (string.IsNullOrWhiteSpace(middle) && (i % 3 == 0)) return false;
    return true;
}

public static class NameFormatter
{
    public static string FullName(string firstName, string? middleName, string lastName) =>
        string.IsNullOrWhiteSpace(middleName)
            ? $"{firstName} {lastName}"
            : $"{firstName} {middleName} {lastName}";
}

public class AttendanceReportFilter
{
    public int? ClassId { get; set; }
    public int? SubjectId { get; set; }
    public int? StudentId { get; set; }
    public DateOnly? FromDate { get; set; }
    public DateOnly? ToDate { get; set; }
}