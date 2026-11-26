namespace HelpDesk.Domain.Enums;

/// <summary>
/// Estados posibles de un ticket. Se guardan como texto en la base de datos
/// (CK_Tickets_Status) y viajan como texto en la API.
/// </summary>
public enum TicketStatus
{
    Open,
    InProgress,
    Resolved,
}
