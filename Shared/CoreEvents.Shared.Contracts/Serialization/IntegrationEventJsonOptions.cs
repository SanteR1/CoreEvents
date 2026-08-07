using System.Text.Json;
using System.Text.Json.Serialization;

namespace CoreEvents.Shared.Contracts.Serialization
{
    public static class IntegrationEventJsonOptions
    {
        public static readonly JsonSerializerOptions Default = new()
        {
            PropertyNameCaseInsensitive = true,
            Converters = { new JsonStringEnumConverter() }
        };
    }
}
