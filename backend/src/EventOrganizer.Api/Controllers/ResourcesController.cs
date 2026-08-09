using EventOrganizer.Api.Authorization;
using EventOrganizer.Api.Contracts.Resources;
using EventOrganizer.Application.Commands.ArchiveResource;
using EventOrganizer.Application.Commands.CreateResource;
using EventOrganizer.Application.Commands.MarkResourceAvailable;
using EventOrganizer.Application.Commands.MarkResourceUnavailable;
using EventOrganizer.Application.Commands.UpdateResource;
using EventOrganizer.Application.Queries.GetResourceById;
using EventOrganizer.Application.Queries.ListResources;
using EventOrganizer.Application.Responses;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EventOrganizer.Api.Controllers
{
    [ApiController]
    [Route("api/resources")]
    [Authorize]
    public sealed class ResourcesController : ControllerBase
    {
        private readonly ISender _sender;

        public ResourcesController(ISender sender)
        {
            _sender = sender;
        }

        [HttpGet]
        [Authorize(Policy = AuthorizationPolicies.CanBrowseResources)]
        [ProducesResponseType(typeof(IReadOnlyList<ResourceResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<IReadOnlyList<ResourceResponse>>> List(
            CancellationToken cancellationToken)
        {
            var resources = await _sender.Send(
                new ListResourcesQuery(),
                cancellationToken);

            return Ok(resources);
        }

        [HttpGet("{id:guid}")]
        [Authorize(Policy = AuthorizationPolicies.CanBrowseResources)]
        [ProducesResponseType(typeof(ResourceResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ResourceResponse>> GetById(
            Guid id,
            CancellationToken cancellationToken)
        {
            var resource = await _sender.Send(
                new GetResourceByIdQuery(id),
                cancellationToken);

            if (resource is null)
            {
                return NotFound();
            }

            return Ok(resource);
        }

        [HttpPost]
        [Authorize(Policy = AuthorizationPolicies.CanManageResources)]
        [ProducesResponseType(typeof(CreateResourceResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<CreateResourceResponse>> Create(
            CreateResourceRequest request,
            CancellationToken cancellationToken)
        {
            var resourceId = await _sender.Send(
                new CreateResourceCommand(
                    request.Name,
                    request.Description,
                    request.Type,
                    request.Cost,
                    request.QualityScore,
                    request.Capacity,
                    request.ExpertiseArea,
                    request.ProviderName,
                    request.SupportedCapacity,
                    request.ServiceArea,
                    request.IncludesTechnicalSupport,
                    request.ContentsSummary),
                cancellationToken);

            return StatusCode(
                StatusCodes.Status201Created,
                new CreateResourceResponse(resourceId));
        }

        [HttpPut("{id:guid}")]
        [Authorize(Policy = AuthorizationPolicies.CanManageResources)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Update(
            Guid id,
            UpdateResourceRequest request,
            CancellationToken cancellationToken)
        {
            await _sender.Send(
                new UpdateResourceCommand(
                    id,
                    request.Name,
                    request.Description,
                    request.Type,
                    request.Cost,
                    request.QualityScore,
                    request.Capacity,
                    request.ExpertiseArea,
                    request.ProviderName,
                    request.SupportedCapacity,
                    request.ServiceArea,
                    request.IncludesTechnicalSupport,
                    request.ContentsSummary),
                cancellationToken);

            return NoContent();
        }

        [HttpPatch("{id:guid}/mark-available")]
        [Authorize(Policy = AuthorizationPolicies.CanManageResources)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> MarkAvailable(
            Guid id,
            CancellationToken cancellationToken)
        {
            await _sender.Send(
                new MarkResourceAvailableCommand(id),
                cancellationToken);

            return NoContent();
        }

        [HttpPatch("{id:guid}/mark-unavailable")]
        [Authorize(Policy = AuthorizationPolicies.CanManageResources)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> MarkUnavailable(
            Guid id,
            CancellationToken cancellationToken)
        {
            await _sender.Send(
                new MarkResourceUnavailableCommand(id),
                cancellationToken);

            return NoContent();
        }

        [HttpPatch("{id:guid}/archive")]
        [Authorize(Policy = AuthorizationPolicies.CanManageResources)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Archive(
            Guid id,
            CancellationToken cancellationToken)
        {
            await _sender.Send(
                new ArchiveResourceCommand(id),
                cancellationToken);

            return NoContent();
        }
    }
}
