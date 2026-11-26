using HelpDesk.Domain.Enums;
using HelpDesk.Domain.Rules;

namespace HelpDesk.Domain.Entities;

/// <summary>
/// Solicitud de soporte asignada a un usuario responsable.
/// Todo ticket nace en estado Open y solo cambia de estado mediante
/// <see cref="ChangeStatus"/>, que aplica <see cref="TicketStatusTransitionPolicy"/>.
/// </summary>
public sealed class Ticket
{
    public const int TitleMaxLength = 150;
    public const int DescriptionMaxLength = 2000;
    public const int CategoryMaxLength = 50;
    public const int MaxTags = 10;
    public const int TagMaxLength = 30;
    public const string EmptyAdditionalData = "{}";

    // Constructor para EF Core.
    private Ticket()
    {
    }

    public int Id { get; private set; }

    public string Title { get; private set; } = string.Empty;

    public string? Description { get; private set; }

    public TicketStatus Status { get; private set; }

    public int AssignedUserId { get; private set; }

    public User? AssignedUser { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public DateTime? UpdatedAt { get; private set; }

    /// <summary>
    /// Información adicional en formato JSON (columna NVARCHAR(MAX)).
    /// </summary>
    public string AdditionalData { get; private set; } = EmptyAdditionalData;

    /// <summary>
    /// Columna calculada por SQL Server con JSON_VALUE(AdditionalData, '$.priority').
    /// Es de solo lectura: EF Core la lee pero nunca la escribe.
    /// </summary>
    public TicketPriority? Priority { get; private set; }

    /// <summary>
    /// Columna calculada por SQL Server con JSON_VALUE(AdditionalData, '$.category').
    /// </summary>
    public string? Category { get; private set; }

    public static Ticket Create(
        string title,
        string? description,
        int assignedUserId,
        string additionalData,
        DateTime createdAtUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(assignedUserId);
        ArgumentException.ThrowIfNullOrWhiteSpace(additionalData);

        return new Ticket
        {
            Title = title.Trim(),
            Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
            Status = TicketStatus.Open,
            AssignedUserId = assignedUserId,
            AdditionalData = additionalData,
            CreatedAt = createdAtUtc,
        };
    }

    /// <summary>
    /// Cambia el estado del ticket si la transición está permitida.
    /// </summary>
    /// <exception cref="Exceptions.InvalidStatusTransitionException">
    /// Si la transición no está permitida (por ejemplo, Open → Resolved).
    /// </exception>
    public void ChangeStatus(TicketStatus newStatus, DateTime changedAtUtc)
    {
        TicketStatusTransitionPolicy.EnsureCanTransition(Status, newStatus);

        Status = newStatus;
        UpdatedAt = changedAtUtc;
    }
}
