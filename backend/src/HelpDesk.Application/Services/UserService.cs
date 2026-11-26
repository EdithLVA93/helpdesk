using FluentValidation;
using HelpDesk.Application.Common;
using HelpDesk.Application.DTOs.Users;
using HelpDesk.Application.Exceptions;
using HelpDesk.Application.Interfaces;
using HelpDesk.Application.Mappings;
using HelpDesk.Domain.Entities;

namespace HelpDesk.Application.Services;

internal sealed class UserService(
    IUserRepository userRepository,
    IValidator<CreateUserRequest> createValidator,
    TimeProvider timeProvider) : IUserService
{
    public async Task<UserResponse> CreateAsync(CreateUserRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        await createValidator.ValidateAndThrowAsync(request, cancellationToken);

        var email = User.NormalizeEmail(request.Email!);

        // Comprobación previa para dar un mensaje claro. El índice único de la
        // base de datos sigue protegiendo el caso de dos altas simultáneas
        // (UserRepository lo traduce también a ConflictException).
        if (await userRepository.EmailExistsAsync(email, cancellationToken))
        {
            throw new ConflictException($"Ya existe un usuario con el correo '{email}'.");
        }

        var user = User.Create(request.Name!, email, timeProvider.GetUtcNowTruncatedToMilliseconds());

        await userRepository.AddAsync(user, cancellationToken);

        return user.ToResponse();
    }

    public async Task<IReadOnlyList<UserResponse>> GetAllAsync(CancellationToken cancellationToken)
    {
        var users = await userRepository.GetAllAsync(cancellationToken);

        return [.. users.Select(user => user.ToResponse())];
    }

    public async Task<UserResponse> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException("el usuario", id);

        return user.ToResponse();
    }
}
