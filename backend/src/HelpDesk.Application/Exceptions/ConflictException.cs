namespace HelpDesk.Application.Exceptions;

/// <summary>
/// La petición entra en conflicto con datos existentes (por ejemplo, un
/// correo ya registrado). La API responde 409.
/// </summary>
public sealed class ConflictException : Exception
{
    public ConflictException(string message)
        : base(message)
    {
    }

    public ConflictException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
