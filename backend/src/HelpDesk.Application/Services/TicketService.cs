using FluentValidation;
using FluentValidation.Results;
using HelpDesk.Application.Common;
using HelpDesk.Application.DTOs.Common;
using HelpDesk.Application.DTOs.Tickets;
using HelpDesk.Application.Exceptions;
using HelpDesk.Application.Interfaces;
using HelpDesk.Application.Mappings;
using HelpDesk.Domain.Entities;
using HelpDesk.Domain.Enums;

namespace HelpDesk.Application.Services;

internal sealed class TicketService(
    ITicketRepository ticketRepository,
    IUserRepository userRepository,
    IValidator<CreateTicketRequest> createValidator,
    IValidator<UpdateTicketStatusRequest> statusValidator,
    IValidator<UpdateTicketPriorityRequest> priorityValidator,
    IValidator<TicketQueryParameters> queryValidator,
    TimeProvider timeProvider) : ITicketService
{
    private const string TicketResourceName = "el ticket";

    public async Task<TicketResponse> CreateAsync(CreateTicketRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        await createValidator.ValidateAndThrowAsync(request, cancellationToken);

        // Regla de negocio: el usuario asignado debe existir. Se informa como
        // error de validación del campo assignedUserId (400), porque el recurso
        // de la URL (/api/tickets) sí existe: lo inválido es un dato del cuerpo.
        var assignedUser = await userRepository.GetByIdAsync(request.AssignedUserId, cancellationToken)
            ?? throw new ValidationException(
            [
                new ValidationFailure(
                    nameof(CreateTicketRequest.AssignedUserId),
                    $"El usuario asignado con id '{request.AssignedUserId}' no existe."),
            ]);

        var additionalData = TicketAdditionalDataJson.Serialize(Normalize(request.AdditionalData!));

        var ticket = Ticket.Create(
            request.Title!,
            request.Description,
            assignedUser.Id,
            additionalData,
            timeProvider.GetUtcNowTruncatedToMilliseconds());

        await ticketRepository.AddAsync(ticket, cancellationToken);

        return ticket.ToResponse(assignedUser.Name);
    }

    public async Task<PagedResult<TicketResponse>> GetPagedAsync(
        TicketQueryParameters parameters,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(parameters);

        await queryValidator.ValidateAndThrowAsync(parameters, cancellationToken);

        var page = await ticketRepository.GetPagedAsync(parameters, cancellationToken);

        return new PagedResult<TicketResponse>(
            [.. page.Items.Select(ticket => ticket.ToResponse())],
            page.Page,
            page.PageSize,
            page.TotalCount);
    }

    public async Task<TicketResponse> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        var ticket = await GetExistingTicketAsync(id, cancellationToken);

        return ticket.ToResponse();
    }

    public async Task<TicketResponse> ChangeStatusAsync(
        int id,
        UpdateTicketStatusRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        await statusValidator.ValidateAndThrowAsync(request, cancellationToken);

        var ticket = await GetExistingTicketAsync(id, cancellationToken);

        // La regla de transiciones vive en el dominio: si no está permitida,
        // lanza InvalidStatusTransitionException y no se guarda nada.
        ticket.ChangeStatus(request.Status!.Value, timeProvider.GetUtcNowTruncatedToMilliseconds());

        await ticketRepository.UpdateAsync(ticket, cancellationToken);

        return ticket.ToResponse();
    }

    public async Task<TicketResponse> ChangePriorityAsync(
        int id,
        UpdateTicketPriorityRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        await priorityValidator.ValidateAndThrowAsync(request, cancellationToken);

        // Actualización parcial en SQL Server (JSON_MODIFY). La prioridad no
        // depende del estado, así que no hay regla de dominio que aplicar.
        var updated = await ticketRepository.UpdatePriorityAsync(
            id,
            request.Priority!.Value,
            timeProvider.GetUtcNowTruncatedToMilliseconds(),
            cancellationToken);

        if (!updated)
        {
            throw new NotFoundException(TicketResourceName, id);
        }

        // Se vuelve a leer para devolver el JSON tal como quedó guardado.
        return await GetByIdAsync(id, cancellationToken);
    }

    public Task<IReadOnlyList<TagUsageResponse>> GetTagUsageAsync(CancellationToken cancellationToken) =>
        ticketRepository.GetTagUsageAsync(cancellationToken);

    public async Task<TicketSummaryResponse> GetSummaryAsync(CancellationToken cancellationToken)
    {
        var counts = await ticketRepository.GetStatusCountsAsync(cancellationToken);

        var open = counts.GetValueOrDefault(TicketStatus.Open);
        var inProgress = counts.GetValueOrDefault(TicketStatus.InProgress);
        var resolved = counts.GetValueOrDefault(TicketStatus.Resolved);

        return new TicketSummaryResponse(open + inProgress + resolved, open, inProgress, resolved);
    }

    private async Task<Ticket> GetExistingTicketAsync(int id, CancellationToken cancellationToken) =>
        await ticketRepository.GetByIdAsync(id, cancellationToken)
        ?? throw new NotFoundException(TicketResourceName, id);

    /// <summary>
    /// Limpia los datos antes de guardarlos: categoría y claves sin espacios
    /// sobrantes, etiquetas en minúsculas y sin duplicados.
    /// </summary>
    private static TicketAdditionalDataDto Normalize(TicketAdditionalDataDto data)
    {
#pragma warning disable CA1308 // Las etiquetas se guardan en minúsculas por convención, para filtrar de forma uniforme.
        var tags = data.Tags?
            .Select(tag => tag.Trim().ToLowerInvariant())
            .Distinct(StringComparer.Ordinal)
            .ToList();
#pragma warning restore CA1308

        return data with
        {
            Category = data.Category?.Trim(),
            Tags = tags,
            Metadata = NormalizeMetadata(data.Metadata),
        };
    }

    /// <summary>
    /// Claves sin espacios y sin distinguir mayúsculas. Si dos claves coinciden
    /// tras normalizarlas ("dept" y "Dept "), gana la última en vez de fallar.
    /// </summary>
    private static Dictionary<string, string>? NormalizeMetadata(IReadOnlyDictionary<string, string>? metadata)
    {
        if (metadata is null)
        {
            return null;
        }

        var normalized = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var (key, value) in metadata)
        {
            normalized[key.Trim()] = value?.Trim() ?? string.Empty;   // el JSON puede traer "clave": null
        }

        return normalized;
    }
}
