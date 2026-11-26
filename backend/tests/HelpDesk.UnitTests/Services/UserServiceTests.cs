using FluentValidation;
using HelpDesk.Application.DTOs.Users;
using HelpDesk.Application.Exceptions;
using HelpDesk.Application.Interfaces;
using HelpDesk.Application.Services;
using HelpDesk.Application.Validators;
using HelpDesk.Domain.Entities;
using HelpDesk.UnitTests.TestDoubles;
using NSubstitute;

namespace HelpDesk.UnitTests.Services;

public sealed class UserServiceTests
{
    private readonly IUserRepository userRepository = Substitute.For<IUserRepository>();
    private readonly UserService service;

    public UserServiceTests()
    {
        service = new UserService(userRepository, new CreateUserRequestValidator(), new FixedTimeProvider());
    }

    [Fact]
    public async Task CreateAsync_ValidUser_SavesNormalizedEmail()
    {
        User? saved = null;
        await userRepository.AddAsync(Arg.Do<User>(user => saved = user), Arg.Any<CancellationToken>());

        var response = await service.CreateAsync(
            new CreateUserRequest(" Ana Torres ", " Ana.Torres@Example.com "),
            CancellationToken.None);

        Assert.NotNull(saved);
        Assert.Equal("Ana Torres", response.Name);
        Assert.Equal("ana.torres@example.com", response.Email);
        await userRepository.Received(1).EmailExistsAsync("ana.torres@example.com", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_EmailAlreadyRegistered_ThrowsConflictAndDoesNotSave()
    {
        userRepository.EmailExistsAsync("ana.torres@example.com", Arg.Any<CancellationToken>()).Returns(true);

        var exception = await Assert.ThrowsAsync<ConflictException>(
            () => service.CreateAsync(new CreateUserRequest("Otra Ana", "ANA.TORRES@example.com"), CancellationToken.None));

        Assert.Equal("Ya existe un usuario con el correo 'ana.torres@example.com'.", exception.Message);
        await userRepository.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
    }

    [Fact]
    public async Task CreateAsync_InvalidEmail_ThrowsValidationBeforeQueryingTheDatabase()
    {
        await Assert.ThrowsAsync<ValidationException>(
            () => service.CreateAsync(new CreateUserRequest("Ana", "correo-invalido"), CancellationToken.None));

        await userRepository.DidNotReceiveWithAnyArgs().EmailExistsAsync(default!, default);
    }

    [Fact]
    public async Task GetByIdAsync_UserDoesNotExist_ThrowsNotFound()
    {
        userRepository.GetByIdAsync(5, Arg.Any<CancellationToken>()).Returns((User?)null);

        await Assert.ThrowsAsync<NotFoundException>(() => service.GetByIdAsync(5, CancellationToken.None));
    }
}
