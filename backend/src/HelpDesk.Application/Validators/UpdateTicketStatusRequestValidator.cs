using FluentValidation;
using HelpDesk.Application.DTOs.Tickets;

namespace HelpDesk.Application.Validators;

/// <summary>
/// Valida que llegue un estado reconocido. Si la transición está permitida o
/// no lo decide el dominio (TicketStatusTransitionPolicy), no el validador.
/// </summary>
internal sealed class UpdateTicketStatusRequestValidator : AbstractValidator<UpdateTicketStatusRequest>
{
    public UpdateTicketStatusRequestValidator()
    {
        RuleFor(request => request.Status)
            .Cascade(CascadeMode.Stop)
            .NotNull().WithMessage("El estado es obligatorio.")
            .IsInEnum().WithMessage("El estado debe ser Open, InProgress o Resolved.");
    }
}
