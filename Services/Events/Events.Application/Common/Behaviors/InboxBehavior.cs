using Events.Application.Abstractions.Messaging;
using Events.Application.Abstractions.Persistence;
using MediatR;

namespace Events.Application.Common.Behaviors;

public sealed class InboxBehavior<TRequest, TResponse>(
    IInboxRepository inboxRepository)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        // Не-инбокс команды и запросы на чтение проходят мимо без накладных расходов
        if (request is not IInboxCommand inboxCommand)
            return await next(cancellationToken);

        if (typeof(TResponse) != typeof(InboxProcessingResult))
            throw new InvalidOperationException(
                $"Недопустимый тип ответа '{typeof(TResponse).Name}' для команды Inbox '{typeof(TRequest).Name}'. " +
                $"Все IInboxCommand обязаны возвращать {nameof(InboxProcessingResult)}.");

        var acquired = await inboxRepository.TryRegisterAsync(
            inboxCommand.Inbox,
            cancellationToken);

        if (!acquired)
            return (TResponse)(object)InboxProcessingResult.AlreadyProcessed;

        var response = await next(cancellationToken);

        if (response is not InboxProcessingResult.Processed)
            throw new InvalidOperationException(
                $"Обработчик команды '{typeof(TRequest).Name}' вернул '{response}'. " +
                $"Все успешные обработчики IInboxCommand обязаны возвращать {nameof(InboxProcessingResult.Processed)}.");

        return response;
    }
}
