namespace HelpDesk.Application.DTOs.Tickets;

/// <summary>
/// Datos para crear un ticket (POST /api/tickets). El estado inicial siempre
/// es Open y la fecha de creación la asigna el servidor.
/// </summary>
/// <param name="Title">Título. Obligatorio, máximo 150 caracteres.</param>
/// <param name="Description">Descripción. Opcional, máximo 2000 caracteres.</param>
/// <param name="AssignedUserId">Id del usuario responsable. Obligatorio y debe existir.</param>
/// <param name="AdditionalData">Información adicional que se guarda como JSON. Obligatoria.</param>
public sealed record CreateTicketRequest(
    string? Title,
    string? Description,
    int AssignedUserId,
    TicketAdditionalDataDto? AdditionalData);
