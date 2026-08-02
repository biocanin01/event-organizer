using EventOrganizer.Api.Authorization;
using EventOrganizer.Api.Contracts.OrganizerRoleRequests;
using EventOrganizer.Application.Commands.ApproveOrganizerRoleRequest;
using EventOrganizer.Application.Commands.RejectOrganizerRoleRequest;
using EventOrganizer.Application.Commands.SubmitOrganizerRoleRequest;
using EventOrganizer.Application.Commands.WithdrawOrganizerRoleRequest;
using EventOrganizer.Application.Queries.GetMyOrganizerRoleRequest;
using EventOrganizer.Application.Queries.ListOrganizerRoleRequests;
using EventOrganizer.Application.Responses;
using EventOrganizer.Domain.Users;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EventOrganizer.Api.Controllers
{
    [ApiController]
    [Route("api/organizer-role-requests")]
    public sealed class OrganizerRoleRequestsController : ControllerBase
    {
        private readonly ISender _sender;

        public OrganizerRoleRequestsController(ISender sender)
        {
            _sender = sender;
        }

        [HttpGet("me")]
        [Authorize(Policy = AuthorizationPolicies.CanRequestOrganizerRole)]
        [ProducesResponseType(typeof(OrganizerRoleRequestResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<OrganizerRoleRequestResponse>> GetMyRequest(
            CancellationToken cancellationToken)
        {
            var organizerRoleRequest = await _sender.Send(
                new GetMyOrganizerRoleRequestQuery(),
                cancellationToken);

            if (organizerRoleRequest is null)
            {
                return NoContent();
            }

            return Ok(organizerRoleRequest);
        }

        [HttpPost]
        [Authorize(Policy = AuthorizationPolicies.CanRequestOrganizerRole)]
        [ProducesResponseType(typeof(SubmitOrganizerRoleRequestResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<ActionResult<SubmitOrganizerRoleRequestResponse>> Submit(
            SubmitOrganizerRoleRequestRequest request,
            CancellationToken cancellationToken)
        {
            var requestId = await _sender.Send(
                new SubmitOrganizerRoleRequestCommand(request.Motivation),
                cancellationToken);

            return StatusCode(
                StatusCodes.Status201Created,
                new SubmitOrganizerRoleRequestResponse(requestId));
        }

        [HttpGet]
        [Authorize(Policy = AuthorizationPolicies.CanManageOrganizerRoleRequests)]
        [ProducesResponseType(typeof(IReadOnlyList<OrganizerRoleRequestResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<IReadOnlyList<OrganizerRoleRequestResponse>>> List(
            OrganizerRoleRequestStatus? status,
            CancellationToken cancellationToken)
        {
            var requests = await _sender.Send(
                new ListOrganizerRoleRequestsQuery(status),
                cancellationToken);

            return Ok(requests);
        }

        [HttpPatch("{id:guid}/approve")]
        [Authorize(Policy = AuthorizationPolicies.CanManageOrganizerRoleRequests)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Approve(
            Guid id,
            ApproveOrganizerRoleRequestRequest request,
            CancellationToken cancellationToken)
        {
            await _sender.Send(
                new ApproveOrganizerRoleRequestCommand(id, request.Version),
                cancellationToken);

            return NoContent();
        }

        [HttpPatch("{id:guid}/reject")]
        [Authorize(Policy = AuthorizationPolicies.CanManageOrganizerRoleRequests)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Reject(
            Guid id,
            RejectOrganizerRoleRequestRequest request,
            CancellationToken cancellationToken)
        {
            await _sender.Send(
                new RejectOrganizerRoleRequestCommand(
                    id,
                    request.DecisionReason,
                    request.Version),
                cancellationToken);

            return NoContent();
        }

        [HttpPatch("{id:guid}/withdraw")]
        [Authorize(Policy = AuthorizationPolicies.CanRequestOrganizerRole)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Withdraw(
            Guid id,
            WithdrawOrganizerRoleRequestRequest request,
            CancellationToken cancellationToken)
        {
            await _sender.Send(
                new WithdrawOrganizerRoleRequestCommand(id, request.Version),
                cancellationToken);

            return NoContent();
        }
    }
}
