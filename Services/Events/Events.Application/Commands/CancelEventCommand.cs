using Events.Application.Abstractions.Messaging;
using Events.Application.Abstractions.Repositories;
using Events.Application.Exceptions;
using MediatR;

namespace Events.Application.Commands;

public sealed record CancelEventCommand(Guid id) : ICommand<bool>;

internal sealed class CancelEventCommandHandler(IEventRepository repository) : IRequestHandler<CancelEventCommand, bool>
{
    public async Task<bool> Handle(CancelEventCommand e, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var existing = await repository.GetByIdAsync(e.id, cancellationToken);
        if (existing == null) throw new EventNotFoundException(e.id);

        existing.Cancel(DateTimeOffset.UtcNow);

        return true;
    }
}
