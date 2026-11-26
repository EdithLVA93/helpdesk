using System.Text.RegularExpressions;
using FluentValidation;
using HelpDesk.Application.DTOs.Users;
using HelpDesk.Domain.Entities;

namespace HelpDesk.Application.Validators;

internal sealed partial class CreateUserRequestValidator : AbstractValidator<CreateUserRequest>
{
    public CreateUserRequestValidator()
    {
        RuleFor(request => request.Name)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("El nombre es obligatorio.")
            .MaximumLength(User.NameMaxLength)
            .WithMessage($"El nombre no puede superar {User.NameMaxLength} caracteres.");

        RuleFor(request => request.Email)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("El correo electrónico es obligatorio.")
            .MaximumLength(User.EmailMaxLength)
            .WithMessage($"El correo electrónico no puede superar {User.EmailMaxLength} caracteres.")
            .Must(email => EmailRegex().IsMatch(email!.Trim()))
            .WithMessage("El correo electrónico no tiene un formato válido.");
    }

    // Texto@dominio.extensión, sin espacios ni arrobas adicionales.
    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.CultureInvariant)]
    private static partial Regex EmailRegex();
}
