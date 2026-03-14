namespace HealthTracker.Infrastructure.Json;

using System.Text.Json;
using System.Text.Json.Serialization;

internal static class JsonConfig
{
    internal static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        WriteIndented = true,
        // DateOnly is natively supported in .NET 7+ System.Text.Json, so no custom converter needed.
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseLower) }
    };
}
