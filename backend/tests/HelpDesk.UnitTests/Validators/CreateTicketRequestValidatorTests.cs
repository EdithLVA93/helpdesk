using FluentValidation.TestHelper;
using HelpDesk.Application.DTOs.Tickets;
using HelpDesk.Application.Validators;
using HelpDesk.Domain.Enums;
using HelpDesk.UnitTests.TestDoubles;

namespace HelpDesk.UnitTests.Validators;

/// <summary>
/// Reloj fijado en el 25/11/2026 (UTC): la fecha estimada se acepta desde el
/// 24/11 (tolerancia de un día por husos horarios por detrás de UTC).
/// </summary>
public sealed class CreateTicketRequestValidatorTests
{
    private readonly CreateTicketRequestValidator validator = new(new FixedTimeProvider());

    private static CreateTicketRequest ValidRequest => TestData.ValidCreateTicketRequest();

    private static CreateTicketRequest WithData(Func<TicketAdditionalDataDto, TicketAdditionalDataDto> change) =>
        ValidRequest with { AdditionalData = change(ValidRequest.AdditionalData!) };

    [Fact]
    public void Validate_ValidTicket_HasNoErrors()
    {
        validator.TestValidate(ValidRequest).ShouldNotHaveAnyValidationErrors();
    }

    // ---- Reglas de negocio de la prueba ----

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_WithoutTitle_FailsBecauseTitleIsRequired(string? title)
    {
        var result = validator.TestValidate(ValidRequest with { Title = title });

        result.ShouldHaveValidationErrorFor(request => request.Title)
            .WithErrorMessage("El título del ticket es obligatorio.");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void Validate_WithoutAssignedUser_FailsBecauseTicketMustBeAssigned(int assignedUserId)
    {
        var result = validator.TestValidate(ValidRequest with { AssignedUserId = assignedUserId });

        result.ShouldHaveValidationErrorFor(request => request.AssignedUserId)
            .WithErrorMessage("El ticket debe tener un usuario asignado.");
    }

    // ---- Límites de longitud ----

    [Fact]
    public void Validate_TitleLongerThan150_Fails()
    {
        var result = validator.TestValidate(ValidRequest with { Title = new string('t', 151) });

        result.ShouldHaveValidationErrorFor(request => request.Title);
    }

    [Fact]
    public void Validate_DescriptionLongerThan2000_Fails()
    {
        var result = validator.TestValidate(ValidRequest with { Description = new string('d', 2001) });

        result.ShouldHaveValidationErrorFor(request => request.Description);
    }

    // ---- Información adicional (JSON) ----

    [Fact]
    public void Validate_WithoutAdditionalData_Fails()
    {
        var result = validator.TestValidate(ValidRequest with { AdditionalData = null });

        result.ShouldHaveValidationErrorFor(request => request.AdditionalData);
    }

    [Fact]
    public void Validate_WithoutPriority_Fails()
    {
        var result = validator.TestValidate(WithData(data => data with { Priority = null }));

        result.ShouldHaveValidationErrorFor("AdditionalData.Priority").WithErrorMessage("La prioridad es obligatoria.");
    }

    [Fact]
    public void Validate_UnknownPriorityValue_Fails()
    {
        var result = validator.TestValidate(WithData(data => data with { Priority = (TicketPriority)99 }));

        result.ShouldHaveValidationErrorFor("AdditionalData.Priority");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("  ")]
    public void Validate_WithoutCategory_Fails(string? category)
    {
        var result = validator.TestValidate(WithData(data => data with { Category = category }));

        result.ShouldHaveValidationErrorFor("AdditionalData.Category");
    }

    [Theory]
    [InlineData(2026, 11, 23)]
    [InlineData(2020, 1, 1)]
    public void Validate_EstimatedDateInThePast_Fails(int year, int month, int day)
    {
        var result = validator.TestValidate(WithData(data => data with { EstimatedResolutionDate = new DateOnly(year, month, day) }));

        result.ShouldHaveValidationErrorFor("AdditionalData.EstimatedResolutionDate")
            .WithErrorMessage("La fecha estimada de solución no puede ser anterior a hoy.");
    }

    [Theory]
    [InlineData(2026, 11, 24)] // "ayer" en UTC: todavía es hoy en UTC-5
    [InlineData(2026, 11, 25)]
    [InlineData(2027, 1, 31)]
    public void Validate_EstimatedDateTodayOrLater_Passes(int year, int month, int day)
    {
        var result = validator.TestValidate(WithData(data => data with { EstimatedResolutionDate = new DateOnly(year, month, day) }));

        result.ShouldNotHaveValidationErrorFor("AdditionalData.EstimatedResolutionDate");
    }

    [Fact]
    public void Validate_EstimatedDateMissing_IsOptional()
    {
        var result = validator.TestValidate(WithData(data => data with { EstimatedResolutionDate = null }));

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_MoreThan10Tags_Fails()
    {
        var tags = Enumerable.Range(1, 11).Select(number => $"tag{number}").ToList();

        var result = validator.TestValidate(WithData(data => data with { Tags = tags }));

        result.ShouldHaveValidationErrorFor("AdditionalData.Tags");
    }

    [Fact]
    public void Validate_TagLongerThan30_Fails()
    {
        var result = validator.TestValidate(WithData(data => data with { Tags = [new string('x', 31)] }));

        result.ShouldHaveValidationErrorFor("AdditionalData.Tags[0]");
    }

    [Fact]
    public void Validate_MetadataValueNull_IsAllowed()
    {
        var metadata = new Dictionary<string, string> { ["department"] = null! };

        var result = validator.TestValidate(WithData(data => data with { Metadata = metadata }));

        result.ShouldNotHaveAnyValidationErrors();
    }
}
