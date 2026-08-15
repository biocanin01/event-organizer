using EventOrganizer.Application.Common.Exceptions;
using EventOrganizer.Application.Common.Interfaces;
using EventOrganizer.Application.Responses;
using EventOrganizer.Application.Reviews;
using EventOrganizer.Domain.Events;
using EventOrganizer.Domain.Registrations;
using EventOrganizer.Domain.Reviews;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EventOrganizer.Application.Commands.CreateReview
{
    public sealed class CreateReviewCommandHandler : IRequestHandler<CreateReviewCommand, ReviewResponse>
    {
        private readonly IApplicationDbContext _dbContext;
        private readonly ICurrentUserService _currentUserService;
        private readonly IUserManagementService _userManagementService;

        public CreateReviewCommandHandler(
            IApplicationDbContext dbContext,
            ICurrentUserService currentUserService,
            IUserManagementService userManagementService)
        {
            _dbContext = dbContext;
            _currentUserService = currentUserService;
            _userManagementService = userManagementService;
        }

        public async Task<ReviewResponse> Handle(
            CreateReviewCommand request,
            CancellationToken cancellationToken)
        {
            var userId = ReviewGuards.RequireAuthenticatedUser(_currentUserService);
            var eventItem = await _dbContext.Events.FirstOrDefaultAsync(
                eventItem => eventItem.Id == request.EventId,
                cancellationToken);

            if (eventItem is null)
            {
                throw new NotFoundException(nameof(Event), request.EventId);
            }

            if (eventItem.Status != EventStatus.Completed)
            {
                throw new ConflictException("Reviews can be created only after the event is completed.");
            }

            var hasConfirmedRegistration = await _dbContext.Registrations.AnyAsync(
                registration => registration.EventId == eventItem.Id
                    && registration.ParticipantUserId == userId
                    && registration.Status == RegistrationStatus.Confirmed,
                cancellationToken);

            if (!hasConfirmedRegistration)
            {
                throw new ForbiddenException(
                    "Only users with a confirmed registration can review this event.");
            }

            if (await _dbContext.Reviews.AnyAsync(
                review => review.EventId == eventItem.Id
                    && review.ParticipantUserId == userId,
                cancellationToken))
            {
                throw new ConflictException("A review for this event already exists.");
            }

            var review = Review.Create(
                eventItem.Id,
                userId,
                request.Rating,
                request.Comment,
                DateTime.UtcNow);
            _dbContext.Reviews.Add(review);

            try
            {
                await _dbContext.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException exception)
            {
                throw new ConflictException("A review for this event already exists.", exception);
            }

            return await ReviewResponseFactory.CreateAsync(
                _dbContext,
                _userManagementService,
                review,
                cancellationToken);
        }
    }
}
