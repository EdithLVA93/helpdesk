using HelpDesk.Domain.Enums;

namespace HelpDesk.Application.DTOs.Tickets;

/// <summary>
/// Nueva prioridad del ticket (PATCH /api/tickets/{id}/priority).
/// Solo cambia $.priority dentro de AdditionalData; el resto del JSON se conserva.
/// </summary>
/// <param name="Priority">Low, Medium, High o Critical.</param>
public sealed record UpdateTicketPriorityRequest(TicketPriority? Priority);
