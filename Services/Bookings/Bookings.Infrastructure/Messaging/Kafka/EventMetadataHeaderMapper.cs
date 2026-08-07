using System.Text;
using System.Text.Json;
using Confluent.Kafka;
using CoreEvents.Shared.Contracts.Events;
using CoreEvents.Shared.Contracts.Serialization;

namespace Bookings.Infrastructure.Messaging.Kafka
{
    internal static class EventMetadataHeaderMapper
    {
        public static Dictionary<string, string> ToHeaderDictionary(EventMetadata metadata)
        {
            var dict = new Dictionary<string, string>
            {
                ["messageId"] = metadata.MessageId.ToString(),
                ["correlationId"] = metadata.CorrelationId.ToString(),
                ["eventType"] = metadata.EventType,
                ["createdAt"] = metadata.CreatedAt.ToString("O")
            };

            if (metadata.CausationId is { } causationId)
            {
                dict["causationId"] = causationId.ToString();
            }

            return dict;
        }

        public static EventMetadata ParseMetadata(Headers headers) => new()
        {
            MessageId = Guid.Parse(GetHeader(headers, "messageId")),
            CorrelationId = Guid.Parse(GetHeader(headers, "correlationId")),
            CausationId = TryGetHeader(headers, "causationId", out var c) ? Guid.Parse(c) : null,
            CreatedAt = DateTimeOffset.Parse(GetHeader(headers, "createdAt")),
            EventType = GetHeader(headers, "eventType")
        };

        // Полный дамп всех headers как есть — для хранения в Inbox/аудита,
        // в отличие от ParseMetadata, который вычленяет только известные 5 полей.
        public static string SerializeHeaders(Headers headers)
        {
            var dict = headers.ToDictionary(
                h => h.Key,
                h => Encoding.UTF8.GetString(h.GetValueBytes()));
            return JsonSerializer.Serialize(dict, IntegrationEventJsonOptions.Default);
        }

        private static string GetHeader(Headers headers, string key) =>
            Encoding.UTF8.GetString(headers.GetLastBytes(key));

        private static bool TryGetHeader(Headers headers, string key, out string value)
        {
            if (headers.TryGetLastBytes(key, out var bytes))
            {
                value = Encoding.UTF8.GetString(bytes);
                return true;
            }
            value = null!;
            return false;
        }
    }
}
