using HelpDesk.Domain.Enums;

namespace HelpDesk.Domain.Exceptions;

/// <summary>
/// Se lanza cuando se intenta un cambio de estado no permitido,
/// por ejemplo Open → Resolved.
/// </summary>
public sealed class InvalidStatusTransitionException : DomainException
{
    public InvalidStatusTransitionException(TicketStatus currentStatus, TicketStatus requestedStatus, string message)
        : base(message)
    {
        CurrentStatus = currentStatus;
        RequestedStatus = requestedStatus;
    }

    public TicketStatus CurrentStatus { get; }

    public TicketStatus RequestedStatus { get; }
}
