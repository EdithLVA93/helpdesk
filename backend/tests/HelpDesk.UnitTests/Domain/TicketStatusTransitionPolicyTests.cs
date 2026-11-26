using HelpDesk.Domain.Enums;
using HelpDesk.Domain.Exceptions;
using HelpDesk.Domain.Rules;

namespace HelpDesk.UnitTests.Domain;

/// <summary>
/// La política se prueba con las 9 combinaciones origen/destino (3 × 3):
/// 4 permitidas y 5 prohibidas (Open → Resolved, Resolved → Open y los 3
/// "cambios" al mismo estado).
/// </summary>
public sealed class TicketStatusTransitionPolicyTests
{
    public static TheoryData<TicketStatus, TicketStatus> AllowedTransitions => new()
    {
        { TicketStatus.Open, TicketStatus.InProgress },
        { TicketStatus.InProgress, TicketStatus.Resolved },
        { TicketStatus.InProgress, TicketStatus.Open },
        { TicketStatus.Resolved, TicketStatus.InProgress },
    };

    public static TheoryData<TicketStatus, TicketStatus> ForbiddenTransitions => new()
    {
        { TicketStatus.Open, TicketStatus.Resolved },
        { TicketStatus.Resolved, TicketStatus.Open },
        { TicketStatus.Open, TicketStatus.Open },
        { TicketStatus.InProgress, TicketStatus.InProgress },
        { TicketStatus.Resolved, TicketStatus.Resolved },
    };

    [Theory]
    [MemberData(nameof(AllowedTransitions))]
    public void CanTransition_AllowedTransition_ReturnsTrue(TicketStatus from, TicketStatus to)
    {
        Assert.True(TicketStatusTransitionPolicy.CanTransition(from, to));
    }

    [Theory]
    [MemberData(nameof(ForbiddenTransitions))]
    public void CanTransition_ForbiddenTransition_ReturnsFalse(TicketStatus from, TicketStatus to)
    {
        Assert.False(TicketStatusTransitionPolicy.CanTransition(from, to));
    }

    [Theory]
    [MemberData(nameof(AllowedTransitions))]
    public void EnsureCanTransition_AllowedTransition_DoesNotThrow(TicketStatus from, TicketStatus to)
    {
        var exception = Record.Exception(() => TicketStatusTransitionPolicy.EnsureCanTransition(from, to));

        Assert.Null(exception);
    }

    [Theory]
    [MemberData(nameof(ForbiddenTransitions))]
    public void EnsureCanTransition_ForbiddenTransition_ThrowsWithBothStates(TicketStatus from, TicketStatus to)
    {
        var exception = Assert.Throws<InvalidStatusTransitionException>(
            () => TicketStatusTransitionPolicy.EnsureCanTransition(from, to));

        Assert.Equal(from, exception.CurrentStatus);
        Assert.Equal(to, exception.RequestedStatus);
    }

    [Fact]
    public void EnsureCanTransition_OpenToResolved_ExplainsWhichTransitionsAreAllowed()
    {
        var exception = Assert.Throws<InvalidStatusTransitionException>(
            () => TicketStatusTransitionPolicy.EnsureCanTransition(TicketStatus.Open, TicketStatus.Resolved));

        Assert.Equal(
            "No se permite cambiar el estado de 'Open' a 'Resolved'. Desde 'Open' solo se puede pasar a: InProgress.",
            exception.Message);
    }

    [Fact]
    public void EnsureCanTransition_SameStatus_ExplainsThatNothingChanges()
    {
        var exception = Assert.Throws<InvalidStatusTransitionException>(
            () => TicketStatusTransitionPolicy.EnsureCanTransition(TicketStatus.InProgress, TicketStatus.InProgress));

        Assert.Equal("El ticket ya se encuentra en estado 'InProgress'.", exception.Message);
    }

    [Theory]
    [InlineData(TicketStatus.Open, new[] { TicketStatus.InProgress })]
    [InlineData(TicketStatus.InProgress, new[] { TicketStatus.Resolved, TicketStatus.Open })]
    [InlineData(TicketStatus.Resolved, new[] { TicketStatus.InProgress })]
    public void GetAllowedTransitions_ReturnsTargetsForEachStatus(TicketStatus from, TicketStatus[] expected)
    {
        Assert.Equal(expected, TicketStatusTransitionPolicy.GetAllowedTransitions(from));
    }

    [Fact]
    public void GetAllowedTransitions_NeverIncludesResolvedFromOpen()
    {
        Assert.DoesNotContain(TicketStatus.Resolved, TicketStatusTransitionPolicy.GetAllowedTransitions(TicketStatus.Open));
    }
}
