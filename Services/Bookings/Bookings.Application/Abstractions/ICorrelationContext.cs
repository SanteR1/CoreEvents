namespace Bookings.Application.Abstractions
{
    public interface ICorrelationContext
    {
        Guid CorrelationId { get; }
        Guid? CausationId { get; }
        void SetCorrelationId(Guid correlationId);
        void SetCausationId(Guid? causationId);
    }
}
