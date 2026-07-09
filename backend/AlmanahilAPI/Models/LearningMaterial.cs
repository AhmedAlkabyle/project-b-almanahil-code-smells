// ============================================
// LearningMaterial - one study resource (like a YouTube link or, later, a PDF)
// that a teacher attaches to a subject.
// A "model" is a plain C# class that maps to one database table. Each
// LearningMaterial object is one row in the 'LearningMaterials' table.
// ============================================
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AlmanahilAPI.Models;

/// <summary>
/// A learning material a teacher attaches to a subject. Stage 1 stores YouTube video links
/// (<see cref="MaterialType"/> = "YouTube", <see cref="Url"/> = the video link). Stage 2 will
/// add "Pdf" (Url = the uploaded file's URL) with NO schema change — MaterialType + Url already
/// generalize both kinds. Mirrors the Attendance/Grades modules (AttendanceRecord/GradeRecord).
/// </summary>
// One study material. This class maps to the 'LearningMaterials' table.
public class LearningMaterial
{
    // The unique ID number for this material. Filled in automatically by the
    // database. This is the "primary key".
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    // The ID of the subject this material belongs to. A link (foreign key) to
    // the Subjects table.
    /// <summary>FK -> Subject (the subject this material belongs to).</summary>
    [Required]
    public int SubjectId { get; set; }

    // The title/name of the material. Required.
    [Required, MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    // An optional description or note about the material.
    /// <summary>Optional description / note about the material.</summary>
    [MaxLength(1000)]
    public string? Description { get; set; }

    // The kind of material: "YouTube" for now (a "Pdf" option is planned).
    /// <summary>'YouTube' now; 'Pdf' later — validated in the controller.</summary>
    [Required, MaxLength(20)]
    public string MaterialType { get; set; } = "YouTube";

    // The web address (link) to the resource. Required.
    /// <summary>The resource URL: the YouTube link now, an uploaded file's URL in Stage 2.</summary>
    [Required, MaxLength(1000)]
    public string Url { get; set; } = string.Empty;

    // The ID of the teacher (or admin) who added this material. A link
    // (foreign key) to the Users table.
    /// <summary>FK -> User (the teacher — or admin — who added this material).</summary>
    [Required]
    public int UploadedByTeacherId { get; set; }

    // The date and time this material was added. Set automatically.
    public DateTime CreatedAt { get; set; }

    // The date and time this material was last changed. Set automatically.
    public DateTime UpdatedAt { get; set; }

    // ----- Navigation -----
    // "Navigation" links to the full related objects.
    public Subject? Subject { get; set; }

    public User? UploadedByTeacher { get; set; }
}
