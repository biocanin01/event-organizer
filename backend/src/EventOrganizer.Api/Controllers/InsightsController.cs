using EventOrganizer.Api.Authorization;
using EventOrganizer.Application.Queries.GetEventInsightById;
using EventOrganizer.Application.Queries.ListEventInsights;
using EventOrganizer.Application.Responses;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EventOrganizer.Api.Controllers
{
    [ApiController]
    [Route("api/insights/events")]
    [Authorize(Policy = AuthorizationPolicies.CanViewInsights)]
    public sealed class InsightsController : ControllerBase
    {
        private readonly ISender _sender;

        public InsightsController(ISender sender)
        {
            _sender = sender;
        }

        [HttpGet]
        [ProducesResponseType(typeof(IReadOnlyList<EventInsightSummaryResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<IReadOnlyList<EventInsightSummaryResponse>>> List(
            CancellationToken cancellationToken)
        {
            return Ok(await _sender.Send(new ListEventInsightsQuery(), cancellationToken));
        }

        [HttpGet("{eventId:guid}")]
        [ProducesResponseType(typeof(EventInsightDetailsResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<EventInsightDetailsResponse>> GetById(
            Guid eventId,
            CancellationToken cancellationToken)
        {
            return Ok(await _sender.Send(
                new GetEventInsightByIdQuery(eventId),
                cancellationToken));
        }
    }
}
