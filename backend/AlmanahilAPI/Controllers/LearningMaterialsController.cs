// ============================================
// LearningMaterialsController — teaching resources for each subject.
// Job: a teacher attaches materials (a YouTube link, or an uploaded PDF) to a subject
// they teach; the students of that class, their parents, and admins can view them.
// It always trusts the logged-in user from their sign-in token, not the request body.
// Used by: the Vue materials pages (teacher add/upload, student/parent view).
// ============================================
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using AlmanahilAPI.Data;
using AlmanahilAPI.DTOs;
using AlmanahilAPI.Helpers;
using AlmanahilAPI.Models;
using AlmanahilAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AlmanahilAPI.Controllers;

/// <summary>
/// Learning Materials (Module 5). A teacher attaches materials to a subject she teaches;
/// the students of that subject's class (and their parents, and admins) can view them.
/// Stage 1 = YouTube links only. EF Core + LINQ only, every response uses the standard
/// ApiResponse envelope. Mirrors the Attendance/Grades controllers' conventions.
///
/// RBAC:
///  • Add (POST) / delete → the TEACHER assigned to the subject (via TeacherSubjects) — or
///    the uploading teacher / an Admin for delete. A teacher not assigned → 403.
///  • View a subject's materials → the assigned teacher, a student in that class, a parent
///    of a student in that class, or an Admin.
///  • A student's own feed (/my) → scoped to the caller's class via the JWT.
/// The acting user is always taken from the validated JWT (never the request body).
/// </summary>
// This class manages learning materials (YouTube links and PDFs) attached to subjects.
// [Authorize] means you must be signed in to reach ANY endpoint below.
[ApiController]
[Route("api/materials")]
[Authorize]
public class LearningMaterialsController(
    AlmanahilDbContext context,
    ILogger<LearningMaterialsController> logger,
    SupabaseStorageService storage) : ControllerBase
{
    private readonly AlmanahilDbContext _context = context;
    private readonly ILogger<LearningMaterialsController> _logger = logger;
    private readonly SupabaseStorageService _storage = storage;

    /// <summary>The material types the API accepts: "YouTube" (Stage 1) + "Pdf" (Stage 2).</summary>
    private static readonly string[] AllowedMaterialTypes = ["YouTube", "Pdf"];

    /// <summary>The maximum size a single uploaded PDF may be (50 MB).</summary>
    private const long MaxPdfBytes = 50L * 1024 * 1024;

    /// <summary>
    /// GET /api/materials/my-subjects — the subjects assigned to the logged-in teacher (from
    /// TeacherSubjects), for the subject picker. Admins have none → empty list.
    /// </summary>
    // [Any signed-in user] Gives the logged-in teacher the subjects they teach, for the
    // subject picker. (Admins/others get an empty list.)
    [HttpGet("my-subjects")]
    public async Task<IActionResult> MySubjects()
    {
        try
        {
            if (!TryGetUserId(out var userId))
                return Unauthorized(ApiResponse<List<TeacherSubjectDto>>.Fail("Invalid or missing authentication token."));

            var subjects = await _context.TeacherSubjects
                .Where(ts => ts.TeacherId == userId)
                .OrderBy(ts => ts.Subject!.Class!.Name).ThenBy(ts => ts.Subject!.Name)
                .Select(ts => new TeacherSubjectDto
                {
                    SubjectId = ts.SubjectId,
                    SubjectName = ts.Subject!.Name,
                    ClassId = ts.Subject.ClassId,
                    ClassName = ts.Subject.Class!.Name
                })
                .ToListAsync();

            return Ok(ApiResponse<List<TeacherSubjectDto>>.Ok(subjects, "Your subjects retrieved successfully."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve the teacher's subjects.");
            return StatusCode(500, ApiResponse<List<TeacherSubjectDto>>.Fail("An error occurred while loading your subjects."));
        }
    }

    /// <summary>
    /// POST /api/materials — the assigned teacher (or an admin) adds a material to a subject.
    /// Stage 1 accepts MaterialType "YouTube" + a Url. UploadedByTeacherId comes from the JWT.
    /// </summary>
    // [Teacher assigned to this subject, or Admin] Adds a link-type material (e.g. a YouTube
    // video) to a subject. The uploader is taken from their sign-in token.
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateMaterialDto dto)
    {
        try
        {
            var materialType = NormalizeMaterialType(dto.MaterialType);
            if (materialType is null)
                return BadRequest(ApiResponse<LearningMaterialDto>.Fail(
                    $"Material type must be one of: {string.Join(", ", AllowedMaterialTypes)}."));

            if (string.IsNullOrWhiteSpace(dto.Title))
                return BadRequest(ApiResponse<LearningMaterialDto>.Fail("A title is required."));

            if (string.IsNullOrWhiteSpace(dto.Url))
                return BadRequest(ApiResponse<LearningMaterialDto>.Fail("A link (URL) is required."));

            var subjectExists = await _context.Subjects.AnyAsync(s => s.Id == dto.SubjectId);
            if (!subjectExists)
                return NotFound(ApiResponse<LearningMaterialDto>.Fail("Subject not found."));

            // RBAC: the assigned teacher or an admin may add materials to this subject.
            var access = await AuthorizeSubjectManageAsync(dto.SubjectId);
            if (access is not null) return access;

            if (!TryGetUserId(out var uploaderId))
                return Unauthorized(ApiResponse<LearningMaterialDto>.Fail("Invalid or missing authentication token."));

            var now = DateTime.UtcNow;
            var entity = new LearningMaterial
            {
                SubjectId = dto.SubjectId,
                Title = dto.Title.Trim(),
                Description = string.IsNullOrWhiteSpace(dto.Description) ? null : dto.Description.Trim(),
                MaterialType = materialType,
                Url = dto.Url.Trim(),
                UploadedByTeacherId = uploaderId,
                CreatedAt = now,
                UpdatedAt = now
            };

            _context.LearningMaterials.Add(entity);
            await _context.SaveChangesAsync();

            var result = await _context.LearningMaterials
                .Where(m => m.Id == entity.Id)
                .Select(m => new LearningMaterialDto
                {
                    Id = m.Id,
                    SubjectId = m.SubjectId,
                    SubjectName = m.Subject!.Name,
                    Title = m.Title,
                    Description = m.Description,
                    MaterialType = m.MaterialType,
                    Url = m.Url,
                    CreatedAt = m.CreatedAt
                })
                .FirstAsync();

            return Ok(ApiResponse<LearningMaterialDto>.Ok(result, "Material added successfully."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add a learning material.");
            return StatusCode(500, ApiResponse<LearningMaterialDto>.Fail("An error occurred while adding the material."));
        }
    }

    /// <summary>
    /// POST /api/materials/upload-pdf (multipart/form-data) — the assigned teacher (or admin)
    /// uploads a PDF to Supabase Storage; a LearningMaterial row is then created with
    /// MaterialType "Pdf" and Url = the file's public URL. If the upload fails, NO row is created.
    /// Fields: subjectId, title, description?, file.
    /// </summary>
    // [Teacher assigned to this subject, or Admin] Uploads a PDF file for a subject. The file
    // is sent to online storage (Supabase); only if that succeeds do we save a material row
    // pointing at the file's public link. If the upload fails, nothing is saved.
    [HttpPost("upload-pdf")]
    [RequestSizeLimit(60_000_000)] // ~57 MB envelope; the 50 MB rule below gives a friendly message
    public async Task<IActionResult> UploadPdf(
        [FromForm] int subjectId,
        [FromForm] string title,
        [FromForm] string? description,
        IFormFile? file)
    {
        try
        {
            // TEMPORARY DEBUG: prove the request reached this endpoint and show what bound.
            _logger.LogInformation("[upload-pdf] hit — file={File} size={Size} subjectId={SubjectId} title={Title}",
                file?.FileName ?? "(null)", file?.Length ?? 0, subjectId, string.IsNullOrEmpty(title) ? "(empty)" : title);
            Console.WriteLine($"[upload-pdf] hit — file={file?.FileName ?? "(null)"} size={file?.Length ?? 0} subjectId={subjectId} title={(string.IsNullOrEmpty(title) ? "(empty)" : title)}");

            // Step 1: Basic checks — a title is required and a file must actually be attached.
            if (string.IsNullOrWhiteSpace(title))
                return BadRequest(ApiResponse<LearningMaterialDto>.Fail("A title is required."));

            if (file is null || file.Length == 0)
                return BadRequest(ApiResponse<LearningMaterialDto>.Fail("Please choose a PDF file to upload."));

            // Step 2: Make sure the file really is a PDF and isn't too big (max 50 MB).
            // Validate it's a PDF by content-type and/or extension.
            var isPdf = string.Equals(file.ContentType, "application/pdf", StringComparison.OrdinalIgnoreCase)
                        || Path.GetExtension(file.FileName).Equals(".pdf", StringComparison.OrdinalIgnoreCase);
            if (!isPdf)
                return BadRequest(ApiResponse<LearningMaterialDto>.Fail("Only PDF files are allowed."));

            if (file.Length > MaxPdfBytes)
                return BadRequest(ApiResponse<LearningMaterialDto>.Fail("The file is too large (maximum 50 MB)."));

            // Step 3: The subject must exist.
            var subjectExists = await _context.Subjects.AnyAsync(s => s.Id == subjectId);
            if (!subjectExists)
                return NotFound(ApiResponse<LearningMaterialDto>.Fail("Subject not found."));

            // Step 4: Permission check — only the assigned teacher (or an admin) may add here.
            // RBAC: the assigned teacher or an admin may add materials to this subject.
            var access = await AuthorizeSubjectManageAsync(subjectId);
            if (access is not null) return access;

            // Step 5: Find out who is uploading (from their sign-in token).
            if (!TryGetUserId(out var uploaderId))
                return Unauthorized(ApiResponse<LearningMaterialDto>.Fail("Invalid or missing authentication token."));

            if (!_storage.IsConfigured)
                return StatusCode(500, ApiResponse<LearningMaterialDto>.Fail(
                    "File uploads aren't configured on the server yet. Please contact the administrator."));

            // Step 6: Read the file into memory, then send it to online storage under a unique name.
            byte[] bytes;
            using (var ms = new MemoryStream())
            {
                await file.CopyToAsync(ms);
                bytes = ms.ToArray();
            }

            var path = $"materials/{Guid.NewGuid():N}.pdf";
            string publicUrl;
            try
            {
                publicUrl = await _storage.UploadAsync(bytes, path, "application/pdf");
            }
            catch (Exception uploadEx)
            {
                // Upload failed → do NOT create a row.
                // TEMPORARY DEBUG: surface the REAL storage error (status + Supabase body) to the
                // client and terminal so we can see exactly what went wrong. Revert to the friendly
                // message ("We couldn't upload the file right now. Please try again.") afterwards.
                _logger.LogError(uploadEx, "PDF upload to storage failed for subject {SubjectId}.", subjectId);
                Console.WriteLine($"[upload-pdf] ERROR: {uploadEx.Message}");
                return StatusCode(502, ApiResponse<LearningMaterialDto>.Fail(
                    $"[DEBUG] Upload failed: {uploadEx.Message}"));
            }

            // Step 7: The upload worked — now save the material row pointing at the file's link.
            var now = DateTime.UtcNow;
            var entity = new LearningMaterial
            {
                SubjectId = subjectId,
                Title = title.Trim(),
                Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
                MaterialType = "Pdf",
                Url = publicUrl,
                UploadedByTeacherId = uploaderId,
                CreatedAt = now,
                UpdatedAt = now
            };

            _context.LearningMaterials.Add(entity);
            await _context.SaveChangesAsync();

            var result = await _context.LearningMaterials
                .Where(m => m.Id == entity.Id)
                .Select(m => new LearningMaterialDto
                {
                    Id = m.Id,
                    SubjectId = m.SubjectId,
                    SubjectName = m.Subject!.Name,
                    Title = m.Title,
                    Description = m.Description,
                    MaterialType = m.MaterialType,
                    Url = m.Url,
                    CreatedAt = m.CreatedAt
                })
                .FirstAsync();

            return Ok(ApiResponse<LearningMaterialDto>.Ok(result, "PDF uploaded successfully."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload a PDF material.");
            return StatusCode(500, ApiResponse<LearningMaterialDto>.Fail("An error occurred while uploading the material."));
        }
    }

    /// <summary>
    /// GET /api/materials/subject/{subjectId} — the materials for one subject. Viewable by the
    /// assigned teacher, a student in the subject's class, a parent of such a student, or an admin.
    /// </summary>
    // [Assigned teacher, a student in the class, a parent of such a student, or Admin]
    // Lists all the materials for one subject.
    [HttpGet("subject/{subjectId:int}")]
    public async Task<IActionResult> GetForSubject(int subjectId)
    {
        try
        {
            var subject = await _context.Subjects
                .Where(s => s.Id == subjectId)
                .Select(s => new { s.Id, s.ClassId })
                .FirstOrDefaultAsync();

            if (subject is null)
                return NotFound(ApiResponse<List<LearningMaterialDto>>.Fail("Subject not found."));

            // RBAC: teacher assigned / student in class / parent of such a student / admin.
            var access = await AuthorizeSubjectViewAsync(subjectId, subject.ClassId);
            if (access is not null) return access;

            var materials = await _context.LearningMaterials
                .Where(m => m.SubjectId == subjectId)
                .OrderByDescending(m => m.CreatedAt)
                .Select(m => new LearningMaterialDto
                {
                    Id = m.Id,
                    SubjectId = m.SubjectId,
                    SubjectName = m.Subject!.Name,
                    Title = m.Title,
                    Description = m.Description,
                    MaterialType = m.MaterialType,
                    Url = m.Url,
                    CreatedAt = m.CreatedAt
                })
                .ToListAsync();

            return Ok(ApiResponse<List<LearningMaterialDto>>.Ok(materials, "Materials retrieved successfully."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load materials for subject {SubjectId}.", subjectId);
            return StatusCode(500, ApiResponse<List<LearningMaterialDto>>.Fail("An error occurred while loading materials."));
        }
    }

    /// <summary>
    /// GET /api/materials/my — the materials for every subject in the logged-in student's class
    /// (so a student sees everything for their class). Scoped to the caller's ClassId via the JWT.
    /// </summary>
    // [Any signed-in user] Shows the logged-in student every material for their own class
    // (across all their subjects). Someone with no class gets an empty list.
    [HttpGet("my")]
    public async Task<IActionResult> My()
    {
        try
        {
            if (!TryGetUserId(out var userId))
                return Unauthorized(ApiResponse<List<LearningMaterialDto>>.Fail("Invalid or missing authentication token."));

            var classId = await _context.Users
                .Where(u => u.Id == userId)
                .Select(u => u.ClassId)
                .FirstOrDefaultAsync();

            // No class (non-students, or a student not yet enrolled) → empty feed.
            if (classId is null)
                return Ok(ApiResponse<List<LearningMaterialDto>>.Ok([], "Your materials retrieved successfully."));

            var materials = await _context.LearningMaterials
                .Where(m => m.Subject!.ClassId == classId.Value)
                .OrderBy(m => m.Subject!.Name).ThenByDescending(m => m.CreatedAt)
                .Select(m => new LearningMaterialDto
                {
                    Id = m.Id,
                    SubjectId = m.SubjectId,
                    SubjectName = m.Subject!.Name,
                    Title = m.Title,
                    Description = m.Description,
                    MaterialType = m.MaterialType,
                    Url = m.Url,
                    CreatedAt = m.CreatedAt
                })
                .ToListAsync();

            return Ok(ApiResponse<List<LearningMaterialDto>>.Ok(materials, "Your materials retrieved successfully."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load the student's own materials.");
            return StatusCode(500, ApiResponse<List<LearningMaterialDto>>.Fail("An error occurred while loading your materials."));
        }
    }

    /// <summary>
    /// GET /api/materials/child/{studentId} — the materials for every subject in the given
    /// student's class (the same feed the student sees in /my). RBAC: an Admin, or a PARENT
    /// linked to this student via ParentStudents. A parent asking for a child that isn't
    /// theirs → 403. The acting user is taken from the validated JWT.
    /// </summary>
    // [Admin, or a parent linked to this student] Shows a parent every material for their
    // child's class (the same list the child sees). A parent asking for a child that isn't
    // theirs is refused.
    [HttpGet("child/{studentId:int}")]
    public async Task<IActionResult> GetForChild(int studentId)
    {
        try
        {
            if (!TryGetUserId(out var userId))
                return Unauthorized(ApiResponse<List<LearningMaterialDto>>.Fail("Invalid or missing authentication token."));

            // RBAC: an Admin always may; otherwise the caller must be a parent of this student.
            if (!User.IsInRole(nameof(UserRole.Admin)))
            {
                var linked = await _context.ParentStudents
                    .AnyAsync(ps => ps.ParentId == userId && ps.StudentId == studentId);

                if (!linked)
                    return StatusCode(StatusCodes.Status403Forbidden,
                        ApiResponse<List<LearningMaterialDto>>.Fail("You are not allowed to view this student's materials."));
            }

            // The child's class (student not found / not enrolled → empty feed, same as /my).
            var classId = await _context.Users
                .Where(u => u.Id == studentId)
                .Select(u => u.ClassId)
                .FirstOrDefaultAsync();

            if (classId is null)
                return Ok(ApiResponse<List<LearningMaterialDto>>.Ok([], "Materials retrieved successfully."));

            var materials = await _context.LearningMaterials
                .Where(m => m.Subject!.ClassId == classId.Value)
                .OrderBy(m => m.Subject!.Name).ThenByDescending(m => m.CreatedAt)
                .Select(m => new LearningMaterialDto
                {
                    Id = m.Id,
                    SubjectId = m.SubjectId,
                    SubjectName = m.Subject!.Name,
                    Title = m.Title,
                    Description = m.Description,
                    MaterialType = m.MaterialType,
                    Url = m.Url,
                    CreatedAt = m.CreatedAt
                })
                .ToListAsync();

            return Ok(ApiResponse<List<LearningMaterialDto>>.Ok(materials, "Materials retrieved successfully."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load materials for child {StudentId}.", studentId);
            return StatusCode(500, ApiResponse<List<LearningMaterialDto>>.Fail("An error occurred while loading materials."));
        }
    }

    /// <summary>
    /// DELETE /api/materials/{id} — the teacher who uploaded the material (or an admin) removes it.
    /// </summary>
    // [The teacher who uploaded it, or Admin] Deletes one material by its id.
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            var material = await _context.LearningMaterials.FindAsync(id);
            if (material is null)
                return NotFound(ApiResponse<object>.Fail("Material not found."));

            // RBAC: an admin always may; otherwise only the teacher who uploaded it.
            if (!User.IsInRole(nameof(UserRole.Admin)))
            {
                if (!TryGetUserId(out var userId))
                    return Unauthorized(ApiResponse<object>.Fail("Invalid or missing authentication token."));

                if (material.UploadedByTeacherId != userId)
                    return StatusCode(StatusCodes.Status403Forbidden,
                        ApiResponse<object>.Fail("You can only delete materials you added."));
            }

            _context.LearningMaterials.Remove(material);
            await _context.SaveChangesAsync();

            return Ok(ApiResponse<object>.Ok(new { id }, "Material deleted successfully."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete material {MaterialId}.", id);
            return StatusCode(500, ApiResponse<object>.Fail("An error occurred while deleting the material."));
        }
    }

    // ---- Helpers ----

    /// <summary>
    /// Enforce that the caller may MANAGE (add to) this subject's materials: an Admin always
    /// may; a Teacher only if TeacherSubjects assigns them to it. Returns null when allowed,
    /// otherwise the IActionResult to return (401/403). Same shape as the Grades controller.
    /// </summary>
    // Helper: the "can add to this subject" check. Admins always pass; a teacher passes only
    // if assigned to the subject. Returns null when allowed, or an error result (401/403).
    private async Task<IActionResult?> AuthorizeSubjectManageAsync(int subjectId)
    {
        if (User.IsInRole(nameof(UserRole.Admin)))
            return null;

        if (!TryGetUserId(out var userId))
            return Unauthorized(ApiResponse<object>.Fail("Invalid or missing authentication token."));

        var assigned = await _context.TeacherSubjects
            .AnyAsync(ts => ts.TeacherId == userId && ts.SubjectId == subjectId);

        if (!assigned)
            return StatusCode(StatusCodes.Status403Forbidden,
                ApiResponse<object>.Fail("You are not assigned to this subject, so you can't add materials to it."));

        return null;
    }

    /// <summary>
    /// Enforce that the caller may VIEW this subject's materials: an Admin, the assigned
    /// teacher, a student in the subject's class, or a parent of such a student. Returns null
    /// when allowed, otherwise the IActionResult to return (401/403).
    /// </summary>
    // Helper: the "can view this subject's materials" check. Allowed for an admin, the
    // assigned teacher, a student in the class, or a parent of such a student. Returns null
    // when allowed, or an error result (401/403).
    private async Task<IActionResult?> AuthorizeSubjectViewAsync(int subjectId, int classId)
    {
        if (User.IsInRole(nameof(UserRole.Admin)))
            return null;

        if (!TryGetUserId(out var userId))
            return Unauthorized(ApiResponse<object>.Fail("Invalid or missing authentication token."));

        // The teacher assigned to this subject.
        if (await _context.TeacherSubjects.AnyAsync(ts => ts.TeacherId == userId && ts.SubjectId == subjectId))
            return null;

        // A student enrolled in the subject's class.
        if (await _context.Users.AnyAsync(u => u.Id == userId && u.ClassId == classId))
            return null;

        // A parent of a student enrolled in the subject's class.
        if (await _context.ParentStudents.AnyAsync(ps => ps.ParentId == userId && ps.Student!.ClassId == classId))
            return null;

        return StatusCode(StatusCodes.Status403Forbidden,
            ApiResponse<object>.Fail("You are not allowed to view this subject's materials."));
    }

    /// <summary>
    /// Validate/normalize a material type. Returns the canonical value (case-insensitive), or
    /// null when it isn't one of the allowed types (Stage 1: "YouTube").
    /// </summary>
    // Helper: tidies a material type into "YouTube" or "Pdf" (ignoring casing); returns null
    // if it's neither.
    private static string? NormalizeMaterialType(string? type)
    {
        if (string.IsNullOrWhiteSpace(type)) return null;
        return AllowedMaterialTypes.FirstOrDefault(t => t.Equals(type.Trim(), StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Pull the current user's id from the validated JWT (NameIdentifier / "nameid" / "sub").
    /// Same approach as the Attendance / Grades / Events controllers.
    /// </summary>
    // Helper: reads the logged-in user's id out of their sign-in token (checks a few labels).
    private bool TryGetUserId(out int userId)
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier)
                    ?? User.FindFirstValue(JwtRegisteredClaimNames.NameId)
                    ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);
        return int.TryParse(claim, out userId);
    }
}
