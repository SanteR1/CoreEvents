namespace Events.Application.Abstractions.Messaging;

public interface IInboxCommand : ICommand<InboxProcessingResult>
{
    InboxEnvelope Inbox { get; }
}
