using HelpDesk.Application.DTOs.Common;
using HelpDesk.Application.DTOs.Tickets;
using HelpDesk.Application.Interfaces;
using HelpDesk.Domain.Entities;
using HelpDesk.Domain.Enums;
using HelpDesk.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HelpDesk.Infrastructure.Repositories;

internal sealed class TicketRepository(HelpDeskDbContext context) : ITicketRepository
{
    public Task<Ticket?> GetByIdAsync(int id, CancellationToken cancellationToken) =>
        context.Tickets
            .Include(ticket => ticket.AssignedUser)
            .FirstOrDefaultAsync(ticket => ticket.Id == id, cancellationToken);

    public async Task<PagedResult<Ticket>> GetPagedAsync(
        TicketQueryParameters parameters,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(parameters);

        var filtered = ApplyFilters(CreateBaseQuery(parameters.Tag).AsNoTracking(), parameters);

        var totalCount = await filtered.CountAsync(cancellationToken);

        var items = await ApplySorting(filtered, parameters.SortDirection)
            .Include(ticket => ticket.AssignedUser)
            .Skip((parameters.Page - 1) * parameters.PageSize)
            .Take(parameters.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<Ticket>(items, parameters.Page, parameters.PageSize, totalCount);
    }

    public async Task<IReadOnlyDictionary<TicketStatus, int>> GetStatusCountsAsync(CancellationToken cancellationToken) =>
        await context.Tickets
            .GroupBy(ticket => ticket.Status)
            .Select(group => new { Status = group.Key, Count = group.Count() })
            .ToDictionaryAsync(row => row.Status, row => row.Count, cancellationToken);

    public async Task AddAsync(Ticket ticket, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(ticket);

        context.Tickets.Add(ticket);

        // Tras el INSERT, EF Core lee el Id generado y las columnas calculadas
        // Priority y Category.
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Ticket ticket, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(ticket);

        // Normalmente el ticket viene de GetByIdAsync y ya está en seguimiento.
        if (context.Entry(ticket).State == EntityState.Detached)
        {
            context.Tickets.Update(ticket);
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> UpdatePriorityAsync(
        int id,
        TicketPriority priority,
        DateTime updatedAtUtc,
        CancellationToken cancellationToken)
    {
        // JSON_MODIFY devuelve el documento con solo '$.priority' cambiado.
        // Los valores interpolados se envían como parámetros (@p0, @p1, @p2).
        // La columna calculada Priority y su índice se actualizan solos, y los
        // CHECK de la tabla (ISJSON y prioridad permitida) siguen aplicándose.
        var affectedRows = await context.Database.ExecuteSqlAsync(
            $"""
            UPDATE dbo.Tickets
            SET AdditionalData = JSON_MODIFY(AdditionalData, N'$.priority', {priority.ToString()}),
                UpdatedAt      = {updatedAtUtc}
            WHERE Id = {id}
            """,
            cancellationToken);

        return affectedRows > 0;
    }

    public async Task<IReadOnlyList<TagUsageResponse>> GetTagUsageAsync(CancellationToken cancellationToken) =>
        await context.Database
            .SqlQuery<TagUsageResponse>(
                $"""
                SELECT tag.[value] AS Tag, COUNT(*) AS TicketCount
                FROM dbo.Tickets AS t
                CROSS APPLY OPENJSON(t.AdditionalData, N'$.tags') AS tag
                GROUP BY tag.[value]
                ORDER BY TicketCount DESC, Tag
                """)
            .ToListAsync(cancellationToken);

    /// <summary>
    /// Sin etiqueta, la consulta parte de la tabla. Con etiqueta, parte de una
    /// consulta SQL que recorre el arreglo JSON $.tags con OPENJSON.
    ///
    /// FromSql con una cadena interpolada envía {tag} como parámetro
    /// (@p0), nunca concatenado: no hay riesgo de inyección SQL. EF Core
    /// envuelve esta consulta como subconsulta y añade encima los demás
    /// filtros, el orden, el Include y la paginación.
    /// </summary>
    private IQueryable<Ticket> CreateBaseQuery(string? tag)
    {
        if (string.IsNullOrWhiteSpace(tag))
        {
            return context.Tickets;
        }

        var normalizedTag = tag.Trim();

        return context.Tickets.FromSql(
            $"""
            SELECT t.*
            FROM dbo.Tickets AS t
            WHERE EXISTS (
                SELECT 1
                FROM OPENJSON(t.AdditionalData, N'$.tags') AS tag
                WHERE tag.[value] = {normalizedTag}
            )
            """);
    }

    /// <summary>
    /// Cada filtro solo se añade si llega en la petición, así SQL Server recibe
    /// una consulta sencilla y puede usar el índice adecuado.
    /// </summary>
    private static IQueryable<Ticket> ApplyFilters(IQueryable<Ticket> query, TicketQueryParameters parameters)
    {
        if (parameters.Status is { } status)
        {
            query = query.Where(ticket => ticket.Status == status);
        }

        if (parameters.AssignedUserId is { } assignedUserId)
        {
            query = query.Where(ticket => ticket.AssignedUserId == assignedUserId);
        }

        // Priority y Category son columnas calculadas con JSON_VALUE: el filtro
        // se resuelve en SQL Server sobre un valor del JSON, con índice.
        if (parameters.Priority is { } priority)
        {
            query = query.Where(ticket => ticket.Priority == priority);
        }

        if (!string.IsNullOrWhiteSpace(parameters.Category))
        {
            var category = parameters.Category.Trim();
            query = query.Where(ticket => ticket.Category == category);
        }

        return query;
    }

    // Id como segundo criterio: con fechas iguales, el orden sigue siendo
    // estable entre páginas.
    private static IQueryable<Ticket> ApplySorting(IQueryable<Ticket> query, SortDirection direction) =>
        direction == SortDirection.Ascending
            ? query.OrderBy(ticket => ticket.CreatedAt).ThenBy(ticket => ticket.Id)
            : query.OrderByDescending(ticket => ticket.CreatedAt).ThenByDescending(ticket => ticket.Id);
}
