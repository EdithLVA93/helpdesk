using HelpDesk.Domain.Enums;

namespace HelpDesk.Application.DTOs.Tickets;

/// <summary>
/// Ticket devuelto por la API.
/// </summary>
/// <param name="AllowedTransitions">
/// Estados a los que puede pasar el ticket desde su estado actual. El frontend
/// los usa para ofrecer solo opciones válidas; la API sigue siendo quien decide.
/// </param>
public sealed record TicketResponse(
    int Id,
    string Title,
    string? Description,
    TicketStatus Status,
    int AssignedUserId,
    string AssignedUserName,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    TicketAdditionalDataDto AdditionalData,
    IReadOnlyList<TicketStatus> AllowedTransitions);
