using FluentValidation;
using HelpDesk.Application.DTOs.Tickets;
using HelpDesk.Domain.Entities;

namespace HelpDesk.Application.Validators;

internal sealed class TicketQueryParametersValidator : AbstractValidator<TicketQueryParameters>
{
    public TicketQueryParametersValidator()
    {
        RuleFor(query => query.Status)
            .IsInEnum().WithMessage("El estado debe ser Open, InProgress o Resolved.");

        RuleFor(query => query.Priority)
            .IsInEnum().WithMessage("La prioridad debe ser Low, Medium, High o Critical.");

        RuleFor(query => query.SortDirection)
            .IsInEnum().WithMessage("El orden debe ser Descending o Ascending.");

        RuleFor(query => query.AssignedUserId)
            .GreaterThan(0).When(query => query.AssignedUserId.HasValue)
            .WithMessage("El id de usuario debe ser mayor que 0.");

        RuleFor(query => query.Category)
            .MaximumLength(Ticket.CategoryMaxLength)
            .WithMessage($"La categoría no puede superar {Ticket.CategoryMaxLength} caracteres.");

        RuleFor(query => query.Tag)
            .MaximumLength(Ticket.TagMaxLength)
            .WithMessage($"La etiqueta no puede superar {Ticket.TagMaxLength} caracteres.");

        RuleFor(query => query.Page)
            .GreaterThanOrEqualTo(1).WithMessage("La página debe ser 1 o mayor.");

        RuleFor(query => query.PageSize)
            .InclusiveBetween(1, TicketQueryParameters.MaxPageSize)
            .WithMessage($"El tamaño de página debe estar entre 1 y {TicketQueryParameters.MaxPageSize}.");
    }
}
