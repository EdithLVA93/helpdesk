using FluentValidation;
using HelpDesk.Application.DTOs.Tickets;
using HelpDesk.Domain.Entities;

namespace HelpDesk.Application.Validators;

internal sealed class TicketAdditionalDataDtoValidator : AbstractValidator<TicketAdditionalDataDto>
{
    public const int MaxMetadataEntries = 10;
    public const int MetadataKeyMaxLength = 50;
    public const int MetadataValueMaxLength = 200;

    public TicketAdditionalDataDtoValidator(TimeProvider timeProvider)
    {
        RuleFor(data => data.Priority)
            .Cascade(CascadeMode.Stop)
            .NotNull().WithMessage("La prioridad es obligatoria.")
            .IsInEnum().WithMessage("La prioridad debe ser Low, Medium, High o Critical.");

        RuleFor(data => data.Category)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("La categoría es obligatoria.")
            .MaximumLength(Ticket.CategoryMaxLength)
            .WithMessage($"La categoría no puede superar {Ticket.CategoryMaxLength} caracteres.");

        // Se acepta desde "ayer" en UTC: un usuario en un huso horario detrás
        // de UTC (por ejemplo UTC-5 a las 21:00) sigue estando en el día anterior.
        RuleFor(data => data.EstimatedResolutionDate)
            .Must(date => date is null || date.Value >= EarliestAllowedDate(timeProvider))
            .WithMessage("La fecha estimada de solución no puede ser anterior a hoy.");

        RuleFor(data => data.Tags)
            .Must(tags => tags is null || tags.Count <= Ticket.MaxTags)
            .WithMessage($"Se permiten como máximo {Ticket.MaxTags} etiquetas.");

        RuleForEach(data => data.Tags)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Las etiquetas no pueden estar vacías.")
            .MaximumLength(Ticket.TagMaxLength)
            .WithMessage($"Cada etiqueta puede tener como máximo {Ticket.TagMaxLength} caracteres.");

        RuleFor(data => data.Metadata)
            .Must(metadata => metadata is null || metadata.Count <= MaxMetadataEntries)
            .WithMessage($"Se permiten como máximo {MaxMetadataEntries} metadatos.");

        RuleForEach(data => data.Metadata)
            .Must(entry => !string.IsNullOrWhiteSpace(entry.Key) && entry.Key.Length <= MetadataKeyMaxLength)
            .WithMessage($"Cada clave de metadatos es obligatoria y puede tener como máximo {MetadataKeyMaxLength} caracteres.")
            .Must(entry => entry.Value is null || entry.Value.Length <= MetadataValueMaxLength)
            .WithMessage($"Cada valor de metadatos puede tener como máximo {MetadataValueMaxLength} caracteres.");
    }

    private static DateOnly EarliestAllowedDate(TimeProvider timeProvider) =>
        DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime).AddDays(-1);
}
