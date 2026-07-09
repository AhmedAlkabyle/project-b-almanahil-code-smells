// ============================================
// AlmanahilDbContext - the "bridge" between the C# code and the PostgreSQL
// database. This is the Entity Framework Core (EF Core) part that talks to the
// database for us, so we write C# instead of raw SQL.
// Each "DbSet" below is one table. OnModelCreating (further down) sets the
// extra rules for those tables (like "no duplicates" and delete behavior).
// ============================================
using AlmanahilAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace AlmanahilAPI.Data;

// The database bridge for the whole app. It receives its settings (like the
// connection string) through "options" when the app starts.
public class AlmanahilDbContext(DbContextOptions<AlmanahilDbContext> options) : DbContext(options)
{
    // Each DbSet is one table. This one is the 'Users' table.
    public DbSet<User> Users { get; set; }

    // ----- Module 2 (Admin Dashboard) -----
    // The 'Classes' table.
    public DbSet<Class> Classes { get; set; }
    // The 'Subjects' table.
    public DbSet<Subject> Subjects { get; set; }
    // The 'TeacherSubjects' table (which teacher teaches which subject).
    public DbSet<TeacherSubject> TeacherSubjects { get; set; }
    // The 'ParentStudents' table (which parent is linked to which student).
    public DbSet<ParentStudent> ParentStudents { get; set; }
    // The 'Events' table (announcements and events).
    public DbSet<Event> Events { get; set; }
    // The 'TimetableSlots' table (the weekly timetable cells).
    public DbSet<TimetableSlot> TimetableSlots { get; set; }
    // The 'AttendanceRecords' table (attendance marks).
    public DbSet<AttendanceRecord> AttendanceRecords { get; set; }
    // The 'GradeRecords' table (grades/marks).
    public DbSet<GradeRecord> GradeRecords { get; set; }
    // The 'LearningMaterials' table (study links/files).
    public DbSet<LearningMaterial> LearningMaterials { get; set; }

    // This method runs once when EF Core builds the model. Here we set the
    // extra database rules for each table that the model classes cannot show
    // on their own (unique columns, delete behavior, default values, etc.).
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(entity =>
        {
            // No two users can share the same email (a "unique index" =
            // "no duplicates allowed").
            entity.HasIndex(u => u.Email).IsUnique();

            // Store the Role (Admin/Teacher/...) as a number in the database.
            entity.Property(u => u.Role)
                  .HasConversion<int>();

            // If nobody says otherwise, a new user is active by default.
            entity.Property(u => u.IsActive)
                  .HasDefaultValue(true);

            // If nobody says otherwise, a new user starts on their first login.
            entity.Property(u => u.IsFirstLogin)
                  .HasDefaultValue(true);

            // Fill CreatedAt with the current time automatically (NOW() is the
            // database's clock).
            entity.Property(u => u.CreatedAt)
                  .HasDefaultValueSql("NOW()");

            // Password-reset fields (ResetCode, ResetCodeExpiresAt) need no
            // explicit mapping: they are nullable by convention (string?/DateTime?),
            // ResetCode's length comes from its [MaxLength(256)] attribute, and
            // they must NOT be unique (many users can hold a code at once).
        });

        // ===== Module 2 (Admin Dashboard) data foundation =====

