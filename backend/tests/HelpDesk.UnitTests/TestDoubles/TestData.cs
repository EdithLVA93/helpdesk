using System.Reflection;
using HelpDesk.Application.DTOs.Tickets;
using HelpDesk.Domain.Entities;
using HelpDesk.Domain.Enums;

namespace HelpDesk.UnitTests.TestDoubles;

/// <summary>
/// Construye entidades y peticiones de prueba.
/// Id y AssignedUser los asigna EF Core al leer de la base de datos (setters
/// privados); aquí se simulan con reflexión, solo en los tests.
/// </summary>
internal static class TestData
{
    public static readonly DateTime CreatedAt = new(2026, 11, 20, 10, 0, 0, DateTimeKind.Utc);

    public static User User(int id = 1, string name = "Ana Torres", string email = "ana.torres@example.com")
    {
        var user = global::HelpDesk.Domain.Entities.User.Create(name, email, CreatedAt);
        SetPrivate(user, nameof(global::HelpDesk.Domain.Entities.User.Id), id);
        return user;
    }

    /// <summary>Ticket como lo devolvería el repositorio: con Id y usuario cargado.</summary>
    public static Ticket Ticket(int id = 10, TicketStatus status = TicketStatus.Open, User? assignedUser = null)
    {
        assignedUser ??= User();

        var ticket = global::HelpDesk.Domain.Entities.Ticket.Create(
            "VPN desconecta cada 10 minutos",
            "Cortes frecuentes trabajando desde casa.",
            assignedUser.Id,
            """{"priority":"High","category":"Network","tags":["vpn"]}""",
            CreatedAt);

        SetPrivate(ticket, nameof(global::HelpDesk.Domain.Entities.Ticket.Id), id);
        SetPrivate(ticket, nameof(global::HelpDesk.Domain.Entities.Ticket.AssignedUser), assignedUser);

        // Se llega al estado pedido por transiciones válidas, como en la realidad.
        if (status != TicketStatus.Open)
        {
            ticket.ChangeStatus(TicketStatus.InProgress, CreatedAt.AddHours(1));
        }

        if (status == TicketStatus.Resolved)
        {
            ticket.ChangeStatus(TicketStatus.Resolved, CreatedAt.AddHours(2));
        }

        return ticket;
    }

    public static CreateTicketRequest ValidCreateTicketRequest(int assignedUserId = 1) =>
        new(
            "El correo no sincroniza",
            "Outlook en Android muestra el error 0x800CCC0E.",
            assignedUserId,
            new TicketAdditionalDataDto(
                TicketPriority.High,
                "Network",
                new DateOnly(2026, 12, 15),
                ["Email", " mobile ", "email"],
                new Dictionary<string, string> { ["department"] = "Sales" }));

    private static void SetPrivate(object target, string propertyName, object value) =>
        target.GetType()
            .GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public)!
            .SetValue(target, value);
}
