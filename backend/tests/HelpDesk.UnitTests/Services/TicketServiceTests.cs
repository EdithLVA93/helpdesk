using FluentValidation;
using HelpDesk.Application.DTOs.Tickets;
using HelpDesk.Application.Exceptions;
using HelpDesk.Application.Interfaces;
using HelpDesk.Application.Services;
using HelpDesk.Application.Validators;
using HelpDesk.Domain.Entities;
using HelpDesk.Domain.Enums;
using HelpDesk.Domain.Exceptions;
using HelpDesk.UnitTests.TestDoubles;
using NSubstitute;

namespace HelpDesk.UnitTests.Services;

/// <summary>
/// TicketService con validadores reales y repositorios simulados: se prueba la
/// orquestación del caso de uso sin base de datos.
/// </summary>
public sealed class TicketServiceTests
{
    private readonly ITicketRepository ticketRepository = Substitute.For<ITicketRepository>();
    private readonly IUserRepository userRepository = Substitute.For<IUserRepository>();
    private readonly FixedTimeProvider timeProvider = new();
    private readonly TicketService service;

    public TicketServiceTests()
    {
        service = new TicketService(
            ticketRepository,
            userRepository,
            new CreateTicketRequestValidator(timeProvider),
            new UpdateTicketStatusRequestValidator(),
            new UpdateTicketPriorityRequestValidator(),
            new TicketQueryParametersValidator(),
            timeProvider);
    }

    // ---------------------------------------------------------------- Crear

    [Fact]
    public async Task CreateAsync_ValidRequest_SavesOpenTicketWithNormalizedJson()
    {
        var user = TestData.User(id: 3, name: "Lucía Ramírez");
        userRepository.GetByIdAsync(3, Arg.Any<CancellationToken>()).Returns(user);
        Ticket? saved = null;
        await ticketRepository.AddAsync(Arg.Do<Ticket>(ticket => saved = ticket), Arg.Any<CancellationToken>());

        var response = await service.CreateAsync(TestData.ValidCreateTicketRequest(assignedUserId: 3), CancellationToken.None);

        Assert.NotNull(saved);
        Assert.Equal(TicketStatus.Open, saved.Status);
        Assert.Equal(3, saved.AssignedUserId);
        Assert.Equal(FixedTimeProvider.DefaultNow.UtcDateTime, saved.CreatedAt);

        // Etiquetas en minúsculas, sin espacios ni duplicados; propiedades en camelCase.
        Assert.Equal(
            """{"priority":"High","category":"Network","estimatedResolutionDate":"2026-12-15","tags":["email","mobile"],"metadata":{"department":"Sales"}}""",
            saved.AdditionalData);

        Assert.Equal("Lucía Ramírez", response.AssignedUserName);
        Assert.Equal([TicketStatus.InProgress], response.AllowedTransitions);
    }

    [Fact]
    public async Task CreateAsync_AssignedUserDoesNotExist_ThrowsValidationErrorOnAssignedUserId()
    {
        userRepository.GetByIdAsync(99, Arg.Any<CancellationToken>()).Returns((User?)null);

        var exception = await Assert.ThrowsAsync<ValidationException>(
            () => service.CreateAsync(TestData.ValidCreateTicketRequest(assignedUserId: 99), CancellationToken.None));

        var error = Assert.Single(exception.Errors);
        Assert.Equal(nameof(CreateTicketRequest.AssignedUserId), error.PropertyName);
        Assert.Equal("El usuario asignado con id '99' no existe.", error.ErrorMessage);
        await ticketRepository.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
    }

    [Fact]
    public async Task CreateAsync_WithoutTitle_FailsBeforeTouchingTheDatabase()
    {
        var request = TestData.ValidCreateTicketRequest() with { Title = "  " };

        await Assert.ThrowsAsync<ValidationException>(() => service.CreateAsync(request, CancellationToken.None));

        await userRepository.DidNotReceiveWithAnyArgs().GetByIdAsync(default, default);
        await ticketRepository.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
    }

