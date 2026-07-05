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

        public CancelEventCommandHandler(IApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
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

            eventItem.Cancel(DateTime.UtcNow);

            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}