using System.Collections.Frozen;
using HelpDesk.Domain.Enums;
using HelpDesk.Domain.Exceptions;

namespace HelpDesk.Domain.Rules;

/// <summary>
/// Única fuente de verdad de las transiciones de estado permitidas.
///
///   Open       → InProgress
///   InProgress → Resolved | Open   (resolver o devolver a la cola)
///   Resolved   → InProgress        (reapertura)
///
/// Open → Resolved está prohibido por regla de negocio, y también lo está
/// "cambiar" al mismo estado en el que ya se encuentra el ticket.
/// </summary>
public static class TicketStatusTransitionPolicy
{
    private static readonly FrozenDictionary<TicketStatus, IReadOnlyList<TicketStatus>> AllowedTransitions =
        new Dictionary<TicketStatus, IReadOnlyList<TicketStatus>>
        {
            [TicketStatus.Open] = [TicketStatus.InProgress],
            [TicketStatus.InProgress] = [TicketStatus.Resolved, TicketStatus.Open],
            [TicketStatus.Resolved] = [TicketStatus.InProgress],
        }.ToFrozenDictionary();

    /// <summary>
    /// Estados a los que puede pasar un ticket desde <paramref name="current"/>.
    /// La API los devuelve para que el frontend solo ofrezca opciones válidas.
    /// </summary>
    public static IReadOnlyList<TicketStatus> GetAllowedTransitions(TicketStatus current) =>
        AllowedTransitions.TryGetValue(current, out var targets) ? targets : [];

    public static bool CanTransition(TicketStatus current, TicketStatus target) =>
        GetAllowedTransitions(current).Contains(target);

    /// <exception cref="InvalidStatusTransitionException">Si la transición no está permitida.</exception>
    public static void EnsureCanTransition(TicketStatus current, TicketStatus target)
    {
        if (current == target)
        {
            throw new InvalidStatusTransitionException(
                current,
                target,
                $"El ticket ya se encuentra en estado '{current}'.");
        }

        if (!CanTransition(current, target))
        {
            var allowed = string.Join(", ", GetAllowedTransitions(current));
            throw new InvalidStatusTransitionException(
                current,
                target,
                $"No se permite cambiar el estado de '{current}' a '{target}'. Desde '{current}' solo se puede pasar a: {allowed}.");
        }
    }
}
