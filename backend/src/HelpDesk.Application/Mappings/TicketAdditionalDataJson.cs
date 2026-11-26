using System.Text.Json;
using System.Text.Json.Serialization;
using HelpDesk.Application.DTOs.Tickets;

namespace HelpDesk.Application.Mappings;

/// <summary>
/// Convierte <see cref="TicketAdditionalDataDto"/> al JSON que se guarda en
/// Tickets.AdditionalData y viceversa. Las propiedades se escriben en camelCase
/// ("priority", "category"...), que es lo que leen JSON_VALUE y OPENJSON en SQL.
/// </summary>
internal static class TicketAdditionalDataJson
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter() },
    };

    public static string Serialize(TicketAdditionalDataDto data)
    {
        ArgumentNullException.ThrowIfNull(data);

        return JsonSerializer.Serialize(data, Options);
    }

    /// <summary>
    /// Lee el JSON de la base de datos. Devuelve listas vacías en lugar de null
    /// para que el cliente no tenga que comprobarlo.
    /// </summary>
    public static TicketAdditionalDataDto Deserialize(string json)
    {
        var data = string.IsNullOrWhiteSpace(json)
            ? null
            : JsonSerializer.Deserialize<TicketAdditionalDataDto>(json, Options);

        data ??= new TicketAdditionalDataDto(null, null, null, null, null);

        return data with
        {
            Tags = data.Tags ?? [],
            Metadata = data.Metadata ?? new Dictionary<string, string>(),
        };
    }
}
