using HelpDesk.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HelpDesk.Infrastructure.Data.Configurations;

/// <summary>
/// Mapeo de dbo.Users. Debe coincidir con database/01_schema.sql.
/// </summary>
internal sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");

        builder.HasKey(user => user.Id);

        builder.Property(user => user.Id)
            .UseIdentityColumn();

        builder.Property(user => user.Name)
            .HasMaxLength(User.NameMaxLength)
            .IsRequired();

        builder.Property(user => user.Email)
            .HasMaxLength(User.EmailMaxLength)
            .IsRequired();

        builder.Property(user => user.CreatedAt)
            .HasColumnType("datetime2(3)")
            .IsRequired();

        builder.HasIndex(user => user.Email)
            .IsUnique()
            .HasDatabaseName("UX_Users_Email");
    }
}
