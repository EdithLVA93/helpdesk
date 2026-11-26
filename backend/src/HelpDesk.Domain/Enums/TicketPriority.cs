namespace HelpDesk.Domain.Enums;

/// <summary>
/// Prioridad de un ticket. Se guarda dentro del JSON AdditionalData
/// ($.priority) y la base de datos la restringe con CK_Tickets_Priority.
/// </summary>
public enum TicketPriority
{
    Low,
    Medium,
    High,
    Critical,
}
