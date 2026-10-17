using System.Text.Json;
using System.Text.Json.Serialization;

namespace RedAnts.Show.Features.Music;

public static class DJJson
{
    public static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter() },
        WriteIndented = true,
    };
}
