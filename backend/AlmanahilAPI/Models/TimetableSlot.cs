// ============================================
// TimetableSlot - one cell of a class's weekly timetable (which subject is
// taught on a certain day and period).
// A "model" is a plain C# class that maps to one database table. Each
// TimetableSlot object is one row in the 'TimetableSlots' table.
// ============================================
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AlmanahilAPI.Models;

/// <summary>
/// One cell of a class's weekly timetable grid: the subject taught to a given
/// <see cref="Class"/> on a given day + period. The school week runs Sunday–Thursday,
/// and the number of periods per day depends on the class Level (Secondary = 5,
/// High School = 6). A cell may be empty — <see cref="SubjectId"/> is nullable.
///
/// The (<see cref="ClassId"/>, <see cref="Day"/>, <see cref="Period"/>) trio is UNIQUE
/// (one subject per cell), enforced by a unique index in the DbContext.
///
/// NOTE: period *clock times* are NOT stored here. They are fixed and computed on the
/// fly by <c>PeriodSchedule.For(level)</c>, so this row only records "which subject sits
/// in which cell".
/// </summary>
// One timetable cell. This class maps to the 'TimetableSlots' table.
public class TimetableSlot
{
    // The unique ID number for this cell. Filled in automatically by the
    // database. This is the "primary key".
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    /// <summary>
    /// The class this slot belongs to (required). FK -> Class. Deleting the class
    /// cascade-deletes its whole timetable.
    /// </summary>
    // The ID of the class this cell belongs to. A link (foreign key) to the
    // Classes table. Required.
    [Required]
    public int ClassId { get; set; }

    // The day of the school week (1 = Sunday ... 5 = Thursday).
    /// <summary>Day of the school week: 1 = Sunday … 5 = Thursday.</summary>
    [Required]
    public int Day { get; set; }

    // The lesson number in that day (1 up to 5 or 6, depending on the level).
    /// <summary>Period number within the day: 1..5 (Secondary) or 1..6 (High School).</summary>
    [Required]
    public int Period { get; set; }

    /// <summary>
    /// The subject placed in this cell, or null for an empty slot. FK -> Subject.
    /// On subject delete this is set to null (the cell simply becomes empty).
    /// </summary>
    // The ID of the subject placed in this cell. A link (foreign key) to the
    // Subjects table. Empty means the cell is a free/empty period.
    public int? SubjectId { get; set; }

    // The date and time this cell was created. Set automatically.
    public DateTime CreatedAt { get; set; }

    // The date and time this cell was last changed. Set automatically.
    public DateTime UpdatedAt { get; set; }

    // ----- Navigation -----
    // "Navigation" links to the full Class and Subject objects for this cell.
    public Class? Class { get; set; }
    public Subject? Subject { get; set; }
}
