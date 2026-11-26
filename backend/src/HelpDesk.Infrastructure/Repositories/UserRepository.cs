using HelpDesk.Application.Exceptions;
using HelpDesk.Application.Interfaces;
using HelpDesk.Domain.Entities;
using HelpDesk.Infrastructure.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace HelpDesk.Infrastructure.Repositories;

internal sealed class UserRepository(HelpDeskDbContext context) : IUserRepository
{
    // Códigos de SQL Server para violación de índice único / restricción UNIQUE.
    private const int UniqueIndexViolation = 2601;
    private const int UniqueConstraintViolation = 2627;

    public Task<User?> GetByIdAsync(int id, CancellationToken cancellationToken) =>
        context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(user => user.Id == id, cancellationToken);

    public async Task<IReadOnlyList<User>> GetAllAsync(CancellationToken cancellationToken) =>
        await context.Users
            .AsNoTracking()
            .OrderBy(user => user.Name)
            .ToListAsync(cancellationToken);

    public Task<bool> EmailExistsAsync(string normalizedEmail, CancellationToken cancellationToken) =>
        context.Users.AnyAsync(user => user.Email == normalizedEmail, cancellationToken);

    public async Task AddAsync(User user, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(user);

        context.Users.Add(user);

        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (exception.InnerException is SqlException
        {
            Number: UniqueIndexViolation or UniqueConstraintViolation,
        })
        {
            // Dos altas simultáneas con el mismo correo pasan la comprobación
            // previa del servicio; el índice único UX_Users_Email lo impide.
            throw new ConflictException($"Ya existe un usuario con el correo '{user.Email}'.", exception);
        }
    }
}
