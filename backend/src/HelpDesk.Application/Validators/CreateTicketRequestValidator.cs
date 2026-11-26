using FluentValidation;
using HelpDesk.Application.DTOs.Tickets;
using HelpDesk.Domain.Entities;

namespace HelpDesk.Application.Validators;

/// <summary>
/// Valida el formato de la petición. Que el usuario asignado exista se
/// comprueba en TicketService, porque requiere consultar la base de datos.
/// </summary>
internal sealed class CreateTicketRequestValidator : AbstractValidator<CreateTicketRequest>
{
    public CreateTicketRequestValidator(TimeProvider timeProvider)
    {
        RuleFor(request => request.Title)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("El título del ticket es obligatorio.")
            .MaximumLength(Ticket.TitleMaxLength)
            .WithMessage($"El título no puede superar {Ticket.TitleMaxLength} caracteres.");

        RuleFor(request => request.Description)
            .MaximumLength(Ticket.DescriptionMaxLength)
            .WithMessage($"La descripción no puede superar {Ticket.DescriptionMaxLength} caracteres.");

        RuleFor(request => request.AssignedUserId)
            .GreaterThan(0).WithMessage("El ticket debe tener un usuario asignado.");

        RuleFor(request => request.AdditionalData)
            .NotNull().WithMessage("La información adicional (prioridad y categoría) es obligatoria.")
            .SetValidator(new TicketAdditionalDataDtoValidator(timeProvider)!);
    }
}
