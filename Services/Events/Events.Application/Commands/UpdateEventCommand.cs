
using Events.Application.Abstractions.Messaging;
using Events.Application.Abstractions.Repositories;
using Events.Application.DTOs;
using Events.Application.Exceptions;
using MediatR;

namespace Events.Application.Commands;

public sealed record UpdateEventCommand(Guid Id, EventUpdateDto updateDto) :
ICommand<EventResponseDto>;

internal sealed class UpdateEventCommandHandler(
    IEventRepository repository) : IRequestHandler<UpdateEventCommand, EventResponseDto>
{
    public async Task<EventResponseDto> Handle(UpdateEventCommand request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var existing = await repository.GetByIdAsync(request.Id, cancellationToken);
        if (existing == null) throw new EventNotFoundException(request.Id);

        existing.Update(
            request.updateDto.Title,
            request.updateDto.StartAt,
            request.updateDto.EndAt,
            DateTimeOffset.UtcNow,
            request.updateDto.Description,
            request.updateDto.Price,
            request.updateDto.Currency);

        return EventResponseDto.FromEntity(existing);
    }
}
