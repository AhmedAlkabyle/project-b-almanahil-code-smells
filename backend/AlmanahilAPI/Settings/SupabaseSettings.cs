// ============================================
// SupabaseSettings — the file-storage configuration for uploading files.
// This is NOT a DTO; it holds settings the app reads from appsettings.json.
// Supabase is the outside service where we keep uploaded files (like PDFs).
// ============================================
namespace AlmanahilAPI.Settings;

// Holds the Supabase storage settings, filled in from appsettings.json when the app starts.
/// <summary>
/// Strongly-typed Supabase Storage configuration, bound from the "Supabase" section of
/// appsettings.json in Program.cs. Used by <c>SupabaseStorageService</c> to upload files
/// (Module 5 Stage 2 — PDFs) to a public bucket. The service_role key is a SECRET — it
/// lives only in appsettings.json, never in code.
/// </summary>
public class SupabaseSettings
{
    // The name of the block in appsettings.json these settings come from ("Supabase").
    /// <summary>The configuration section name these settings are bound from.</summary>
    public const string SectionName = "Supabase";

    // The web address of our Supabase project.
    /// <summary>The project base URL, e.g. "https://&lt;ref&gt;.supabase.co".</summary>
    public string Url { get; set; } = string.Empty;

    // Names the secret access key (the value itself lives in appsettings.json, never in code).
    /// <summary>The service_role key, sent as "Authorization: Bearer &lt;key&gt;". SECRET.</summary>
    public string ServiceRoleKey { get; set; } = string.Empty;

    // The name of the storage "folder" (bucket) files are uploaded into.
    /// <summary>The public storage bucket name, e.g. "learning-materials".</summary>
    public string Bucket { get; set; } = "learning-materials";
}
