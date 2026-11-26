using HelpDesk.Application.DTOs.Users;

namespace HelpDesk.Application.Interfaces;

/// <summary>
/// Casos de uso de usuarios.
/// </summary>
public interface IUserService
{
    /// <exception cref="FluentValidation.ValidationException">Datos inválidos (400).</exception>
    /// <exception cref="Exceptions.ConflictException">Correo ya registrado (409).</exception>
    Task<UserResponse> CreateAsync(CreateUserRequest request, CancellationToken cancellationToken);

    Task<IReadOnlyList<UserResponse>> GetAllAsync(CancellationToken cancellationToken);
}
