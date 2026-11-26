using HelpDesk.Application.DTOs.Tickets;
using HelpDesk.Domain.Entities;
using HelpDesk.Domain.Rules;

namespace HelpDesk.Application.Mappings;

internal static class TicketMappings
{
    /// <summary>
    /// Mapea un ticket leído con su usuario responsable (Include).
    /// </summary>
    public static TicketResponse ToResponse(this Ticket ticket)
    {
        var assignedUserName = ticket.AssignedUser?.Name
            ?? throw new InvalidOperationException(
                $"El ticket {ticket.Id} se mapeó sin cargar su usuario responsable (falta Include).");

        return ticket.ToResponse(assignedUserName);
    }

    /// <summary>
    /// Mapea un ticket cuando el nombre del responsable ya se conoce (por
    /// ejemplo, al crearlo), sin otra consulta a la base de datos.
    /// </summary>
    public static TicketResponse ToResponse(this Ticket ticket, string assignedUserName) =>
        new(
            ticket.Id,
            ticket.Title,
            ticket.Description,
            ticket.Status,
            ticket.AssignedUserId,
            assignedUserName,
            ticket.CreatedAt,
            ticket.UpdatedAt,
            TicketAdditionalDataJson.Deserialize(ticket.AdditionalData),
            TicketStatusTransitionPolicy.GetAllowedTransitions(ticket.Status));
}
