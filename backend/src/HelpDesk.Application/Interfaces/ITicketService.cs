using HelpDesk.Application.DTOs.Common;
using HelpDesk.Application.DTOs.Tickets;

namespace HelpDesk.Application.Interfaces;

/// <summary>
/// Casos de uso de tickets.
/// </summary>
public interface ITicketService
{
    /// <exception cref="FluentValidation.ValidationException">Datos inválidos o usuario asignado inexistente (400).</exception>
    Task<TicketResponse> CreateAsync(CreateTicketRequest request, CancellationToken cancellationToken);

    /// <exception cref="FluentValidation.ValidationException">Parámetros de consulta inválidos (400).</exception>
    Task<PagedResult<TicketResponse>> GetPagedAsync(TicketQueryParameters parameters, CancellationToken cancellationToken);

    /// <exception cref="Exceptions.NotFoundException">El ticket no existe (404).</exception>
    Task<TicketResponse> GetByIdAsync(int id, CancellationToken cancellationToken);

    /// <exception cref="FluentValidation.ValidationException">Estado ausente o inválido (400).</exception>
    /// <exception cref="Exceptions.NotFoundException">El ticket no existe (404).</exception>
    /// <exception cref="Domain.Exceptions.InvalidStatusTransitionException">Transición no permitida (409).</exception>
    Task<TicketResponse> ChangeStatusAsync(int id, UpdateTicketStatusRequest request, CancellationToken cancellationToken);

    /// <exception cref="FluentValidation.ValidationException">Prioridad ausente o inválida (400).</exception>
    /// <exception cref="Exceptions.NotFoundException">El ticket no existe (404).</exception>
    Task<TicketResponse> ChangePriorityAsync(int id, UpdateTicketPriorityRequest request, CancellationToken cancellationToken);

    Task<TicketSummaryResponse> GetSummaryAsync(CancellationToken cancellationToken);

    Task<IReadOnlyList<TagUsageResponse>> GetTagUsageAsync(CancellationToken cancellationToken);
}
