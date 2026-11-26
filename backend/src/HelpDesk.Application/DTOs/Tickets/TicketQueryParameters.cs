using HelpDesk.Application.DTOs.Common;
using HelpDesk.Domain.Enums;

namespace HelpDesk.Application.DTOs.Tickets;

/// <summary>
/// Filtros, orden y paginación de GET /api/tickets. Todos los filtros son
/// opcionales y se combinan entre sí (AND).
/// </summary>
public sealed class TicketQueryParameters
{
    public const int DefaultPageSize = 10;
    public const int MaxPageSize = 100;

    /// <summary>Filtra por estado.</summary>
    public TicketStatus? Status { get; init; }

    /// <summary>Filtra por usuario responsable.</summary>
    public int? AssignedUserId { get; init; }

    /// <summary>Filtra por prioridad (valor dentro del JSON).</summary>
    public TicketPriority? Priority { get; init; }

    /// <summary>Filtra por categoría (valor dentro del JSON).</summary>
    public string? Category { get; init; }

    /// <summary>Orden por fecha de creación. Por defecto, los más recientes primero.</summary>
    public SortDirection SortDirection { get; init; } = SortDirection.Descending;

    /// <summary>Número de página, empezando en 1.</summary>
    public int Page { get; init; } = 1;

    /// <summary>Tamaño de página, entre 1 y 100.</summary>
    public int PageSize { get; init; } = DefaultPageSize;
}
