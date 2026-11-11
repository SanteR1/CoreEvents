
using Events.Application.Abstractions.Messaging;
using Events.Application.Abstractions.Repositories;
using Events.Application.DTOs;
using Events.Domain.Entities;
using MediatR;

namespace Events.Application.Commands;

public sealed record CreateEventCommand(EventCreateDto createDto) : ICommand<EventResponseDto>;

internal sealed class CreateEventCommandHandler(IEventRepository repository) : IRequestHandler<CreateEventCommand, EventResponseDto>
{
    public Task<EventResponseDto> Handle(CreateEventCommand request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var entity = Event.Create(
            title: request.createDto.Title!,
            startAt: request.createDto.StartAt!.Value,
            endAt: request.createDto.EndAt!.Value,
            now: DateTimeOffset.UtcNow,
            totalSeats: request.createDto.TotalSeats!.Value,
            description: request.createDto.Description,
            price: request.createDto.Price,
            currency: request.createDto.Currency);

        repository.Add(entity);
        return Task.FromResult(EventResponseDto.FromEntity(entity));
    }
}
