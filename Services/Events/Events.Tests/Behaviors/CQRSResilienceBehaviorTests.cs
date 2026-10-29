using AwesomeAssertions;
using Events.Application.Abstractions.Messaging;
using Events.Application.Abstractions.Resilience.Attributes;
using Events.Application.Abstractions.Resilience.Constants;
using Events.Infrastructure.Resilience.Behaviors;
using MediatR;
using Polly;
using Polly.Registry;
using Xunit;

namespace Events.Tests.Behaviors;

public sealed class CQRSResilienceBehaviorTests : IDisposable
{
    private readonly ResiliencePipelineRegistry<string> _pipelineRegistry = new();

    private CqrsResilienceBehavior<TRequest, TResponse> CreateBehavior<TRequest, TResponse>() where TRequest : notnull =>
        new(_pipelineRegistry);

    [Fact]
    public async Task Handle_WhenCommandImplementsIInboxCommand_ShouldAutomaticallyUseCommandConcurrencyPipeline()
    {
        // Arrange
        var pipelineExecuted = false;
        _pipelineRegistry.TryAddBuilder(ResiliencePipelines.CommandConcurrency, (builder, _) =>
        {
            builder.AddStrategy(_ => new TestResilienceStrategy(() => pipelineExecuted = true));
        });

        var behavior = CreateBehavior<TestAutoResilienceInboxCommand, InboxProcessingResult>();
        var command = new TestAutoResilienceInboxCommand(CreateDefaultEnvelope());

        var nextCalled = false;
        RequestHandlerDelegate<InboxProcessingResult> next = _ =>
        {
            nextCalled = true;
            return Task.FromResult(InboxProcessingResult.Processed);
        };

        // Act
        var result = await behavior.Handle(command, next, CancellationToken.None);

        // Assert
        result.Should().Be(InboxProcessingResult.Processed);
        nextCalled.Should().BeTrue();
        pipelineExecuted.Should().BeTrue("Все команды IInboxCommand обязаны автоматически защищаться пайплайном CommandConcurrency");
    }

    [Fact]
    public async Task Handle_WhenCommandHasExplicitAttribute_ShouldUseSpecifiedPipelineKey()
    {
        // Arrange
        var customPipelineExecuted = false;
        const string customKey = "CustomTestPipeline";

        _pipelineRegistry.TryAddBuilder(customKey, (builder, _) =>
        {
            builder.AddStrategy(_ => new TestResilienceStrategy(() => customPipelineExecuted = true));
        });

        var behavior = CreateBehavior<TestExplicitResilienceCommand, string>();
        var command = new TestExplicitResilienceCommand();

        // Act
        var result = await behavior.Handle(command, _ => Task.FromResult("ok"), CancellationToken.None);

        // Assert
        result.Should().Be("ok");
        customPipelineExecuted.Should().BeTrue("Команда с явным атрибутом должна использовать указанный ключ пайплайна");
    }

    [Fact]
    public async Task Handle_WhenCommandHasNoAttributes_AndIsNotInboxCommand_ShouldBypassPipelines()
    {
        // Arrange
        var behavior = CreateBehavior<TestUnprotectedCommand, string>();
        var command = new TestUnprotectedCommand();

        var nextCalled = false;
        RequestHandlerDelegate<string> next = _ =>
        {
            nextCalled = true;
            return Task.FromResult("direct");
        };

        // Act
        var result = await behavior.Handle(command, next, CancellationToken.None);

        // Assert
        result.Should().Be("direct");
        nextCalled.Should().BeTrue();
    }

    public void Dispose()
    {
        _pipelineRegistry.Dispose();
    }

    private static InboxEnvelope CreateDefaultEnvelope() => new(
        Guid.NewGuid(), Guid.NewGuid(), null, "consumer", "topic", 0, 1,
        "key", "type", "{}", "{}", DateTimeOffset.UtcNow);

    public sealed record TestAutoResilienceInboxCommand(InboxEnvelope Inbox) : IInboxCommand;

    [ResiliencePipeline("CustomTestPipeline")]
    public sealed record TestExplicitResilienceCommand : ICommand<string>;

    public sealed record TestUnprotectedCommand : ICommand<string>;

    private sealed class TestResilienceStrategy(Action onExecute) : Polly.ResilienceStrategy
    {
        protected override ValueTask<Outcome<TResult>> ExecuteCore<TResult, TState>(
            Func<ResilienceContext, TState, ValueTask<Outcome<TResult>>> callback,
            ResilienceContext context,
            TState state)
        {
            onExecute();
            return callback(context, state);
        }
    }
}
