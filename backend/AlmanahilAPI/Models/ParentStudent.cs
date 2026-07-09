// ============================================
// ParentStudent - a link that says "this parent is the parent of this student".
// This is a "join" table: it connects two rows in the Users table (a parent
// and their child). Each ParentStudent object is one row in the
// 'ParentStudents' table, and stands for one parent-child pairing.
// ============================================
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AlmanahilAPI.Models;

/// <summary>
/// Join entity linking a Parent (<see cref="User"/>) to their child, a Student
/// (<see cref="User"/>). Each (ParentId, StudentId) pair is unique — enforced by a
/// unique index in the DbContext.
/// </summary>
// One parent-child pairing. This class maps to the 'ParentStudents' table.
public class ParentStudent
{
    // The unique ID number for this pairing. Filled in automatically by the
    // database. This is the "primary key".
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    // The ID of the parent. A link (foreign key) to the Users table
    // (a user whose role is Parent).
    /// <summary>FK -> User (a user whose role is Parent).</summary>
    [Required]
    public int ParentId { get; set; }

    // The ID of the student (the child). A link (foreign key) to the Users
    // table (a user whose role is Student).
    /// <summary>FK -> User (a user whose role is Student).</summary>
    [Required]
    public int StudentId { get; set; }

    // The date and time this pairing was created. Set automatically.
    public DateTime CreatedAt { get; set; }

    // ----- Navigation -----
    // "Navigation" links to the full Parent and Student user objects.
    public User? Parent { get; set; }
    public User? Student { get; set; }
}
