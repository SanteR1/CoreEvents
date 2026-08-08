using Bookings.Application.Abstractions;

namespace Bookings.Infrastructure.Tracing;

public class CorrelationContext : ICorrelationContext
{
    private static readonly AsyncLocal<Guid> _correlationId = new();
    private static readonly AsyncLocal<Guid?> _causationId = new();

    public Guid CorrelationId => _correlationId.Value == Guid.Empty
        ? Guid.NewGuid()
        : _correlationId.Value;

    public Guid? CausationId => _causationId.Value;

    public void SetCorrelationId(Guid correlationId)
    {
        _correlationId.Value = correlationId;
    }

    public void SetCausationId(Guid? causationId)
    {
        _causationId.Value = causationId;
    }
}
