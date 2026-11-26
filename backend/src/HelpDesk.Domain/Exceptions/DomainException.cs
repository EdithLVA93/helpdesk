namespace HelpDesk.Domain.Exceptions;

/// <summary>
/// Base de las excepciones que representan una regla de negocio incumplida.
/// La API las traduce a 409 Conflict: la petición es válida, pero entra en
/// conflicto con el estado actual del recurso.
/// </summary>
public abstract class DomainException : Exception
{
    protected DomainException(string message)
        : base(message)
    {
    }

    protected DomainException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
