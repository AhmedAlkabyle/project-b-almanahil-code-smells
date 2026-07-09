// ============================================
// DevSeedController — TEMPORARY test-data seeder — REMOVE BEFORE DEPLOYMENT.
// Job: quickly fills the database with fake parents, students and subjects so the admin
// dashboard has realistic data to demo, without typing them all in by hand.
// It sends NO emails and can be re-run safely (it skips anything that already exists).
// Used by: developers only (run once from Swagger), then DELETE this whole file. Admin-only.
// ============================================
using AlmanahilAPI.Data;
using AlmanahilAPI.Helpers;
using AlmanahilAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AlmanahilAPI.Controllers;

// =====================================================================================
// TODO: REMOVE before deployment
// -------------------------------------------------------------------------------------
// TEMPORARY, one-time DEVELOPMENT seed. Bulk-inserts test Parents + Students so the admin
// dashboard has realistic data to work with, WITHOUT hand-entering them. Admin-only.
// Reuses the existing DbContext, User/ParentStudent models, and PasswordHasher (BCrypt).
// EF Core + LINQ only, no raw SQL, and it sends NO emails.
// Run it once from Swagger, then DELETE THIS WHOLE FILE.
// =====================================================================================
// TEMPORARY class: creates fake test users and subjects. Admin-only. Delete before launch.
[ApiController]
[Route("api/dev")]
[Authorize(Roles = "Admin")]
public class DevSeedController(AlmanahilDbContext context) : ControllerBase
{
    private readonly AlmanahilDbContext _context = context;

    // Every seeded account shares this password. It is hashed with BCrypt before being
    // stored and is NEVER returned in the response.
    private const string SeedPassword = "Almanahil@2026";

    private const int ParentCount = 20;
    private const int StudentCount = 40;

    /// <summary>
    /// POST api/dev/seed-test-users — creates 20 Parents + 40 Students (each Student linked
    /// to one Parent and one existing Class, spread evenly). Re-running SKIPS any email that
    /// already exists, so it never duplicates. TODO: REMOVE before deployment.
    /// </summary>
    // [Admin only, TEMPORARY] Creates 20 fake parents + 40 fake students, links each student
    // to a parent and an existing class. Skips any email that already exists (safe to re-run).
    [HttpPost("seed-test-users")]
    public async Task<IActionResult> SeedTestUsers()
    {
        try
        {
            // Step 1: There must be at least one class to put students in — stop if there isn't.
            // Students must be assigned to a real class — bail out early if none exist.
            var classIds = await _context.Classes
                .OrderBy(c => c.Id)
                .Select(c => c.Id)
                .ToListAsync();
            if (classIds.Count == 0)
                return BadRequest(ApiResponse<string>.Fail("Create at least one class first."));

            // Step 2: Scramble the shared password once and reuse it for every fake account.
            // Hash the shared password ONCE (BCrypt salts internally) and reuse the hash for
            // every seeded row — all of them verify against the same plain password.
            var passwordHash = PasswordHasher.HashPassword(SeedPassword);

            // Pull existing emails so a re-run SKIPS anything already there (no duplicates).
            var existingEmails = (await _context.Users
                    .Select(u => u.Email)
                    .ToListAsync())
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var people = BuildPeople(ParentCount + StudentCount);
            var now = DateTime.UtcNow;
            var rnd = new Random(2026); // fixed seed → reproducible phones / birth dates

            int parentsCreated = 0, parentsSkipped = 0;
            int studentsCreated = 0, studentsSkipped = 0, linksCreated = 0;

            // Step 3: Add the parents (skipping any whose email already exists).
            // ---- 1) Parents ----
            var parentEmails = new string[ParentCount];
            for (int i = 0; i < ParentCount; i++)
            {
                var (first, last, gender) = people[i];
                var email = MakeEmail(first, last, "parent");
                parentEmails[i] = email;

                if (existingEmails.Contains(email)) { parentsSkipped++; continue; }

                _context.Users.Add(new User
                {
                    FirstName = first,
                    LastName = last,
                    Email = email,
                    PasswordHash = passwordHash,
                    Role = UserRole.Parent,
                    Gender = gender,
                    DateOfBirth = RandomDate(rnd, 1975, 1990),
                    PhoneNumber = RandomPhone(rnd),
                    IsActive = true,
                    IsFirstLogin = true,
                    CreatedAt = now
                });
                existingEmails.Add(email);
                parentsCreated++;
            }

            // Persist parents first so newly-created ones get their Id (needed to link students).
            await _context.SaveChangesAsync();

            // Resolve every parent's Id by email — covers both just-created and pre-existing rows.
            var parentIdByEmail = await _context.Users
                .Where(u => parentEmails.Contains(u.Email))
                .ToDictionaryAsync(u => u.Email, u => u.Id);

            // Step 4: Add the students, giving each one a parent and a class.
            // ---- 2) Students (each linked to one parent + one existing class) ----
            for (int i = 0; i < StudentCount; i++)
            {
                var (first, last, gender) = people[ParentCount + i];
                var email = MakeEmail(first, last, "student");

                if (existingEmails.Contains(email)) { studentsSkipped++; continue; }

                var student = new User
                {
                    FirstName = first,
                    LastName = last,
                    Email = email,
                    PasswordHash = passwordHash,
                    Role = UserRole.Student,
                    Gender = gender,
                    DateOfBirth = RandomDate(rnd, 2008, 2014),
                    PhoneNumber = RandomPhone(rnd),
                    ClassId = classIds[i % classIds.Count], // spread students evenly across classes
                    IsActive = true,
                    IsFirstLogin = true,
                    CreatedAt = now
                };
                _context.Users.Add(student);
                existingEmails.Add(email);
                studentsCreated++;

                // Link to a parent — ~2 students per parent (i and i+20 map to the same parent).
                var parentEmail = parentEmails[i % ParentCount];
                if (parentIdByEmail.TryGetValue(parentEmail, out var parentId))
                {
                    _context.ParentStudents.Add(new ParentStudent
                    {
                        ParentId = parentId,
                        Student = student, // StudentId is filled from the new student on save
                        CreatedAt = now
                    });
                    linksCreated++;
                }
            }

            // One atomic save inserts the new students, then their ParentStudent links.
            await _context.SaveChangesAsync();

            var summary = new
            {
                parentsCreated,
                parentsSkipped,
                studentsCreated,
                studentsSkipped,
                linksCreated
            };
            var message =
                $"Created {parentsCreated} parents, {studentsCreated} students, {linksCreated} links. " +
                $"Skipped {parentsSkipped + studentsSkipped} existing users.";

            return Ok(ApiResponse<object>.Ok(summary, message));
        }
        catch (Exception ex)
        {
            // Friendly summary; the detail helps while seeding but this endpoint is temporary.
            return StatusCode(500, ApiResponse<string>.Fail($"Seeding failed: {ex.Message}"));
        }
    }

