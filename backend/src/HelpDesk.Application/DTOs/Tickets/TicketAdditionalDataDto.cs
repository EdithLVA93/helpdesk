using HelpDesk.Domain.Enums;

namespace HelpDesk.Application.DTOs.Tickets;

/// <summary>
/// Información adicional del ticket. Se guarda como JSON en la columna
/// Tickets.AdditionalData con esta misma forma (propiedades en camelCase):
/// <code>
/// {
///   "priority": "High",
///   "category": "Infrastructure",
///   "estimatedResolutionDate": "2026-11-30",
///   "tags": ["network", "server"],
///   "metadata": { "department": "IT", "source": "Internal" }
/// }
/// </code>
/// </summary>
/// <param name="Priority">Prioridad. Obligatoria.</param>
/// <param name="Category">Categoría. Obligatoria, máximo 50 caracteres.</param>
/// <param name="EstimatedResolutionDate">Fecha estimada de solución (yyyy-MM-dd). Opcional, no anterior a hoy al crear.</param>
/// <param name="Tags">Etiquetas. Opcional, máximo 10, de hasta 30 caracteres cada una.</param>
/// <param name="Metadata">Metadatos libres clave/valor. Opcional.</param>
public sealed record TicketAdditionalDataDto(
    TicketPriority? Priority,
    string? Category,
    DateOnly? EstimatedResolutionDate,
    IReadOnlyList<string>? Tags,
    IReadOnlyDictionary<string, string>? Metadata);
