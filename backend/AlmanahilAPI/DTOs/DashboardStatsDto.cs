// ============================================
// DashboardStatsDto — the summary numbers for the admin home page.
// A DTO is a simple 'data parcel' the server sends to the website.
// This one carries the totals shown on the dashboard cards.
// ============================================
namespace AlmanahilAPI.DTOs;

// Travels: server -> website. The counts shown on the admin dashboard cards.
/// <summary>Headline counts shown on the admin dashboard cards.</summary>
public class DashboardStatsDto
{
    // Total number of user accounts.
    public int TotalUsers { get; set; }
    // How many users are students.
    public int TotalStudents { get; set; }
    // How many users are teachers.
    public int TotalTeachers { get; set; }
    // How many users are parents.
    public int TotalParents { get; set; }
    // How many users are admins.
    public int TotalAdmins { get; set; }
    // Total number of classes.
    public int TotalClasses { get; set; }
    // Total number of subjects.
    public int TotalSubjects { get; set; }
}
