using FluentValidation;
using HelpDesk.Application.Interfaces;
using HelpDesk.Application.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace HelpDesk.Application;

/// <summary>
/// Registro de los servicios de la capa de aplicación.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Reloj inyectable: en producción es el del sistema; en los tests se
        // puede sustituir por uno controlado.
        services.TryAddSingleton(TimeProvider.System);

        // Registra todos los validadores (AbstractValidator<T>) de este ensamblado.
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly, includeInternalTypes: true);

        services.AddScoped<IUserService, UserService>();
        services.AddScoped<ITicketService, TicketService>();

        return services;
    }
}
