namespace Honeydew.Controllers.Models;

/// <summary>Standard error body for all API error responses (4xx/5xx).</summary>
public class ApiErrorResponse
{
    public string Error { get; set; } = string.Empty;
    public string? Code { get; set; }

    public static ApiErrorResponse From(string message, string? code = null) => new()
    {
        Error = message,
        Code = code,
    };

    public static ApiErrorResponse Validation(string message) => From(message, "Validation");
    public static ApiErrorResponse NotFound(string message = "Resource not found.") => From(message, "NotFound");
    public static ApiErrorResponse Forbid(string message = "Access denied.") => From(message, "Forbid");
}
