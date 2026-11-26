using FluentValidation;
using HelpDesk.Application.Exceptions;
using HelpDesk.Domain.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace HelpDesk.Api.Middleware;

/// <summary>
/// Manejo centralizado de errores. Traduce cada tipo de excepción a una
/// respuesta ProblemDetails (RFC 9457) con el código HTTP correspondiente:
///
///   ValidationException (FluentValidation) → 400 con el detalle por campo
///   NotFoundException                      → 404
///   ConflictException                      → 409 (dato duplicado)
///   DomainException                        → 409 (regla de negocio, p. ej. Open → Resolved)
///   BadHttpRequestException                → su código (normalmente 400)
///   cualquier otra                         → 500, sin exponer detalles internos
///
/// Así los controladores y servicios no tienen bloques try/catch.
/// </summary>
internal sealed partial class GlobalExceptionHandler(
    IProblemDetailsService problemDetailsService,
    ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var problemDetails = exception switch
        {
            ValidationException validationException => CreateValidationProblem(validationException),
            NotFoundException => Create(StatusCodes.Status404NotFound, "Recurso no encontrado", exception.Message),
            ConflictException => Create(StatusCodes.Status409Conflict, "Conflicto con datos existentes", exception.Message),
            DomainException => Create(StatusCodes.Status409Conflict, "Regla de negocio incumplida", exception.Message),
            BadHttpRequestException badRequest => Create(badRequest.StatusCode, "Solicitud inválida", badRequest.Message),
            _ => null,
        };

        if (problemDetails is null)
        {
            LogUnhandledException(logger, exception, httpContext.Request.Method, httpContext.Request.Path.Value);

            problemDetails = Create(
                StatusCodes.Status500InternalServerError,
                "Error interno del servidor",
                "Ocurrió un error inesperado. Intente nuevamente más tarde.");
        }

        httpContext.Response.StatusCode = problemDetails.Status!.Value;

        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problemDetails,
            Exception = exception,
        });
    }

    private static ProblemDetails Create(int status, string title, string detail) =>
        new()
        {
            Status = status,
            Title = title,
            Detail = detail,
        };

    /// <summary>
    /// Agrupa los errores por campo, con el nombre en camelCase como en el JSON
    /// de la petición: "AdditionalData.Priority" → "additionalData.priority".
    /// </summary>
    private static ValidationProblemDetails CreateValidationProblem(ValidationException exception)
    {
        var errors = exception.Errors
            .GroupBy(failure => ToCamelCasePath(failure.PropertyName))
            .ToDictionary(
                group => group.Key,
                group => group.Select(failure => failure.ErrorMessage).Distinct().ToArray());

        return new ValidationProblemDetails(errors)
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "Uno o más campos no son válidos.",
        };
    }

    private static string ToCamelCasePath(string propertyPath) =>
        string.Join('.', propertyPath
            .Split('.')
            .Select(segment => segment.Length == 0
                ? segment
                : string.Concat(char.ToLowerInvariant(segment[0]).ToString(), segment.AsSpan(1))));

    [LoggerMessage(EventId = 1, Level = LogLevel.Error, Message = "Excepción no controlada en {Method} {Path}")]
    private static partial void LogUnhandledException(ILogger logger, Exception exception, string method, string? path);
}
