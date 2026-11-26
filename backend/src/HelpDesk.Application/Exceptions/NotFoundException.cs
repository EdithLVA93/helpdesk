namespace HelpDesk.Application.Exceptions;

/// <summary>
/// El recurso solicitado en la URL no existe. La API responde 404.
/// </summary>
public sealed class NotFoundException : Exception
{
    public NotFoundException(string resourceName, object key)
        : base($"No se encontró {resourceName} con id '{key}'.")
    {
    }
}
