using System.Text.Json;
using System.Text.Json.Serialization;

namespace Tracking.Contracts;

public static class JsonDefaults
{
    public static JsonSerializerOptions Options { get; } = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };
}