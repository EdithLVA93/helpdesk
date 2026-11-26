namespace HelpDesk.UnitTests.TestDoubles;

/// <summary>
/// Reloj fijo: hace deterministas las pruebas que dependen de la fecha actual
/// (fecha de creación, fecha estimada "no anterior a hoy").
/// </summary>
internal sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
{
    public static readonly DateTimeOffset DefaultNow = new(2026, 11, 25, 15, 30, 45, 123, TimeSpan.Zero);

    public FixedTimeProvider()
        : this(DefaultNow)
    {
    }

    public override DateTimeOffset GetUtcNow() => utcNow;
}
