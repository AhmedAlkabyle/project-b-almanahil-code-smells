// ============================================
// SupabaseStorageService — uploads files (like PDFs) to online storage.
// Job: take a file's bytes, send them to Supabase Storage (a cloud file box)
// over the internet, and get back a public web link to that file.
// We save only the link in our database, not the file itself.
// Used by: the learning-materials feature when a teacher uploads a PDF.
// ============================================
using System.Net.Http.Headers;
using AlmanahilAPI.Settings;
using Microsoft.Extensions.Options;

namespace AlmanahilAPI.Services;

/// <summary>
/// Uploads files to Supabase Storage via its Storage REST API and returns the public URL.
/// Module 5 Stage 2 stores only that URL (in LearningMaterial.Url, MaterialType="Pdf") — the
/// bytes live in the bucket, not the DB. Uses a typed HttpClient (registered via
/// IHttpClientFactory in Program.cs). The service_role key comes from SupabaseSettings.
/// </summary>
// The class that talks to Supabase Storage. It gets its settings (URL, key, bucket) injected.
public class SupabaseStorageService(
    HttpClient httpClient,
    IOptions<SupabaseSettings> options,
    ILogger<SupabaseStorageService> logger)
{
    private readonly HttpClient _httpClient = httpClient;
    private readonly SupabaseSettings _settings = options.Value;
    private readonly ILogger<SupabaseStorageService> _logger = logger;

    /// <summary>True only when Url + ServiceRoleKey + Bucket are all present (config is filled in).</summary>
    // True if the Supabase settings are all filled in, so uploads can actually work.
    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(_settings.Url)
        && !string.IsNullOrWhiteSpace(_settings.ServiceRoleKey)
        && !string.IsNullOrWhiteSpace(_settings.Bucket);

    /// <summary>
    /// Upload <paramref name="content"/> to {Url}/storage/v1/object/{bucket}/{path} with the
    /// service_role key, and return its public URL
    /// {Url}/storage/v1/object/public/{bucket}/{path}. Throws on a non-success response.
    /// </summary>
    // Uploads the file bytes and returns the public web link to it.
    // Receives: the file's bytes, the path/name to save it under, and its file type.
    public async Task<string> UploadAsync(byte[] content, string path, string contentType, CancellationToken ct = default)
    {
        // Step 1: Build the web address where this file will be uploaded (bucket + path).
        var baseUrl = _settings.Url.TrimEnd('/');
        var bucket = _settings.Bucket;
        var objectUrl = $"{baseUrl}/storage/v1/object/{bucket}/{path}";

        // Step 2: Create the upload request and add our secret key twice (Supabase needs both).
        using var request = new HttpRequestMessage(HttpMethod.Post, objectUrl);
        // Supabase's API gateway (Kong) requires BOTH the bearer token AND the apikey header;
        // with only Authorization it answers 401 "No API key found in request".
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _settings.ServiceRoleKey);
        request.Headers.TryAddWithoutValidation("apikey", _settings.ServiceRoleKey);
        // "x-upsert: true" means if a file with the same name exists, overwrite it instead of erroring.
        // Upsert so a retried path overwrites rather than failing with "already exists".
        request.Headers.TryAddWithoutValidation("x-upsert", "true");

        // Step 3: Attach the actual file bytes to the request, tagged with the file type.
        var body = new ByteArrayContent(content);
        body.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        request.Content = body;

        // Step 4: Send the file to Supabase over the internet and wait for the reply.
        HttpResponseMessage response;
        try
        {
            response = await _httpClient.SendAsync(request, ct);
        }
        catch (Exception ex)
        {
            // If we couldn't even reach Supabase (no internet, etc.), log it and stop with an error.
            // Network / DNS / TLS failure — the request never reached Supabase.
            _logger.LogError(ex, "Supabase Storage request to {Url} failed at the network level.", objectUrl);
            Console.WriteLine($"[Supabase upload] NETWORK ERROR calling {objectUrl}: {ex.Message}");
            throw new InvalidOperationException($"Could not reach the storage service at {objectUrl}: {ex.Message}", ex);
        }

        // Step 5: If Supabase rejected the upload, log the reason and stop with an error.
        if (!response.IsSuccessStatusCode)
        {
            var detail = await response.Content.ReadAsStringAsync(ct);
            // TEMPORARY DEBUG: log the real status + body to the terminal AND carry it up in the
            // exception message so the endpoint can surface it. Revert to a generic message later.
            _logger.LogError("Supabase Storage upload to {Url} failed ({Status}): {Detail}",
                objectUrl, (int)response.StatusCode, detail);
            Console.WriteLine($"[Supabase upload] {(int)response.StatusCode} {response.StatusCode} for {objectUrl} -> {detail}");
            throw new InvalidOperationException(
                $"Supabase Storage returned {(int)response.StatusCode} {response.StatusCode}: {detail}");
        }

        // Step 6: Upload worked — build and return the public web link to the file.
        _logger.LogInformation("Supabase Storage upload OK: {Url}", objectUrl);
        // Public bucket → the object is served from the /public/ path.
        return $"{baseUrl}/storage/v1/object/public/{bucket}/{path}";
    }
}
