using EventOrganizer.Api.Contracts.Reviews;
using EventOrganizer.Application.Commands.CreateReview;
using EventOrganizer.Application.Commands.UpdateReview;
using EventOrganizer.Application.Queries.ListEventReviews;
using EventOrganizer.Application.Queries.ListManagedReviews;
using EventOrganizer.Application.Queries.ListMyReviews;
using EventOrganizer.Application.Responses;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EventOrganizer.Api.Controllers
{
    [ApiController]
    [Route("api")]
    public sealed class ReviewsController : ControllerBase
    {
        private readonly ISender _sender;

        public ReviewsController(ISender sender)
        {
            _sender = sender;
        }

        [HttpGet("events/{eventId:guid}/reviews")]
        [ProducesResponseType(typeof(IReadOnlyList<ReviewResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<IReadOnlyList<ReviewResponse>>> ListForEvent(
            Guid eventId,
            CancellationToken cancellationToken)
        {
            return Ok(await _sender.Send(
                new ListEventReviewsQuery(eventId),
                cancellationToken));
        }

        [HttpPost("events/{eventId:guid}/reviews")]
        [Authorize]
        [ProducesResponseType(typeof(ReviewResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<ActionResult<ReviewResponse>> Create(
            Guid eventId,
            CreateReviewRequest request,
            CancellationToken cancellationToken)
        {
            var review = await _sender.Send(
                new CreateReviewCommand(eventId, request.Rating, request.Comment),
                cancellationToken);

            return StatusCode(StatusCodes.Status201Created, review);
        }

        [HttpGet("reviews/me")]
        [Authorize]
        [ProducesResponseType(typeof(IReadOnlyList<ReviewResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<IReadOnlyList<ReviewResponse>>> ListMine(
            CancellationToken cancellationToken)
        {
            return Ok(await _sender.Send(new ListMyReviewsQuery(), cancellationToken));
        }

        [HttpPut("reviews/{id:guid}")]
        [Authorize]
        [ProducesResponseType(typeof(ReviewResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<ActionResult<ReviewResponse>> Update(
            Guid id,
            UpdateReviewRequest request,
            CancellationToken cancellationToken)
        {
            return Ok(await _sender.Send(
                new UpdateReviewCommand(id, request.Version, request.Rating, request.Comment),
                cancellationToken));
        }

        [HttpGet("reviews/manage")]
        [Authorize]
        [ProducesResponseType(typeof(IReadOnlyList<ReviewResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<IReadOnlyList<ReviewResponse>>> ListManaged(
            Guid? eventId,
            CancellationToken cancellationToken)
        {
            return Ok(await _sender.Send(
                new ListManagedReviewsQuery(eventId),
                cancellationToken));
        }
    }
}
