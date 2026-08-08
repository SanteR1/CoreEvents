namespace Bookings.Application.Abstractions.Persistence;

public interface IExceptionAnalyzer
{
    bool IsTransient(Exception exception);
    bool IsConcurrency(Exception exception);
}
