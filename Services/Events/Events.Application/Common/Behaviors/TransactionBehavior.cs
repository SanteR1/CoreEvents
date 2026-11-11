using System.Data;
using Events.Application.Abstractions.Messaging;
using Events.Application.Abstractions.Persistence;
using Events.Domain.DomainEvents;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Events.Application.Common.Behaviors;

public sealed class TransactionBehavior<TRequest, TResponse>(
    IUnitOfWork unitOfWork,
    IDomainEventDispatcher dispatcher,
    ILogger<TransactionBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private const int MaxDispatchRounds = 20;

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (request is not IBaseCommand)
            return await next(cancellationToken);

        if (unitOfWork.HasActiveTransaction)
        {
            throw new InvalidOperationException(
                $"Обнаружен вложенный вызов транзакционной команды '{typeof(TRequest).Name}' при уже открытой транзакции. " +
                $"Вложенные транзакционные границы через mediator.Send() запрещены. " +
                $"Переиспользуйте доменные сущности или внутренние сервисы.");
        }

        TResponse response;
        var allEvents = new List<IDomainEvent>();

        await unitOfWork.BeginTransactionAsync(IsolationLevel.RepeatableRead, cancellationToken);
        try
        {
            response = await next(cancellationToken);
            await ExecutePreCommitPhaseAsync(allEvents, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            await unitOfWork.CommitTransactionAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            await SafeRollbackAsync(ex);
            throw;
        }

        await ExecutePostCommitPhaseAsync(allEvents);
        return response;
    }

    private async Task ExecutePreCommitPhaseAsync(List<IDomainEvent> allEvents, CancellationToken ct)
    {
        var round = 0;
        while (true)
        {
            var events = unitOfWork.CollectDomainEvents();
            if (events.Count == 0) break;

            if (round >= MaxDispatchRounds)
            {
                throw new InvalidOperationException(
                    $"Превышен лимит раундов диспетчеризации доменных событий ({MaxDispatchRounds}). " +
                    "Обнаружен вероятный бесконечный каскадный цикл событий.");
            }

            round++;
            allEvents.AddRange(events);
            foreach (var e in events)
            {
                await dispatcher.DispatchPreCommitAsync(e, ct);
            }
        }
    }

    private async Task SafeRollbackAsync(Exception ex)
    {
        try
        {
            await unitOfWork.RollbackTransactionAsync(CancellationToken.None);
        }
        catch (Exception rollbackEx)
        {
            logger.LogWarning(
                rollbackEx,
                "Сбой при откате транзакции для команды {Command}. Ошибка: {Error}",
                typeof(TRequest).Name,
                ex.Message);
        }
    }

    private async Task ExecutePostCommitPhaseAsync(List<IDomainEvent> allEvents)
    {
        foreach (var e in allEvents)
        {
            try
            {
                await dispatcher.DispatchPostCommitAsync(e, CancellationToken.None);
            }
            catch (Exception ex)
            {
                logger.LogError(
                    ex,
                    "Post-commit обработчик для события {Event} завершился с ошибкой (данные уже зафиксированы)",
                    e.GetType().Name);
            }
        }
    }
}
