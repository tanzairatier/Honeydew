namespace Honeydew.Services.Models;

public record AuthResult(bool Success, string? Token, string? Error);
