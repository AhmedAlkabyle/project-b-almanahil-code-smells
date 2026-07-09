// ============================================
// CreateParentLinkDto — the parcel to connect a parent to a student.
// A DTO is a simple 'data parcel' the website sends to the server.
// This one carries which parent should be linked to which child.
// ============================================
using System.ComponentModel.DataAnnotations;

namespace AlmanahilAPI.DTOs;

// Travels: website -> server. The admin's "link this parent to this child" choice.
/// <summary>Payload the admin sends to link a parent to a student.</summary>
public class CreateParentLinkDto
{
    // The parent account being linked.
    /// <summary>The parent (a User with role Parent). Existence/role is checked in the controller.</summary>
    [Range(1, int.MaxValue, ErrorMessage = "Please select a parent.")]
    public int ParentId { get; set; }

    // The student (child) being linked to that parent.
    /// <summary>The student (a User with role Student). Existence/role is checked in the controller.</summary>
    [Range(1, int.MaxValue, ErrorMessage = "Please select a student.")]
    public int StudentId { get; set; }
}
