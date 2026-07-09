// ============================================
// ApiResponse — the standard reply shape for every API answer.
// Job: wrap every response in the same format so the Vue website always
// gets the same three fields: was it successful, a message, and the data.
// Think of it as a consistent envelope for all our answers.
// Used by: every Controller in the project.
// ============================================
namespace AlmanahilAPI.Helpers;

/// <summary>
/// A generic wrapper that gives every API endpoint a consistent JSON shape:
/// { success, message, data }.
/// </summary>
/// <typeparam name="T">The type of the payload returned in <see cref="Data"/>.</typeparam>
// The reply box. <T> just means it can carry any type of data inside.
public class ApiResponse<T>
{
    /// <summary>True when the operation succeeded.</summary>
    // true = it worked, false = something went wrong.
    public bool Success { get; set; }

    /// <summary>A human-friendly message describing the result.</summary>
    // A short text we can show the user (e.g. "Login successful").
    public string Message { get; set; } = string.Empty;

    /// <summary>The payload. Null on failure (or when there is nothing to return).</summary>
    // The actual information being sent back (e.g. a list of students). Empty if it failed.
    public T? Data { get; set; }

    /// <summary>Builds a successful response carrying the given data.</summary>
    // Shortcut to make a "success" reply with data. Sets Success = true.
    public static ApiResponse<T> Ok(T data, string message = "Success") => new()
    {
        Success = true,
        Message = message,
        Data = data
    };

    /// <summary>Builds a failed response with an explanatory message and no data.</summary>
    // Shortcut to make a "failed" reply with just an error message. Sets Success = false.
    public static ApiResponse<T> Fail(string message) => new()
    {
        Success = false,
        Message = message,
        Data = default
    };
}
