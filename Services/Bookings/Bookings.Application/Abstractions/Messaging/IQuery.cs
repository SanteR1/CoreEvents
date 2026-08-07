using MediatR;

namespace Bookings.Application.Abstractions.Messaging
{
    public interface IQuery<out TResponse> : IRequest<TResponse> { }
}
