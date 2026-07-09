// ============================================
// LearningMaterialDtos — the data parcels for study materials (videos/files).
// A DTO is a simple 'data parcel' the website and server pass back and forth.
// This file holds the parcels for showing a material and for adding one.
// ============================================
using System.ComponentModel.DataAnnotations;

namespace AlmanahilAPI.DTOs;

// ==== Learning Materials module DTOs (Module 5) ======================================
// A material belongs to a subject. Stage 1 stores YouTube links (MaterialType = "YouTube",
// Url = the link); Stage 2 adds "Pdf" with no DTO change. The teacher's subject list reuses
// TeacherSubjectDto (defined in AttendanceDtos.cs — same namespace).

// Travels: server -> website. One study material for the list/detail screens.
/// <summary>A learning material as returned to the client (list/detail).</summary>
public class LearningMaterialDto
{
    // The material's id.
    public int Id { get; set; }
    // The subject this material belongs to.
    public int SubjectId { get; set; }
    public string SubjectName { get; set; } = string.Empty;
    // The material's title.
    public string Title { get; set; } = string.Empty;
    // A short description (optional).
    public string? Description { get; set; }

    // The kind of material: a YouTube video now, a PDF file later.
    /// <summary>'YouTube' now; 'Pdf' later.</summary>
    public string MaterialType { get; set; } = string.Empty;

    // The web link to the video or file.
    /// <summary>The resource URL (the YouTube link now; a file URL in Stage 2).</summary>
    public string Url { get; set; } = string.Empty;

    // When this material was added.
    public DateTime CreatedAt { get; set; }
}

// Travels: website -> server. The "add material" form the teacher fills in.
/// <summary>
/// The "add material" payload. The subject comes from the body; the uploading teacher is
/// always taken from the JWT (never the client).
/// </summary>
public class CreateMaterialDto
{
    // The subject to add this material to.
    [Required]
    public int SubjectId { get; set; }

    // The material's title (required).
    [Required, MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    // A short description (optional).
    [MaxLength(1000)]
    public string? Description { get; set; }

    // The kind of material: YouTube video now, PDF file later.
    /// <summary>'YouTube' (Stage 1) or 'Pdf' (Stage 2) — validated in the controller.</summary>
    [Required, MaxLength(20)]
    public string MaterialType { get; set; } = "YouTube";

    // The web link to the video or file (required).
    /// <summary>The YouTube link (Stage 1) — required. A file URL in Stage 2.</summary>
    [Required, MaxLength(1000)]
    public string Url { get; set; } = string.Empty;
}
