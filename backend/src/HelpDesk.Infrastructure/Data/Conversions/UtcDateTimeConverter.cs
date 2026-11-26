using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace HelpDesk.Infrastructure.Data.Conversions;

/// <summary>
/// Guarda el DateTime tal cual (ya es UTC) y al leerlo le asigna DateTimeKind.Utc.
/// </summary>
internal sealed class UtcDateTimeConverter()
    : ValueConverter<DateTime, DateTime>(
        value => value,
        value => DateTime.SpecifyKind(value, DateTimeKind.Utc));
