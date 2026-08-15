using EventOrganizer.Application.Common.Exceptions;
using EventOrganizer.Application.Common.Interfaces;
using EventOrganizer.Application.Responses;
using EventOrganizer.Application.Reviews;
using EventOrganizer.Domain.Events;
using EventOrganizer.Domain.Reviews;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EventOrganizer.Application.Commands.UpdateReview
{
    public sealed class UpdateReviewCommandHandler : IRequestHandler<UpdateReviewCommand, ReviewResponse>
    {
        private readonly IApplicationDbContext _dbContext;
        private readonly ICurrentUserService _currentUserService;
        private readonly IUserManagementService _userManagementService;

        public UpdateReviewCommandHandler(
            IApplicationDbContext dbContext,
            ICurrentUserService currentUserService,
            IUserManagementService userManagementService)
        {
            _dbContext = dbContext;
            _currentUserService = currentUserService;
            _userManagementService = userManagementService;
        }

        public async Task<ReviewResponse> Handle(
            UpdateReviewCommand request,
            CancellationToken cancellationToken)
        {
            var userId = ReviewGuards.RequireAuthenticatedUser(_currentUserService);
            var review = await _dbContext.Reviews.FirstOrDefaultAsync(
                review => review.Id == request.ReviewId,
                cancellationToken);

            if (review is null)
            {
                throw new NotFoundException(nameof(Review), request.ReviewId);
            }

            ReviewGuards.EnsureOwner(review, userId);
            ReviewGuards.EnsureExpectedVersion(review, request.Version);

            var eventStatus = await _dbContext.Events
                .Where(eventItem => eventItem.Id == review.EventId)
                .Select(eventItem => (EventStatus?)eventItem.Status)
                .FirstOrDefaultAsync(cancellationToken);

            if (eventStatus is null)
            {
                throw new NotFoundException(nameof(Event), review.EventId);
            }

            if (eventStatus != EventStatus.Completed)
            {
                throw new ConflictException("Reviews can be updated only after the event is completed.");
            }

            try
            {
                review.Update(request.Rating, request.Comment, DateTime.UtcNow);
                await _dbContext.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateConcurrencyException exception)
            {
                throw new ConflictException("The review has changed. Refresh it and try again.", exception);
            }

            return await ReviewResponseFactory.CreateAsync(
                _dbContext,
                _userManagementService,
                review,
                cancellationToken);
        }
    }
}
