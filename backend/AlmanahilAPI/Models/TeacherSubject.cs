// ============================================
// TeacherSubject - a link that says "this teacher teaches this subject".
// This is a "join" table: it connects two other tables (Users and Subjects).
// Each TeacherSubject object is one row in the 'TeacherSubjects' table, and
// stands for one teacher-subject pairing.
// ============================================
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AlmanahilAPI.Models;

/// <summary>
/// Join entity assigning a Teacher (<see cref="User"/>) to a <see cref="Subject"/>.
/// A teacher can teach many subjects and a subject can have many teachers, but each
/// (TeacherId, SubjectId) pair is unique — enforced by a unique index in the DbContext.
/// </summary>
// One teacher-subject pairing. This class maps to the 'TeacherSubjects' table.
public class TeacherSubject
{
    // The unique ID number for this pairing. Filled in automatically by the
    // database. This is the "primary key".
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    // The ID of the teacher. A link (foreign key) to the Users table
    // (a user whose role is Teacher).
    /// <summary>FK -> User (a user whose role is Teacher).</summary>
    [Required]
    public int TeacherId { get; set; }

    // The ID of the subject. A link (foreign key) to the Subjects table.
    /// <summary>FK -> Subject.</summary>
    [Required]
    public int SubjectId { get; set; }

    // The date and time this pairing was created. Set automatically.
    public DateTime CreatedAt { get; set; }

    // ----- Navigation -----
    // "Navigation" links to the full Teacher and Subject objects.
    public User? Teacher { get; set; }
    public Subject? Subject { get; set; }
}
