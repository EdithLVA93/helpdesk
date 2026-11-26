using Microsoft.EntityFrameworkCore;

namespace HelpDesk.Infrastructure.Data;

/// <summary>
/// Contexto de EF Core para la base de datos HelpDeskDb.
/// El esquema lo define database/01_schema.sql (sin migraciones); las
/// configuraciones de cada entidad, en Data/Configurations, lo replican.
/// </summary>
public sealed class HelpDeskDbContext(DbContextOptions<HelpDeskDbContext> options) : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Aplica todas las clases IEntityTypeConfiguration<T> de este ensamblado.
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(HelpDeskDbContext).Assembly);
    }
}
