using System.Text.Json;
using System.Text.Json.Serialization;

namespace RedAnts.Show.Domain;

public static class ShowJson
{
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public static readonly JsonSerializerOptions Pretty = new(Options) { WriteIndented = true };
}
