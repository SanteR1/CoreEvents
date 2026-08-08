using MediatR;

namespace Events.Application.Abstractions.Messaging;

public interface IQuery<out TResponse> : IRequest<TResponse> { }
