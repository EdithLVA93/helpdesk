namespace HelpDesk.Domain.Entities;

/// <summary>
/// Usuario que puede ser responsable de tickets.
/// Las propiedades solo se modifican desde la propia entidad: el resto del
/// código crea usuarios con <see cref="Create"/>.
/// </summary>
public sealed class User
{
    public const int NameMaxLength = 100;
    public const int EmailMaxLength = 254;

    // Constructor para EF Core.
    private User()
    {
    }

    public int Id { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public string Email { get; private set; } = string.Empty;

    public DateTime CreatedAt { get; private set; }

    public static User Create(string name, string email, DateTime createdAtUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(email);

        return new User
        {
            Name = name.Trim(),
            Email = NormalizeEmail(email),
            CreatedAt = createdAtUtc,
        };
    }

    /// <summary>
    /// Forma canónica del correo: sin espacios y en minúsculas.
    /// Se usa al crear el usuario y al comprobar si el correo ya existe.
    /// </summary>
    public static string NormalizeEmail(string email)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(email);

#pragma warning disable CA1308 // Los correos se normalizan a minúsculas por convención, no para comparar.
        return email.Trim().ToLowerInvariant();
#pragma warning restore CA1308
    }
}
