using EventOrganizer.Application.Common.Interfaces;
using EventOrganizer.Domain.Events;
using MediatR;

namespace EventOrganizer.Application.Commands.CreateEvent
{
    public sealed class CreateEventCommandHandler : IRequestHandler<CreateEventCommand, Guid>
    {
        private readonly IApplicationDbContext _dbContext;

        public CreateEventCommandHandler(IApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<Guid> Handle(CreateEventCommand request, CancellationToken cancellationToken)
        {
            var eventItem = Event.Create(
                request.Title,
                request.Description,
                request.StartsAtUtc,
                request.EndsAtUtc,
                request.Capacity,
                request.OrganizerUserId,
                DateTime.UtcNow);

            _dbContext.Events.Add(eventItem);
            await _dbContext.SaveChangesAsync(cancellationToken);

            return eventItem.Id;
        }
    }
}
