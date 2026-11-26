using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace HelpDesk.Application;

/// <summary>
/// Registro de los servicios de la capa de aplicación.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Registra todos los validadores (AbstractValidator<T>) de este ensamblado.
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly, includeInternalTypes: true);

        return services;
    }
}
