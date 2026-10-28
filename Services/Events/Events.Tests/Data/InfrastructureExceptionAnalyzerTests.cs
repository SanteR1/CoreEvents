using AwesomeAssertions;
using Events.Domain.Exceptions;
using Events.Infrastructure.Data.Analyzers;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Xunit;

namespace Events.Tests.Data;

public class InfrastructureExceptionAnalyzerTests
{
    private readonly InfrastructureExceptionAnalyzer _analyzer = new();

    [Fact]
    public void IsConcurrency_WhenDbUpdateConcurrencyException_ShouldReturnTrue()
    {
        var ex = new DbUpdateConcurrencyException("Concurrency conflict");
        _analyzer.IsConcurrency(ex).Should().BeTrue();
    }

    [Fact]
    public void IsConcurrency_WhenDomainConcurrencyException_ShouldReturnTrue()
    {
        var ex = new ConcurrencyException("Domain concurrency conflict");
        _analyzer.IsConcurrency(ex).Should().BeTrue();
    }

    [Theory]
    [InlineData("40001")] // serialization_failure
    [InlineData("40P01")] // deadlock_detected
    public void IsConcurrency_WhenPostgresConcurrencyCodes_ShouldReturnTrue(string sqlState)
    {
        var ex = new PostgresException("Error", "ERROR", "ERROR", sqlState);
        _analyzer.IsConcurrency(ex).Should().BeTrue();
    }

    [Fact]
    public void IsConcurrency_WhenNestedInGenericException_ShouldReturnTrue()
    {
        var inner = new ConcurrencyException("Inner error");
        var outer = new InvalidOperationException("Outer wrapper", inner);

        _analyzer.IsConcurrency(outer).Should().BeTrue();
    }

    [Fact]
    public void IsConcurrency_WhenUnrelatedException_ShouldReturnFalse()
    {
        var ex = new InvalidOperationException("Normal error");
        _analyzer.IsConcurrency(ex).Should().BeFalse();
    }

    [Fact]
    public void IsConcurrency_WhenOtherPostgresError_ShouldReturnFalse()
    {
        var ex = new PostgresException("Unique violation", "ERROR", "ERROR", "23505");
        _analyzer.IsConcurrency(ex).Should().BeFalse();
    }
}
