using Events.Application.Common.Behaviors;
using Events.Infrastructure.Resilience.Behaviors;
using MediatR;

namespace Microsoft.Extensions.DependencyInjection;

public static class MediatRExtensions
{
    public static IServiceCollection AddEventsCqrsPipeline(this IServiceCollection services)
    {
        // Строгий порядок конвейера MediatR (луковичная архитектура / Onion):
        // 1. Resilience (Polly) — внешний контур, перехватывает сбои (deadlock, transient) и перезапускает всю цепочку с чистой транзакцией
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(CqrsResilienceBehavior<,>));

        // 2. Transaction — открывает транзакцию UnitOfWork и координирует pre/post commit события
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(TransactionBehavior<,>));

        // 3. Inbox — проверка идемпотентности и регистрация сообщения в БД в рамках открытой транзакции
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(InboxBehavior<,>));

        return services;
    }
}
