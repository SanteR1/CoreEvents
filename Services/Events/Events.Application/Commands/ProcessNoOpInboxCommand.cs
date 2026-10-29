using Events.Application.Abstractions.Messaging;
using MediatR;

namespace Events.Application.Commands;

public record ProcessNoOpInboxCommand(InboxEnvelope Inbox) : IInboxCommand;

internal sealed class ProcessNoOpInboxHandler : IRequestHandler<ProcessNoOpInboxCommand, InboxProcessingResult>
{
    public Task<InboxProcessingResult> Handle(ProcessNoOpInboxCommand request, CancellationToken cancellationToken)
    {
        return Task.FromResult(InboxProcessingResult.Processed);
    }
}
