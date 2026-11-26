using HelpDesk.Application.DTOs.Common;
using HelpDesk.Application.DTOs.Tickets;
using HelpDesk.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace HelpDesk.Api.Controllers;

/// <summary>
/// Gestión de tickets. Sin lógica de negocio: la regla de transiciones de
/// estado vive en el dominio (TicketStatusTransitionPolicy) y la aplica el
/// servicio.
/// </summary>
[ApiController]
[Route("api/tickets")]
[Produces("application/json")]
public sealed class TicketsController(ITicketService ticketService) : ControllerBase
{
    /// <summary>Crea un ticket asignado a un usuario. Nace en estado Open.</summary>
    /// <response code="201">Ticket creado. La cabecera Location apunta a GET /api/tickets/{id}.</response>
    /// <response code="400">Datos inválidos: sin título, sin usuario asignado, usuario inexistente o información adicional incorrecta.</response>
    [HttpPost]
    [Consumes("application/json")]
    [ProducesResponseType<TicketResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    public async Task<ActionResult<TicketResponse>> Create(CreateTicketRequest request, CancellationToken cancellationToken)
    {
        var ticket = await ticketService.CreateAsync(request, cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = ticket.Id }, ticket);
    }

    /// <summary>
    /// Lista tickets con filtros opcionales combinables, orden por fecha de
    /// creación y paginación.
    /// </summary>
    /// <remarks>
    /// Ejemplos:
    /// GET /api/tickets?status=Open
    /// GET /api/tickets?assignedUserId=1&amp;status=InProgress
    /// GET /api/tickets?priority=High&amp;category=Infrastructure&amp;sortDirection=Ascending&amp;page=1&amp;pageSize=20
    /// GET /api/tickets?tag=network   (busca dentro del arreglo JSON $.tags con OPENJSON)
    /// </remarks>
    /// <response code="200">Página de tickets.</response>
    /// <response code="400">Parámetros inválidos (estado desconocido, página menor que 1...).</response>
    [HttpGet]
    [ProducesResponseType<PagedResult<TicketResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    public async Task<ActionResult<PagedResult<TicketResponse>>> GetPaged(
        [FromQuery] TicketQueryParameters parameters,
        CancellationToken cancellationToken) =>
        Ok(await ticketService.GetPagedAsync(parameters, cancellationToken));

    /// <summary>Obtiene un ticket por su id.</summary>
    /// <response code="200">Ticket encontrado.</response>
    /// <response code="404">No existe un ticket con ese id.</response>
    [HttpGet("{id:int}")]
    [ProducesResponseType<TicketResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<ActionResult<TicketResponse>> GetById(int id, CancellationToken cancellationToken) =>
        Ok(await ticketService.GetByIdAsync(id, cancellationToken));

    /// <summary>Cambia el estado de un ticket respetando las transiciones permitidas.</summary>
    /// <remarks>
    /// Transiciones permitidas: Open → InProgress; InProgress → Resolved u Open;
    /// Resolved → InProgress. Open → Resolved no está permitido.
    /// </remarks>
    /// <response code="200">Estado actualizado. Devuelve el ticket con sus nuevas transiciones permitidas.</response>
    /// <response code="400">Estado ausente o desconocido.</response>
    /// <response code="404">No existe un ticket con ese id.</response>
    /// <response code="409">Transición no permitida (por ejemplo, Open → Resolved).</response>
    [HttpPut("{id:int}/status")]
    [Consumes("application/json")]
    [ProducesResponseType<TicketResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, "application/problem+json")]
    public async Task<ActionResult<TicketResponse>> ChangeStatus(
        int id,
        UpdateTicketStatusRequest request,
        CancellationToken cancellationToken) =>
        Ok(await ticketService.ChangeStatusAsync(id, request, cancellationToken));

    /// <summary>
    /// Cambia solo la prioridad del ticket. En SQL Server se ejecuta con
    /// JSON_MODIFY sobre AdditionalData, sin reescribir el resto del JSON.
    /// </summary>
    /// <response code="200">Prioridad actualizada. Devuelve el ticket completo.</response>
    /// <response code="400">Prioridad ausente o desconocida.</response>
    /// <response code="404">No existe un ticket con ese id.</response>
    [HttpPatch("{id:int}/priority")]
    [Consumes("application/json")]
    [ProducesResponseType<TicketResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<ActionResult<TicketResponse>> ChangePriority(
        int id,
        UpdateTicketPriorityRequest request,
        CancellationToken cancellationToken) =>
        Ok(await ticketService.ChangePriorityAsync(id, request, cancellationToken));

    /// <summary>
    /// Etiquetas usadas en los tickets y cuántos tickets tiene cada una
    /// (OPENJSON + GROUP BY en SQL Server). Alimenta el filtro por etiqueta.
    /// </summary>
    /// <response code="200">Etiquetas ordenadas de más a menos usadas.</response>
    [HttpGet("tags")]
    [ProducesResponseType<IReadOnlyList<TagUsageResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<TagUsageResponse>>> GetTagUsage(CancellationToken cancellationToken) =>
        Ok(await ticketService.GetTagUsageAsync(cancellationToken));

    /// <summary>Cantidad de tickets por estado, para las tarjetas de indicadores.</summary>
    /// <response code="200">Totales por estado.</response>
    [HttpGet("summary")]
    [ProducesResponseType<TicketSummaryResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<TicketSummaryResponse>> GetSummary(CancellationToken cancellationToken) =>
        Ok(await ticketService.GetSummaryAsync(cancellationToken));
}
