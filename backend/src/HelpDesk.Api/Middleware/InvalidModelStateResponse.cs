using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace HelpDesk.Api.Middleware;

/// <summary>
/// Respuesta para los errores que ASP.NET detecta antes de llegar al
/// controlador: JSON mal formado, cuerpo vacío, enum desconocido
/// ("status": "Closed") o parámetro de consulta inválido (?page=abc).
///
/// Devuelve el mismo formato que los errores de FluentValidation
/// (ValidationProblemDetails, campos en camelCase, mensajes en español) y
/// no expone los mensajes internos del deserializador.
/// </summary>
internal static class InvalidModelStateResponse
{
    public const string Title = "Uno o más campos no son válidos.";

    private const string BodyField = "body";

    public static IActionResult Create(ActionContext context)
    {
        var errors = new ModelStateDictionary();

        foreach (var (key, entry) in context.ModelState)
        {
            if (entry is null || entry.Errors.Count == 0)
            {
                continue;
            }

            var field = ToFieldName(key);
            var message = field == BodyField
                ? "El cuerpo de la solicitud está vacío o no es un JSON válido."
                : $"El valor de '{field}' no es válido.";

            errors.AddModelError(field, message);
        }

        var problemDetails = context.HttpContext.RequestServices
            .GetRequiredService<ProblemDetailsFactory>()
            .CreateValidationProblemDetails(context.HttpContext, errors, StatusCodes.Status400BadRequest, title: Title);

        return new BadRequestObjectResult(problemDetails)
        {
            ContentTypes = { "application/problem+json" },
        };
    }

    /// <summary>
    /// "$.additionalData.priority" → "additionalData.priority";
    /// "Status" → "status"; "" o "$" (todo el cuerpo) → "body".
    /// </summary>
    private static string ToFieldName(string key)
    {
        var name = key.StartsWith("$.", StringComparison.Ordinal) ? key[2..] : key;

        if (name.Length == 0 || name == "$")
        {
            return BodyField;
        }

        return string.Concat(char.ToLowerInvariant(name[0]).ToString(), name.AsSpan(1));
    }
}