    /// <summary>
    /// POST api/dev/seed-subjects — gives every existing class the subject list for its level
    /// (Secondary: 6 subjects, High School: 8), each linked to that class. Re-running SKIPS any
    /// subject that already exists for the same class (same Name + ClassId), so it never
    /// duplicates. EF Core + LINQ only. TODO: REMOVE before deployment.
    /// </summary>
    // [Admin only, TEMPORARY] Gives every existing class the standard subject list for its
    // level (Secondary = 6, High School = 8). Skips subjects a class already has (safe to re-run).
    [HttpPost("seed-subjects")]
    public async Task<IActionResult> SeedSubjects()
    {
        try
        {
            // Read all classes (just the id + level is enough to decide their subjects).
            var classes = await _context.Classes
                .Select(c => new { c.Id, c.Level })
                .ToListAsync();
            if (classes.Count == 0)
                return BadRequest(ApiResponse<string>.Fail("Create classes first."));

            // Existing (Name, ClassId) pairs → skip duplicates so a re-run is safe.
            var existing = (await _context.Subjects
                    .Select(s => new { s.Name, s.ClassId })
                    .ToListAsync())
                .Select(s => (s.Name, s.ClassId))
                .ToHashSet();

            var now = DateTime.UtcNow;
            int created = 0, skipped = 0, classesTouched = 0;

            foreach (var cls in classes)
            {
                var names = SubjectsForLevel(cls.Level);
                if (names.Length == 0) continue; // class with no/unknown level → nothing to add
                classesTouched++;

                foreach (var name in names)
                {
                    if (existing.Contains((name, cls.Id))) { skipped++; continue; }

                    _context.Subjects.Add(new Subject { Name = name, ClassId = cls.Id, CreatedAt = now });
                    existing.Add((name, cls.Id));
                    created++;
                }
            }

            await _context.SaveChangesAsync();

            var message = $"Created {created} subjects across {classesTouched} classes. Skipped {skipped} existing.";
            return Ok(ApiResponse<object>.Ok(new { created, skipped, classes = classesTouched }, message));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<string>.Fail($"Seeding subjects failed: {ex.Message}"));
        }
    }

    /// <summary>
    /// The subject list for a class, chosen by its level. Secondary (إعدادي) gets 6 core
    /// subjects; High School (ثانوي) gets 8 (splitting Science into Physics/Chemistry/Biology).
    /// The Subject model has a single Name column, so English names are stored.
    /// </summary>
    // Helper: picks the subject list for a class based on its level (Secondary vs High School).
    private static string[] SubjectsForLevel(string? level)
    {
        if (string.Equals(level, "Secondary", StringComparison.OrdinalIgnoreCase))
            return ["Islamic Education", "Arabic", "English", "Mathematics", "Science", "Computer Science"];

        if (string.Equals(level, "HighSchool", StringComparison.OrdinalIgnoreCase))
            return ["Islamic Education", "Arabic", "English", "Mathematics", "Physics", "Chemistry", "Biology", "Computer Science"];

        return [];
    }

    // ---- Local helpers (all self-contained in this temporary controller) ----

    /// <summary>
    /// Builds a deterministic pool of <paramref name="count"/> DISTINCT (first, last, gender)
    /// people from realistic Libyan names. Deterministic order means a re-run produces the
    /// same emails, so already-seeded users are skipped instead of duplicated.
    /// </summary>
    // Helper: makes a list of made-up but realistic Libyan names to use for the fake accounts.
    private static List<(string First, string Last, string Gender)> BuildPeople(int count)
    {
        string[] maleFirst = { "Ahmed", "Mohamed", "Ali", "Omar", "Yousef", "Khaled", "Mustafa", "Ibrahim", "Hassan", "Salah", "Tariq", "Fathi" };
        string[] femaleFirst = { "Fatima", "Aisha", "Mariam", "Khadija", "Salma", "Huda", "Nadia", "Amina", "Layla", "Sana", "Rania", "Najwa" };
        string[] families = { "Almansouri", "Alfitouri", "Albarghathi", "Alzawawi", "Alobeidi", "Alwerfalli", "Almagribi", "Altarhouni", "Almisrati", "Alzintani", "Alsabri", "Alhamali", "Aldrissi", "Alghariani", "Alkikhia", "Alshalwi", "Alfakhri", "Alnaas", "Alammari", "Albakoush" };

        var people = new List<(string, string, string)>();
        for (int f = 0; f < families.Length && people.Count < count; f++)
        {
            for (int k = 0; k < maleFirst.Length && people.Count < count; k++)
            {
                // Alternate genders for a natural mix; male/female name arrays are disjoint,
                // so every (first, last) pair produced here is unique.
                if ((f + k) % 2 == 0)
                    people.Add((maleFirst[k], families[f], "Male"));
                else
                    people.Add((femaleFirst[k], families[f], "Female"));
            }
        }
        return people;
    }

    // Helper: builds a fake email like firstname.lastname.role@almanahilschool.com.
    /// <summary>firstname.lastname.role@almanahilschool.com (lowercased, letters/digits only).</summary>
    private static string MakeEmail(string first, string last, string role)
    {
        static string Clean(string s) => new(s.ToLowerInvariant().Where(char.IsLetterOrDigit).ToArray());
        return $"{Clean(first)}.{Clean(last)}.{role}@almanahilschool.com";
    }

    // Helper: picks a random date of birth between the two given years.
    private static DateOnly RandomDate(Random rnd, int minYear, int maxYear)
    {
        int year = rnd.Next(minYear, maxYear + 1);
        int month = rnd.Next(1, 13);
        int day = rnd.Next(1, 28); // 1..27 keeps every month valid
        return new DateOnly(year, month, day);
    }

    // Helper: makes up a believable Libyan mobile number.
    /// <summary>A plausible Libyan mobile number, e.g. 0913456789 (09X + 7 digits).</summary>
    private static string RandomPhone(Random rnd)
    {
        int prefix = rnd.Next(1, 6);              // 091..095
        int number = rnd.Next(1_000_000, 10_000_000); // 7 digits
        return $"09{prefix}{number}";
    }
}
