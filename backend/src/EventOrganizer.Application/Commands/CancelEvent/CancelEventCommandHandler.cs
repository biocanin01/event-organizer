using EventOrganizer.Application.Common.Authorization;
using EventOrganizer.Application.Common.Exceptions;
using EventOrganizer.Application.Common.Interfaces;
using EventOrganizer.Domain.Events;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EventOrganizer.Application.Commands.CancelEvent
{
    public sealed class CancelEventCommandHandler : IRequestHandler<CancelEventCommand>
    {
        private readonly IApplicationDbContext _dbContext;
        private readonly EventAuthorizationService _eventAuthorizationService;

        public CancelEventCommandHandler(
            IApplicationDbContext dbContext,
            EventAuthorizationService eventAuthorizationService)
        {
            _dbContext = dbContext;
            _eventAuthorizationService = eventAuthorizationService;
        }

        public async Task Handle(
            CancelEventCommand request,
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

            eventItem.Cancel(DateTime.UtcNow);

            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
