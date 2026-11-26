namespace HelpDesk.Application.DTOs.Tickets;

/// <summary>
/// Etiqueta usada en los tickets y cuántos tickets la tienen. Se calcula en
/// SQL Server recorriendo el arreglo JSON $.tags con OPENJSON.
/// </summary>
/// <remarks>
/// Propiedades con init (y no un record posicional) porque EF Core la
/// materializa directamente desde una consulta SQL (Database.SqlQuery).
/// </remarks>
public sealed record TagUsageResponse
{
    public required string Tag { get; init; }

    public required int TicketCount { get; init; }
}
