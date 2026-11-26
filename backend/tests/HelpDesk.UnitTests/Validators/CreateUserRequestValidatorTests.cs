using FluentValidation.TestHelper;
using HelpDesk.Application.DTOs.Users;
using HelpDesk.Application.Validators;

namespace HelpDesk.UnitTests.Validators;

public sealed class CreateUserRequestValidatorTests
{
    private readonly CreateUserRequestValidator validator = new();

    [Fact]
    public void Validate_ValidUser_HasNoErrors()
    {
        var result = validator.TestValidate(new CreateUserRequest("Ana Torres", "ana.torres@example.com"));

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_MissingName_FailsWithMessage(string? name)
    {
        var result = validator.TestValidate(new CreateUserRequest(name, "ana@example.com"));

        result.ShouldHaveValidationErrorFor(request => request.Name).WithErrorMessage("El nombre es obligatorio.");
    }

    [Fact]
    public void Validate_NameLongerThan100_Fails()
    {
        var result = validator.TestValidate(new CreateUserRequest(new string('a', 101), "ana@example.com"));

        result.ShouldHaveValidationErrorFor(request => request.Name);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Validate_MissingEmail_FailsWithMessage(string? email)
    {
        var result = validator.TestValidate(new CreateUserRequest("Ana", email));

        result.ShouldHaveValidationErrorFor(request => request.Email)
            .WithErrorMessage("El correo electrónico es obligatorio.");
    }

    [Theory]
    [InlineData("correo-invalido")]
    [InlineData("ana@")]
    [InlineData("@example.com")]
    [InlineData("ana@example")]
    [InlineData("ana torres@example.com")]
    [InlineData("ana@@example.com")]
    public void Validate_BadEmailFormat_FailsWithMessage(string email)
    {
        var result = validator.TestValidate(new CreateUserRequest("Ana", email));

        result.ShouldHaveValidationErrorFor(request => request.Email)
            .WithErrorMessage("El correo electrónico no tiene un formato válido.");
    }

    [Theory]
    [InlineData("ana@example.com")]
    [InlineData("ana.torres+soporte@empresa.com.co")]
    [InlineData("  ana@example.com  ")]
    public void Validate_GoodEmailFormat_Passes(string email)
    {
        var result = validator.TestValidate(new CreateUserRequest("Ana", email));

        result.ShouldNotHaveValidationErrorFor(request => request.Email);
    }
}
