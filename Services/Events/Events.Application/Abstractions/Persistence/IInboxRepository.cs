using Events.Application.Abstractions.Messaging;

namespace Events.Application.Abstractions.Persistence;

public interface IInboxRepository
{
    Task<bool> TryRegisterAsync(InboxEnvelope envelope, CancellationToken ct);
}
