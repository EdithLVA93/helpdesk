using FluentValidation;
using HelpDesk.Application.DTOs.Tickets;

namespace HelpDesk.Application.Validators;

internal sealed class UpdateTicketPriorityRequestValidator : AbstractValidator<UpdateTicketPriorityRequest>
{
    public UpdateTicketPriorityRequestValidator()
    {
        RuleFor(request => request.Priority)
            .Cascade(CascadeMode.Stop)
            .NotNull().WithMessage("La prioridad es obligatoria.")
            .IsInEnum().WithMessage("La prioridad debe ser Low, Medium, High o Critical.");
    }
}
