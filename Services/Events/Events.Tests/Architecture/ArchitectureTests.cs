using System.Reflection;
using AwesomeAssertions;
using Events.Application.Abstractions.Messaging;
using Events.Domain.DomainEvents;
using Events.Domain.Entities;
using MediatR;
using Xunit;

namespace Events.Tests.Architecture;

public class ArchitectureTests
{
    private static readonly Assembly ApplicationAssembly = typeof(IInboxCommand).Assembly;
    private static readonly Assembly DomainAssembly = typeof(IDomainEvent).Assembly;

    [Fact]
    public void CommandHandlers_ShouldNotInjectMediatorOrSender_ToPreventNestedTransactions()
    {
        // Arrange
        var forbiddenTypes = new HashSet<Type>
        {
            typeof(IMediator),
            typeof(ISender),
            typeof(IPublisher)
        };

        var handlerTypes = ApplicationAssembly.GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false })
            .Where(t => t.GetInterfaces().Any(i =>
                i.IsGenericType && (
                    i.GetGenericTypeDefinition() == typeof(IRequestHandler<,>) ||
                    i.GetGenericTypeDefinition() == typeof(IRequestHandler<>))))
            .ToList();

        // Act & Assert
        foreach (var handler in handlerTypes)
        {
            var constructors = handler.GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            foreach (var ctor in constructors)
            {
                var parameters = ctor.GetParameters();
                foreach (var param in parameters)
                {
                    forbiddenTypes.Should().NotContain(
                        param.ParameterType,
                        $"Хендлер '{handler.FullName}' не должен внедрять '{param.ParameterType.Name}' в конструктор, " +
                        "так как вложенные команды MediatR нарушают инвариант единой транзакционной границы (TransactionBehavior).");
                }
            }
        }
    }

    [Fact]
    public void DomainAssembly_ShouldBePure_AndNotDependOnExternalFrameworks()
    {
        // Arrange
        var forbiddenAssemblyPrefixes = new[]
        {
            "MediatR",
            "Microsoft.EntityFrameworkCore",
            "StackExchange.Redis",
            "Confluent.Kafka",
            "Microsoft.Extensions.DependencyInjection",
            "Microsoft.AspNetCore"
        };

        var referencedAssemblies = DomainAssembly.GetReferencedAssemblies();

        // Act & Assert
        foreach (var referenced in referencedAssemblies)
        {
            foreach (var prefix in forbiddenAssemblyPrefixes)
            {
                referenced.Name.Should().NotStartWith(
                    prefix,
                    $"Доменный слой Events.Domain обязан оставаться 100% чистым и не зависеть от '{referenced.Name}'.");
            }
        }
    }

    [Fact]
    public void DomainEvents_ShouldImplementPureIDomainEvent_AndHaveOccurredAt()
    {
        // Arrange
        var domainEventTypes = DomainAssembly.GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false } && string.Equals(t.Namespace, typeof(IDomainEvent).Namespace, StringComparison.Ordinal))
            .ToList();

        domainEventTypes.Should().NotBeEmpty("В домене должны быть определены доменные события.");

        // Act & Assert
        foreach (var eventType in domainEventTypes)
        {
            typeof(IDomainEvent).IsAssignableFrom(eventType)
                .Should().BeTrue($"Событие '{eventType.FullName}' обязано реализовывать интерфейс IDomainEvent.");

            eventType.IsSealed
                .Should().BeTrue($"Событие '{eventType.FullName}' должно быть объявлено как sealed.");

            var occurredAtProp = eventType.GetProperty(nameof(IDomainEvent.OccurredAt));
            occurredAtProp.Should().NotBeNull($"Событие '{eventType.FullName}' должно содержать свойство OccurredAt.");
            occurredAtProp.PropertyType.Should().Be<DateTimeOffset>();
        }

        // Проверяем, что устаревшие интерфейсы удалены
        DomainAssembly.GetTypes().Select(t => t.Name).Should().NotContain("ICacheInvalidationEvent");
        DomainAssembly.GetTypes().Select(t => t.Name).Should().NotContain("IPreCommitDomainEvent");
        DomainAssembly.GetTypes().Select(t => t.Name).Should().NotContain("IPostCommitDomainEvent");
    }

    [Fact]
    public void InboxCommands_MustHaveInboxProcessingResultAsResponseType()
    {
        // Arrange
        var inboxCommandTypes = ApplicationAssembly.GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false })
            .Where(t => typeof(IInboxCommand).IsAssignableFrom(t))
            .ToList();

        inboxCommandTypes.Should().NotBeEmpty("В слое Application должны быть определены команды IInboxCommand.");

        // Act & Assert
        foreach (var cmdType in inboxCommandTypes)
        {
            var requestInterface = cmdType.GetInterfaces()
                .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IRequest<>));

            requestInterface.Should().NotBeNull(
                $"Команда '{cmdType.FullName}' реализует IInboxCommand и обязана реализовывать IRequest<InboxProcessingResult>.");

            requestInterface.GetGenericArguments()[0].Should().Be<InboxProcessingResult>(
                $"Команда '{cmdType.FullName}' обязана возвращать InboxProcessingResult.");
        }
    }

    [Fact]
    public void DomainEventHandlers_MustNotImplementMediatRNotificationHandler()
    {
        // Arrange
        var appTypes = ApplicationAssembly.GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false })
            .ToList();

        // Act & Assert
        foreach (var type in appTypes)
        {
            var notificationHandlerInterface = type.GetInterfaces()
                .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(INotificationHandler<>));

            notificationHandlerInterface.Should().BeNull(
                $"Класс '{type.FullName}' не должен реализовывать INotificationHandler<>. " +
                "Все реакции на доменные события разделены на двухфазные интерфейсы IPreCommitHandler<T> и IPostCommitHandler<T>.");
        }
    }
}
