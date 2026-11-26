namespace HelpDesk.Application.DTOs.Common;

/// <summary>
/// Página de resultados con la información necesaria para el paginador.
/// </summary>
public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount)
{
    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
}
