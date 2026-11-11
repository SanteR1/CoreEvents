using MediatR;

namespace Events.Application.Abstractions.Messaging;

public interface IBaseCommand { }
public interface ICommand<out TResponse> : IRequest<TResponse>, IBaseCommand { }
public interface ICommand : IRequest<Unit>, IBaseCommand { }
