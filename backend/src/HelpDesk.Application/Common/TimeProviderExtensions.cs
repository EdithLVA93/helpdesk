namespace HelpDesk.Application.Common;

internal static class TimeProviderExtensions
{
    /// <summary>
    /// Fecha y hora UTC actual truncada a milisegundos. Las columnas son
    /// DATETIME2(3): así el valor que devuelve la API es exactamente el guardado.
    /// </summary>
    public static DateTime GetUtcNowTruncatedToMilliseconds(this TimeProvider timeProvider)
    {
        var ticks = timeProvider.GetUtcNow().UtcTicks;

        return new DateTime(ticks - (ticks % TimeSpan.TicksPerMillisecond), DateTimeKind.Utc);
    }
}
