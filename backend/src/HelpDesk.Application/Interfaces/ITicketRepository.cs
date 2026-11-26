using HelpDesk.Application.DTOs.Common;
using HelpDesk.Application.DTOs.Tickets;
using HelpDesk.Domain.Entities;
using HelpDesk.Domain.Enums;

namespace HelpDesk.Application.Interfaces;

/// <summary>
/// Acceso a datos de tickets.
/// </summary>
public interface ITicketRepository
{
    /// <summary>
    /// Obtiene un ticket con su usuario responsable. La entidad queda en
    /// seguimiento para poder modificarla y guardarla con <see cref="UpdateAsync"/>.
    /// </summary>
    Task<Ticket?> GetByIdAsync(int id, CancellationToken cancellationToken);

    /// <summary>
    /// Aplica los filtros, el orden y la paginación en SQL Server e incluye
    /// el usuario responsable de cada ticket.
    /// </summary>
    Task<PagedResult<Ticket>> GetPagedAsync(TicketQueryParameters parameters, CancellationToken cancellationToken);

    /// <summary>Cantidad de tickets por estado, calculada con GROUP BY.</summary>
    Task<IReadOnlyDictionary<TicketStatus, int>> GetStatusCountsAsync(CancellationToken cancellationToken);

    /// <summary>Agrega el ticket y guarda los cambios.</summary>
    Task AddAsync(Ticket ticket, CancellationToken cancellationToken);

    /// <summary>Guarda los cambios de un ticket existente.</summary>
    Task UpdateAsync(Ticket ticket, CancellationToken cancellationToken);

    /// <summary>
    /// Cambia solo $.priority dentro de AdditionalData con JSON_MODIFY, sin
    /// leer ni reescribir el resto del documento JSON.
    /// </summary>
    /// <returns>false si el ticket no existe.</returns>
    Task<bool> UpdatePriorityAsync(int id, TicketPriority priority, DateTime updatedAtUtc, CancellationToken cancellationToken);

    /// <summary>
    /// Etiquetas usadas en los tickets con su número de tickets, calculado con
    /// OPENJSON + GROUP BY. Ordenadas de más a menos usadas.
    /// </summary>
    Task<IReadOnlyList<TagUsageResponse>> GetTagUsageAsync(CancellationToken cancellationToken);
}