    // --------------------------------------------------------- Cambiar estado

    [Fact]
    public async Task ChangeStatusAsync_OpenToResolved_ThrowsAndDoesNotSave()
    {
        var ticket = TestData.Ticket(id: 10, status: TicketStatus.Open);
        ticketRepository.GetByIdAsync(10, Arg.Any<CancellationToken>()).Returns(ticket);

        await Assert.ThrowsAsync<InvalidStatusTransitionException>(
            () => service.ChangeStatusAsync(10, new UpdateTicketStatusRequest(TicketStatus.Resolved), CancellationToken.None));

        Assert.Equal(TicketStatus.Open, ticket.Status);
        await ticketRepository.DidNotReceiveWithAnyArgs().UpdateAsync(default!, default);
    }

    [Fact]
    public async Task ChangeStatusAsync_OpenToInProgress_SavesAndReturnsNextAllowedTransitions()
    {
        var ticket = TestData.Ticket(id: 10, status: TicketStatus.Open);
        ticketRepository.GetByIdAsync(10, Arg.Any<CancellationToken>()).Returns(ticket);

        var response = await service.ChangeStatusAsync(
            10,
            new UpdateTicketStatusRequest(TicketStatus.InProgress),
            CancellationToken.None);

        Assert.Equal(TicketStatus.InProgress, response.Status);
        Assert.Equal(FixedTimeProvider.DefaultNow.UtcDateTime, response.UpdatedAt);
        Assert.Equal([TicketStatus.Resolved, TicketStatus.Open], response.AllowedTransitions);
        await ticketRepository.Received(1).UpdateAsync(ticket, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ChangeStatusAsync_TicketDoesNotExist_ThrowsNotFound()
    {
        ticketRepository.GetByIdAsync(404, Arg.Any<CancellationToken>()).Returns((Ticket?)null);

        var exception = await Assert.ThrowsAsync<NotFoundException>(
            () => service.ChangeStatusAsync(404, new UpdateTicketStatusRequest(TicketStatus.InProgress), CancellationToken.None));

        Assert.Equal("No se encontró el ticket con id '404'.", exception.Message);
    }

    [Fact]
    public async Task ChangeStatusAsync_WithoutStatus_ThrowsValidationBeforeLoadingTicket()
    {
        await Assert.ThrowsAsync<ValidationException>(
            () => service.ChangeStatusAsync(10, new UpdateTicketStatusRequest(null), CancellationToken.None));

        await ticketRepository.DidNotReceiveWithAnyArgs().GetByIdAsync(default, default);
    }

    // ------------------------------------------------------- Otros casos de uso

    [Fact]
    public async Task GetByIdAsync_TicketDoesNotExist_ThrowsNotFound()
    {
        ticketRepository.GetByIdAsync(7, Arg.Any<CancellationToken>()).Returns((Ticket?)null);

        await Assert.ThrowsAsync<NotFoundException>(() => service.GetByIdAsync(7, CancellationToken.None));
    }

    [Fact]
    public async Task ChangePriorityAsync_TicketDoesNotExist_ThrowsNotFound()
    {
        ticketRepository
            .UpdatePriorityAsync(8, TicketPriority.Low, Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(false);

        await Assert.ThrowsAsync<NotFoundException>(
            () => service.ChangePriorityAsync(8, new UpdateTicketPriorityRequest(TicketPriority.Low), CancellationToken.None));
    }

    [Fact]
    public async Task GetSummaryAsync_StatusWithoutTickets_CountsAsZero()
    {
        ticketRepository.GetStatusCountsAsync(Arg.Any<CancellationToken>())
            .Returns(new Dictionary<TicketStatus, int> { [TicketStatus.Open] = 5, [TicketStatus.Resolved] = 2 });

        var summary = await service.GetSummaryAsync(CancellationToken.None);

        Assert.Equal(new TicketSummaryResponse(Total: 7, Open: 5, InProgress: 0, Resolved: 2), summary);
    }
}
