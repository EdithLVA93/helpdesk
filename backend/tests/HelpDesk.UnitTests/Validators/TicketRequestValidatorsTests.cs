using FluentValidation.TestHelper;
using HelpDesk.Application.DTOs.Tickets;
using HelpDesk.Application.Validators;
using HelpDesk.Domain.Enums;

namespace HelpDesk.UnitTests.Validators;

/// <summary>Validadores de cambio de estado, cambio de prioridad y consulta.</summary>
public sealed class TicketRequestValidatorsTests
{
    [Fact]
    public void UpdateStatus_WithoutStatus_Fails()
    {
        var result = new UpdateTicketStatusRequestValidator().TestValidate(new UpdateTicketStatusRequest(null));

        result.ShouldHaveValidationErrorFor(request => request.Status).WithErrorMessage("El estado es obligatorio.");
    }

    [Fact]
    public void UpdateStatus_UnknownStatus_Fails()
    {
        var result = new UpdateTicketStatusRequestValidator().TestValidate(new UpdateTicketStatusRequest((TicketStatus)42));

        result.ShouldHaveValidationErrorFor(request => request.Status);
    }

    [Fact]
    public void UpdateStatus_ResolvedFromAnyState_IsValidFormat()
    {
        // El validador solo comprueba el formato. Si Open → Resolved está
        // permitido lo decide el dominio, que conoce el estado actual.
        var result = new UpdateTicketStatusRequestValidator().TestValidate(new UpdateTicketStatusRequest(TicketStatus.Resolved));

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void UpdatePriority_WithoutPriority_Fails()
    {
        var result = new UpdateTicketPriorityRequestValidator().TestValidate(new UpdateTicketPriorityRequest(null));

        result.ShouldHaveValidationErrorFor(request => request.Priority);
    }

    [Fact]
    public void Query_Defaults_AreValid()
    {
        new TicketQueryParametersValidator().TestValidate(new TicketQueryParameters()).ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData(0, 10)]
    [InlineData(1, 0)]
    [InlineData(1, 101)]
    public void Query_InvalidPaging_Fails(int page, int pageSize)
    {
        var result = new TicketQueryParametersValidator().TestValidate(new TicketQueryParameters { Page = page, PageSize = pageSize });

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Query_TagLongerThan30_Fails()
    {
        var result = new TicketQueryParametersValidator().TestValidate(new TicketQueryParameters { Tag = new string('x', 31) });

        result.ShouldHaveValidationErrorFor(query => query.Tag);
    }

    [Fact]
    public void Query_AssignedUserIdZero_Fails()
    {
        var result = new TicketQueryParametersValidator().TestValidate(new TicketQueryParameters { AssignedUserId = 0 });

        result.ShouldHaveValidationErrorFor(query => query.AssignedUserId);
    }
}
