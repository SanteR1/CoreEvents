using System.Net.Sockets;
using System.Text.Json;
using Confluent.Kafka;
using CoreEvents.Shared.Contracts.Exceptions;
using Events.Application.Abstractions.Persistence;
using Events.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Events.Infrastructure.Data.Analyzers;

public sealed class InfrastructureExceptionAnalyzer : IExceptionAnalyzer
{
    public bool IsTransient(Exception exception)
    {
        if (exception is NonRetryableException or JsonException)
            return false;

        var rootCause = exception.GetBaseException();
        if (rootCause is NonRetryableException or JsonException)
            return false;

        // 1. Ошибки PostgreSQL
        if (rootCause is NpgsqlException { IsTransient: true })
            return true;

        if (rootCause is PostgresException pgEx && (string.Equals(pgEx.SqlState, "53300", StringComparison.Ordinal) || string.Equals(pgEx.SqlState, "08006", StringComparison.Ordinal)))
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
        for (var current = exception; current != null; current = current.InnerException)
        {
            if (current is ConcurrencyException or DbUpdateConcurrencyException)
            {
                return true;
            }

            if (current is PostgresException pgEx &&
                (string.Equals(pgEx.SqlState, "40001", StringComparison.Ordinal)
                 || string.Equals(pgEx.SqlState, "40P01", StringComparison.Ordinal)))
            {
                return true;
            }
        }

        return false;
    }
}
