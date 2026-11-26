using HelpDesk.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HelpDesk.Infrastructure.Data.Configurations;

/// <summary>
/// Mapeo de dbo.Tickets. Debe coincidir con database/01_schema.sql.
/// </summary>
internal sealed class TicketConfiguration : IEntityTypeConfiguration<Ticket>
{
    public void Configure(EntityTypeBuilder<Ticket> builder)
    {
        builder.ToTable("Tickets");

        builder.HasKey(ticket => ticket.Id);

        builder.Property(ticket => ticket.Id)
            .UseIdentityColumn();

        builder.Property(ticket => ticket.Title)
            .HasMaxLength(Ticket.TitleMaxLength)
            .IsRequired();

        builder.Property(ticket => ticket.Description)
            .HasMaxLength(Ticket.DescriptionMaxLength);

        // Enum guardado como texto ('Open', 'InProgress', 'Resolved').
        builder.Property(ticket => ticket.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(ticket => ticket.CreatedAt)
            .HasColumnType("datetime2(3)")
            .IsRequired();

        builder.Property(ticket => ticket.UpdatedAt)
            .HasColumnType("datetime2(3)");

        builder.Property(ticket => ticket.AdditionalData)
            .HasColumnType("nvarchar(max)")
            .IsRequired();

        // Columnas calculadas con JSON_VALUE: EF Core las lee (también después
        // de un INSERT o UPDATE) pero nunca intenta escribirlas.
        builder.Property(ticket => ticket.Priority)
            .HasConversion<string>()
            .HasMaxLength(20)
            .HasComputedColumnSql("CAST(JSON_VALUE([AdditionalData], N'$.priority') AS NVARCHAR(20))");

        builder.Property(ticket => ticket.Category)
            .HasMaxLength(Ticket.CategoryMaxLength)
            .HasComputedColumnSql("CAST(JSON_VALUE([AdditionalData], N'$.category') AS NVARCHAR(50))");

        builder.HasOne(ticket => ticket.AssignedUser)
            .WithMany()
            .HasForeignKey(ticket => ticket.AssignedUserId)
            .OnDelete(DeleteBehavior.NoAction)
            .HasConstraintName("FK_Tickets_Users_AssignedUserId");
    }
}
