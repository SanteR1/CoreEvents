using System.Net.Sockets;
using Confluent.Kafka;
using Events.Application.Abstractions.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Events.Infrastructure.Data.Analyzers;

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
        if (rootCause is KafkaException kafkaEx && !kafkaEx.Error.IsFatal && !kafkaEx.Error.IsLocalError)
        {
            return true;
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
