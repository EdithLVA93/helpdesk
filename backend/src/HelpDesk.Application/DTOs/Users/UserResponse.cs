namespace HelpDesk.Application.DTOs.Users;

/// <summary>
/// Usuario devuelto por la API.
/// </summary>
public sealed record UserResponse(int Id, string Name, string Email, DateTime CreatedAt);
