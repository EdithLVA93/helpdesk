namespace HelpDesk.Application.DTOs.Users;

/// <summary>
/// Datos para crear un usuario (POST /api/users).
/// Las propiedades admiten null porque el JSON puede omitirlas; el validador
/// decide qué es obligatorio y devuelve un mensaje claro.
/// </summary>
/// <param name="Name">Nombre completo. Obligatorio, máximo 100 caracteres.</param>
/// <param name="Email">Correo electrónico. Obligatorio, único, máximo 254 caracteres.</param>
public sealed record CreateUserRequest(string? Name, string? Email);
