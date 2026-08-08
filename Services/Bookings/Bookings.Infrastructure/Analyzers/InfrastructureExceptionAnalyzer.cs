using System.Net.Sockets;
using Bookings.Application.Abstractions.Persistence;
using Confluent.Kafka;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Bookings.Infrastructure.Analyzers;

public sealed class InfrastructureExceptionAnalyzer : IExceptionAnalyzer
{
    public bool IsTransient(Exception exception)
    {
        var rootCause = exception.GetBaseException();

        // 1. Ошибки PostgreSQL
        if (rootCause is NpgsqlException { IsTransient: true })
            return true;

        if (rootCause is PostgresException pgEx && (pgEx.SqlState == "53300" || pgEx.SqlState == "08006"))
            return true;

        // 2. Ошибки Kafka (При публикации сообщений)
        if (rootCause is KafkaException kafkaEx)
        {
            // У Kafka есть свой флаг IsFatal (например, если брокер вернул ошибку авторизации).
            // Если ошибка не фатальная (например, брокер временно недоступен или Local_QueueFull),
            // значит она транзитная.
            // Если ошибка не фатальная и не локальная (например, не проблема с памятью/размером сообщения),
            // то это временная проблема сети или брокера.
            if (!kafkaEx.Error.IsFatal && !kafkaEx.Error.IsLocalError)
                return true;

            // Можно проверять конкретные коды, если нужно:
            // if (kafkaEx.Error.Code == ErrorCode.Local_Transport) return true;
        }

        // 3. Общие сетевые ошибки (.NET)
        if (rootCause is SocketException or TimeoutException or HttpRequestException)
            return true;

        return false;
    }

    public bool IsConcurrency(Exception exception)
    {
        return exception is DbUpdateConcurrencyException
               || exception.GetBaseException() is DbUpdateConcurrencyException;
    }
}
