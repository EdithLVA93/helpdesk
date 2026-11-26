using HelpDesk.Domain.Entities;
using HelpDesk.Infrastructure.Data.Conversions;
using Microsoft.EntityFrameworkCore;

namespace HelpDesk.Infrastructure.Data;

/// <summary>
/// Contexto de EF Core para la base de datos HelpDeskDb.
/// El esquema lo define database/01_schema.sql (sin migraciones); las
/// configuraciones de cada entidad, en Data/Configurations, lo replican.
/// </summary>
public sealed class HelpDeskDbContext(DbContextOptions<HelpDeskDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();

    public DbSet<Ticket> Tickets => Set<Ticket>();

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        ArgumentNullException.ThrowIfNull(configurationBuilder);

        // Todas las fechas se guardan en UTC. SQL Server no guarda el "Kind",
        // así que al leerlas se marcan como UTC; de lo contrario se
        // serializarían sin la "Z" y el navegador las tomaría como hora local.
        configurationBuilder.Properties<DateTime>().HaveConversion<UtcDateTimeConverter>();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Aplica todas las clases IEntityTypeConfiguration<T> de este ensamblado.
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(HelpDeskDbContext).Assembly);
    }
}
