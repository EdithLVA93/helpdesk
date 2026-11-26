namespace HelpDesk.Application.DTOs.Tickets;

/// <summary>
/// Conteo de tickets por estado para las tarjetas de indicadores.
/// </summary>
public sealed record TicketSummaryResponse(int Total, int Open, int InProgress, int Resolved);
