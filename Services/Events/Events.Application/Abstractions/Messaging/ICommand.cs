using MediatR;

namespace Events.Application.Abstractions.Messaging;

public interface ICommand<out TResponse> : IRequest<TResponse> { }
public interface ICommand : IRequest<Unit> { } // Для команд без возвращаемого значения
