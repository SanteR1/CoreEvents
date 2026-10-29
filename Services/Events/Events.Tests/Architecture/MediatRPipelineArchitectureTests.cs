using AwesomeAssertions;
using Events.Application.Common.Behaviors;
using Events.Infrastructure.Extensions;
using Events.Infrastructure.Resilience.Behaviors;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Events.Tests.Architecture;

public class MediatRPipelineArchitectureTests
{
    [Fact]
    public void AddEventsCqrsPipeline_ShouldRegisterBehaviorsInStrictOrder_WithoutDuplicates()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddEventsCqrsPipeline();

        // Assert
        var pipelineBehaviors = services
            .Where(sd => sd.ServiceType == typeof(IPipelineBehavior<,>))
            .ToList();

        pipelineBehaviors.Should().HaveCount(3, "Конвейер MediatR должен содержать ровно 3 поведения");

        // Проверяем отсутствие дубликатов типов реализаций
        var implementationTypes = pipelineBehaviors.Select(sd => sd.ImplementationType).ToList();
        implementationTypes.Distinct().Should().HaveCount(3, "Ни одно поведение не должно дублироваться в DI");

        // Проверяем строгий порядок (луковичная архитектура: Resilience -> Transaction -> Inbox)
        implementationTypes[0].Should().Be(typeof(CqrsResilienceBehavior<,>),
            "1-м внешним поведением должен быть CqrsResilienceBehavior (Polly), чтобы оборачивать транзакции снаружи");
        implementationTypes[1].Should().Be(typeof(TransactionBehavior<,>),
            "2-м поведением должен быть TransactionBehavior (UnitOfWork транзакционная граница)");
        implementationTypes[2].Should().Be(typeof(InboxBehavior<,>),
            "3-м внутренним поведением должен быть InboxBehavior, чтобы регистрация в Inbox выполнялась внутри транзакции");
    }

    [Fact]
    public void InfrastructureResilienceExtensions_ShouldNotDirectlyRegisterMediatRBehaviors()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act: регистрируем только пайплайны устойчивости
        services.AddResiliencePipelines(typeof(CqrsResilienceBehavior<,>).Assembly);

        // Assert
        var behaviors = services.Where(sd => sd.ServiceType == typeof(IPipelineBehavior<,>)).ToList();
        behaviors.Should().BeEmpty(
            "AddResiliencePipelines не должен самостоятельно регистрировать IPipelineBehavior, " +
            "так как порядок конвейера настраивается исключительно в Composition Root (AddEventsCqrsPipeline).");
    }
}
