// ============================================
// Subject - one school subject (e.g. "Mathematics") belonging to one class.
// A "model" is a plain C# class that maps to one database table. Each Subject
// object is one row in the 'Subjects' table.
// ============================================
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AlmanahilAPI.Models;

/// <summary>
/// A subject (e.g. "Mathematics") that belongs to exactly one <see cref="Class"/>.
/// </summary>
// One subject. This class maps to the 'Subjects' table.
public class Subject
{
    // The unique ID number for this subject. Filled in automatically by the
    // database. This is the "primary key".
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    // The subject's name, e.g. "Mathematics". Required.
    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    // The ID of the class this subject belongs to. A link (foreign key) to the
    // Classes table. Required - every subject must be in a class.
    /// <summary>The class this subject belongs to (required). FK -> Class.</summary>
    [Required]
    public int ClassId { get; set; }

    // The date and time this subject was created. Set automatically.
    public DateTime CreatedAt { get; set; }

    // ----- Navigation -----
    // A "navigation" link back to the full Class object this subject belongs to.
    public Class? Class { get; set; }
}
