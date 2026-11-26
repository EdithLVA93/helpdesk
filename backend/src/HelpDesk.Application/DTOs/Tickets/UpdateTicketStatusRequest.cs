using HelpDesk.Domain.Enums;

namespace HelpDesk.Application.DTOs.Tickets;

/// <summary>
/// Nuevo estado del ticket (PUT /api/tickets/{id}/status).
/// </summary>
/// <param name="Status">Estado destino: Open, InProgress o Resolved.</param>
public sealed record UpdateTicketStatusRequest(TicketStatus? Status);
