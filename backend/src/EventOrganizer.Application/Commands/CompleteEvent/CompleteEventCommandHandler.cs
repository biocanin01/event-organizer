using EventOrganizer.Application.Common.Authorization;
using EventOrganizer.Application.Common.Exceptions;
using EventOrganizer.Application.Common.Interfaces;
using EventOrganizer.Domain.Events;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EventOrganizer.Application.Commands.CompleteEvent
{
    public sealed class CompleteEventCommandHandler : IRequestHandler<CompleteEventCommand>
    {
        private readonly IApplicationDbContext _dbContext;
        private readonly EventAuthorizationService _eventAuthorizationService;

        public CompleteEventCommandHandler(
            IApplicationDbContext dbContext,
            EventAuthorizationService eventAuthorizationService)
        {
            _dbContext = dbContext;
            _eventAuthorizationService = eventAuthorizationService;
        }

        public async Task Handle(
            CompleteEventCommand request,
            CancellationToken cancellationToken)
        {
            var eventItem = await _dbContext.Events
                .FirstOrDefaultAsync(
                    eventItem => eventItem.Id == request.EventId,
                    cancellationToken);

            if (eventItem is null)
            {
                throw new NotFoundException(nameof(Event), request.EventId);
            }

            _eventAuthorizationService.EnsureCanManage(eventItem);
            var now = DateTime.UtcNow;
            if (eventItem.EndsAtUtc > now)
            {
                throw new ConflictException("Only events that have ended can be completed.");
            }

            try
            {
                eventItem.Complete(now);
            }
            catch (InvalidOperationException exception)
            {
                throw new ConflictException(exception.Message, exception);
            }

            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
