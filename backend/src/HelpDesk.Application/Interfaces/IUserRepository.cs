using HelpDesk.Domain.Entities;

namespace HelpDesk.Application.Interfaces;

/// <summary>
/// Acceso a datos de usuarios. Se define en Application y se implementa en
/// Infrastructure (inversión de dependencias): los servicios no conocen EF Core.
/// </summary>
public interface IUserRepository
{
    Task<User?> GetByIdAsync(int id, CancellationToken cancellationToken);

    Task<IReadOnlyList<User>> GetAllAsync(CancellationToken cancellationToken);

    /// <param name="normalizedEmail">Correo ya normalizado con <see cref="User.NormalizeEmail"/>.</param>
    Task<bool> EmailExistsAsync(string normalizedEmail, CancellationToken cancellationToken);

    /// <summary>Agrega el usuario y guarda los cambios.</summary>
    /// <exception cref="Exceptions.ConflictException">Si el correo ya existe.</exception>
    Task AddAsync(User user, CancellationToken cancellationToken);
}