        modelBuilder.Entity<Class>(entity =>
        {
            // Fill CreatedAt with the current time automatically.
            entity.Property(c => c.CreatedAt).HasDefaultValueSql("NOW()");

            // A class has many students. If a class is deleted, the students are
            // NOT deleted - their ClassId is just emptied ("set-null"), so they
            // simply stop being linked to that class.
            entity.HasMany(c => c.Students)
                  .WithOne(u => u.Class)
                  .HasForeignKey(u => u.ClassId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<Subject>(entity =>
        {
            // Fill CreatedAt with the current time automatically.
            entity.Property(s => s.CreatedAt).HasDefaultValueSql("NOW()");

            // A subject belongs to one class. "Cascade" = if the class is
            // deleted, delete its subjects too (delete the children as well).
            entity.HasOne(s => s.Class)
                  .WithMany(c => c.Subjects)
                  .HasForeignKey(s => s.ClassId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<TeacherSubject>(entity =>
        {
            // Fill CreatedAt with the current time automatically.
            entity.Property(ts => ts.CreatedAt).HasDefaultValueSql("NOW()");

            // The same teacher can be linked to the same subject only once
            // ("no duplicates" of this teacher+subject pair).
            entity.HasIndex(ts => new { ts.TeacherId, ts.SubjectId }).IsUnique();

            // "Restrict" = block deleting a teacher who still has rows here.
            // (This also stops the database from making risky delete chains
            // into the Users table.)
            entity.HasOne(ts => ts.Teacher)
                  .WithMany()
                  .HasForeignKey(ts => ts.TeacherId)
                  .OnDelete(DeleteBehavior.Restrict);

            // "Cascade" = deleting a subject also deletes its teacher links.
            entity.HasOne(ts => ts.Subject)
                  .WithMany()
                  .HasForeignKey(ts => ts.SubjectId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ParentStudent>(entity =>
        {
            // Fill CreatedAt with the current time automatically.
            entity.Property(ps => ps.CreatedAt).HasDefaultValueSql("NOW()");

            // The same parent can be linked to the same student only once
            // ("no duplicates" of this parent+student pair).
            entity.HasIndex(ps => new { ps.ParentId, ps.StudentId }).IsUnique();

            // Both links point to the Users table. "Restrict" = block deleting a
            // parent or student who is still linked here (and it avoids risky
            // delete chains into the Users table).
            entity.HasOne(ps => ps.Parent)
                  .WithMany()
                  .HasForeignKey(ps => ps.ParentId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(ps => ps.Student)
                  .WithMany()
                  .HasForeignKey(ps => ps.StudentId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Event>(entity =>
        {
            // Fill CreatedAt with the current time automatically.
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("NOW()");

            // The event's scheduled date AND time. 'timestamp without time zone' stores it
            // as a plain wall-clock (no timezone conversion), so 14:30 stays 14:30, and
            // Npgsql accepts the datetime-local value the form sends (DateTime with
            // Kind=Unspecified) without the "UTC required" timestamptz error.
            entity.Property(e => e.Date)
                  .HasColumnType("timestamp without time zone");

            // If nobody sets the audience, it defaults to "AllUsers".
            entity.Property(e => e.AudienceType)
                  .HasMaxLength(40)
                  .HasDefaultValue("AllUsers");

            // "Restrict" = block deleting the admin user who still has events.
            entity.HasOne(e => e.CreatedBy)
                  .WithMany()
                  .HasForeignKey(e => e.CreatedByUserId)
                  .OnDelete(DeleteBehavior.Restrict);

            // Optional target class. "Set-null" = if that class is deleted, just
            // empty the link (TargetClassId -> null) instead of deleting the
            // event, so the announcement is never lost.
            entity.HasOne(e => e.TargetClass)
                  .WithMany()
                  .HasForeignKey(e => e.TargetClassId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<TimetableSlot>(entity =>
        {
            // Fill CreatedAt and UpdatedAt with the current time automatically.
            entity.Property(t => t.CreatedAt).HasDefaultValueSql("NOW()");
            entity.Property(t => t.UpdatedAt).HasDefaultValueSql("NOW()");

            // A class can have only ONE slot for the same day + period
            // ("no duplicates" of that cell).
            entity.HasIndex(t => new { t.ClassId, t.Day, t.Period }).IsUnique();

            // "Cascade" = deleting a class also deletes its whole timetable.
            entity.HasOne(t => t.Class)
                  .WithMany()
                  .HasForeignKey(t => t.ClassId)
                  .OnDelete(DeleteBehavior.Cascade);

            // "Set-null" = deleting a subject just empties the cells that used it
            // (SubjectId -> null), so the timetable grid stays in place.
            entity.HasOne(t => t.Subject)
                  .WithMany()
                  .HasForeignKey(t => t.SubjectId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<AttendanceRecord>(entity =>
        {
            // Fill CreatedAt and UpdatedAt with the current time automatically.
            entity.Property(a => a.CreatedAt).HasDefaultValueSql("NOW()");
            entity.Property(a => a.UpdatedAt).HasDefaultValueSql("NOW()");

            // Only ONE attendance row per student, per subject, per day
            // ("no duplicates"). Saving the same day again updates that row.
            entity.HasIndex(a => new { a.SubjectId, a.StudentId, a.Date }).IsUnique();

            // "Cascade" = deleting a subject also deletes its attendance rows.
            entity.HasOne(a => a.Subject)
                  .WithMany()
                  .HasForeignKey(a => a.SubjectId)
                  .OnDelete(DeleteBehavior.Cascade);

            // Both links point to the Users table. "Restrict" = block deleting a
            // student or teacher who still has attendance rows here.
            entity.HasOne(a => a.Student)
                  .WithMany()
                  .HasForeignKey(a => a.StudentId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(a => a.RecordedByTeacher)
                  .WithMany()
                  .HasForeignKey(a => a.RecordedByTeacherId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<GradeRecord>(entity =>
        {
            // Fill CreatedAt and UpdatedAt with the current time automatically.
            entity.Property(g => g.CreatedAt).HasDefaultValueSql("NOW()");
            entity.Property(g => g.UpdatedAt).HasDefaultValueSql("NOW()");

            // Store marks as numbers with up to 2 decimal places (plenty for a
            // 0..100 score). MaxMark defaults to 100.
            entity.Property(g => g.Mark).HasPrecision(6, 2);
            entity.Property(g => g.MaxMark).HasPrecision(6, 2).HasDefaultValue(100m);

            // Only ONE mark per student, per subject, per assessment type
            // ("no duplicates"). Saving it again updates that row.
            entity.HasIndex(g => new { g.SubjectId, g.StudentId, g.AssessmentType }).IsUnique();

            // "Cascade" = deleting a subject also deletes its grades.
            entity.HasOne(g => g.Subject)
                  .WithMany()
                  .HasForeignKey(g => g.SubjectId)
                  .OnDelete(DeleteBehavior.Cascade);

            // Both links point to the Users table. "Restrict" = block deleting a
            // student or teacher who still has grade rows here.
            entity.HasOne(g => g.Student)
                  .WithMany()
                  .HasForeignKey(g => g.StudentId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(g => g.RecordedByTeacher)
                  .WithMany()
                  .HasForeignKey(g => g.RecordedByTeacherId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<LearningMaterial>(entity =>
        {
            // Fill CreatedAt and UpdatedAt with the current time automatically.
            entity.Property(m => m.CreatedAt).HasDefaultValueSql("NOW()");
            entity.Property(m => m.UpdatedAt).HasDefaultValueSql("NOW()");

            // "Cascade" = deleting a subject also deletes its materials.
            entity.HasOne(m => m.Subject)
                  .WithMany()
                  .HasForeignKey(m => m.SubjectId)
                  .OnDelete(DeleteBehavior.Cascade);

            // "Restrict" = block deleting the teacher who still has materials here.
            entity.HasOne(m => m.UploadedByTeacher)
                  .WithMany()
                  .HasForeignKey(m => m.UploadedByTeacherId)
                  .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
