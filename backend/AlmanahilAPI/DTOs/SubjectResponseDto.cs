// ============================================
// SubjectResponseDto — the subject details the server sends back to the website.
// A DTO is a simple 'data parcel' the server sends to the website.
// This one carries a subject's id, name, and its class.
// ============================================
namespace AlmanahilAPI.DTOs;

// Travels: server -> website. One subject's info for lists and screens.
/// <summary>Shape returned to the client for a subject.</summary>
public class SubjectResponseDto
{
    // The subject's database id.
    public int Id { get; set; }

    // The subject's name.
    public string Name { get; set; } = string.Empty;

    // The id of the class this subject belongs to.
    public int ClassId { get; set; }

    // The name of the class this subject belongs to (looked up on the server).
    /// <summary>Name of the owning class, resolved via a LINQ join to Classes.</summary>
    public string ClassName { get; set; } = string.Empty;

    // When this subject was created.
    public DateTime CreatedAt { get; set; }
}
