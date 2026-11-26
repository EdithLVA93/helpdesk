using HelpDesk.Domain.Entities;
using HelpDesk.Domain.Enums;
using HelpDesk.Domain.Exceptions;
using HelpDesk.UnitTests.TestDoubles;

namespace HelpDesk.UnitTests.Domain;

public sealed class TicketTests
{
    private static readonly DateTime Now = new(2026, 11, 25, 15, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Create_ValidData_StartsOpenWithCreationDate()
    {
        var ticket = Ticket.Create("  Título  ", "  Detalle  ", 7, "{}", Now);

        Assert.Equal(TicketStatus.Open, ticket.Status);
        Assert.Equal("Título", ticket.Title);
        Assert.Equal("Detalle", ticket.Description);
        Assert.Equal(7, ticket.AssignedUserId);
        Assert.Equal(Now, ticket.CreatedAt);
        Assert.Null(ticket.UpdatedAt);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_BlankDescription_StoresNull(string? description)
    {
        var ticket = Ticket.Create("Título", description, 1, "{}", Now);

        Assert.Null(ticket.Description);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_BlankTitle_Throws(string title)
    {
        Assert.ThrowsAny<ArgumentException>(() => Ticket.Create(title, null, 1, "{}", Now));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Create_WithoutAssignedUser_Throws(int assignedUserId)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Ticket.Create("Título", null, assignedUserId, "{}", Now));
    }

    [Fact]
    public void ChangeStatus_AllowedTransition_ChangesStatusAndUpdatedAt()
    {
        var ticket = TestData.Ticket(status: TicketStatus.Open);

        ticket.ChangeStatus(TicketStatus.InProgress, Now);

        Assert.Equal(TicketStatus.InProgress, ticket.Status);
        Assert.Equal(Now, ticket.UpdatedAt);
    }

    [Fact]
    public void ChangeStatus_OpenToResolved_ThrowsAndKeepsTicketUnchanged()
    {
        var ticket = TestData.Ticket(status: TicketStatus.Open);

        Assert.Throws<InvalidStatusTransitionException>(() => ticket.ChangeStatus(TicketStatus.Resolved, Now));

        Assert.Equal(TicketStatus.Open, ticket.Status);
        Assert.Null(ticket.UpdatedAt);
    }

    [Fact]
    public void ChangeStatus_FullLifecycleWithReopening_Succeeds()
    {
        var ticket = TestData.Ticket(status: TicketStatus.Open);

        ticket.ChangeStatus(TicketStatus.InProgress, Now);
        ticket.ChangeStatus(TicketStatus.Resolved, Now.AddHours(1));
        ticket.ChangeStatus(TicketStatus.InProgress, Now.AddHours(2));

        Assert.Equal(TicketStatus.InProgress, ticket.Status);
        Assert.Equal(Now.AddHours(2), ticket.UpdatedAt);
    }
}
