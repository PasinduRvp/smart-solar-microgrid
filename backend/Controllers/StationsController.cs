/*
 * ---------------------------------------------------------------------------
 * File        : StationsController.cs
 * Project     : Smart Solar Microgrid Trading System — SE4040 Assignment 1
 * Author      : <Your Full Name> (<IT Number>)
 * Created     : 2026-09-19
 * Description : Microgrid node management. Backs the station screens in the
 *               React back office and the nearby nodes map in the Android
 *               application.
 *
 * Security    : Reading nodes is open to any signed in user, because a prosumer
 *               must be able to see where they can trade energy. Creating,
 *               editing and deactivating are Backoffice duties, and updating
 *               battery slot availability is an operator duty, exactly as the
 *               assignment describes. Each action names the roles it admits.
 * ---------------------------------------------------------------------------
 */

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SolarMicrogrid.Api.Common;
using SolarMicrogrid.Api.Dtos.Stations;
using SolarMicrogrid.Api.Services;

namespace SolarMicrogrid.Api.Controllers;

/// <summary>
/// Management of solar microgrid nodes.
/// </summary>
[Route("api/[controller]")]
[Authorize]
public sealed class StationsController : ApiControllerBase
{
    private readonly IStationService _stationService;

    /// <summary>Receives the station service from the DI container.</summary>
    public StationsController(IStationService stationService)
    {
        _stationService = stationService ?? throw new ArgumentNullException(nameof(stationService));
    }

    /// <summary>Lists microgrid nodes.</summary>
    /// <param name="activeOnly">True to exclude deactivated nodes. Defaults to false.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="200">The matching nodes, ordered by station code.</response>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<StationResponseDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<StationResponseDto>>> GetAllAsync(
        [FromQuery] bool activeOnly,
        CancellationToken cancellationToken)
    {
        ServiceResult<IReadOnlyList<StationResponseDto>> result =
            await _stationService.GetAllAsync(activeOnly, cancellationToken).ConfigureAwait(false);

        return ToActionResult(result);
    }

    /// <summary>
    /// Lists active nodes within a radius of a position, nearest first.
    /// </summary>
    /// <remarks>
    /// This is what the Android map screen calls. The distance is calculated by
    /// the service using the haversine formula, so the client only plots what
    /// it is given.
    /// </remarks>
    /// <param name="lat">Latitude of the search point, in degrees.</param>
    /// <param name="lng">Longitude of the search point, in degrees.</param>
    /// <param name="radiusKm">Search radius in kilometres. Defaults to 25.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="200">Nearby nodes with their distances.</response>
    /// <response code="400">A coordinate or the radius was out of range.</response>
    [HttpGet("nearby")]
    [ProducesResponseType(typeof(IReadOnlyList<NearbyStationDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IReadOnlyList<NearbyStationDto>>> GetNearbyAsync(
        [FromQuery] double lat,
        [FromQuery] double lng,
        [FromQuery] double radiusKm = 25,
        CancellationToken cancellationToken = default)
    {
        ServiceResult<IReadOnlyList<NearbyStationDto>> result =
            await _stationService.GetNearbyAsync(lat, lng, radiusKm, cancellationToken).ConfigureAwait(false);

        return ToActionResult(result);
    }

    /// <summary>Returns a single node by its identifier.</summary>
    /// <param name="id">Document identifier of the node.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="200">The node.</response>
    /// <response code="404">No node with that identifier.</response>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(StationResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<StationResponseDto>> GetByIdAsync(
        string id,
        CancellationToken cancellationToken)
    {
        ServiceResult<StationResponseDto> result =
            await _stationService.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);

        return ToActionResult(result);
    }

    /// <summary>Registers a new microgrid node.</summary>
    /// <response code="201">The node was created.</response>
    /// <response code="400">The body failed validation, or the schedule was invalid.</response>
    /// <response code="409">A node already uses that station code.</response>
    [HttpPost]
    [Authorize(Roles = Roles.Backoffice)]
    [ProducesResponseType(typeof(StationResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<StationResponseDto>> CreateAsync(
        [FromBody] CreateStationRequestDto request,
        CancellationToken cancellationToken)
    {
        ServiceResult<StationResponseDto> result =
            await _stationService.CreateAsync(request, cancellationToken).ConfigureAwait(false);

        return ToCreatedResult(result, nameof(GetByIdAsync), new { id = result.Value?.Id });
    }

    /// <summary>Updates the details of a node.</summary>
    /// <param name="id">Document identifier of the node.</param>
    /// <param name="request">New values.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="200">The updated node.</response>
    /// <response code="400">Available battery slots exceeded the total installed.</response>
    /// <response code="404">No node with that identifier.</response>
    [HttpPut("{id}")]
    [Authorize(Roles = Roles.Backoffice)]
    [ProducesResponseType(typeof(StationResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<StationResponseDto>> UpdateAsync(
        string id,
        [FromBody] UpdateStationRequestDto request,
        CancellationToken cancellationToken)
    {
        ServiceResult<StationResponseDto> result =
            await _stationService.UpdateAsync(id, request, cancellationToken).ConfigureAwait(false);

        return ToActionResult(result);
    }

    /// <summary>Replaces the weekly operating schedule of a node.</summary>
    /// <param name="id">Document identifier of the node.</param>
    /// <param name="request">The replacement schedule.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="200">The node with its new schedule.</response>
    /// <response code="400">The schedule had duplicate days or invalid times.</response>
    /// <response code="404">No node with that identifier.</response>
    [HttpPut("{id}/schedule")]
    [Authorize(Roles = Roles.Backoffice)]
    [ProducesResponseType(typeof(StationResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<StationResponseDto>> UpdateScheduleAsync(
        string id,
        [FromBody] UpdateScheduleRequestDto request,
        CancellationToken cancellationToken)
    {
        ServiceResult<StationResponseDto> result =
            await _stationService.UpdateScheduleAsync(id, request, cancellationToken).ConfigureAwait(false);

        return ToActionResult(result);
    }

    /// <summary>Takes a node out of service.</summary>
    /// <remarks>
    /// This is BR-4. The request is refused while the node still has Pending or
    /// Approved reservations scheduled in the future, and the refusal states
    /// how many are blocking it.
    /// </remarks>
    /// <param name="id">Document identifier of the node.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="200">The node is now deactivated.</response>
    /// <response code="404">No node with that identifier.</response>
    /// <response code="409">The node has active reservations, or is already deactivated.</response>
    [HttpPatch("{id}/deactivate")]
    [Authorize(Roles = Roles.Backoffice)]
    [ProducesResponseType(typeof(StationResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<StationResponseDto>> DeactivateAsync(
        string id,
        CancellationToken cancellationToken)
    {
        ServiceResult<StationResponseDto> result =
            await _stationService.DeactivateAsync(id, cancellationToken).ConfigureAwait(false);

        return ToActionResult(result);
    }

    /// <summary>Returns a deactivated node to service.</summary>
    /// <param name="id">Document identifier of the node.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="200">The node is now active.</response>
    /// <response code="404">No node with that identifier.</response>
    /// <response code="409">The node is already active.</response>
    [HttpPatch("{id}/reactivate")]
    [Authorize(Roles = Roles.Backoffice)]
    [ProducesResponseType(typeof(StationResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<StationResponseDto>> ReactivateAsync(
        string id,
        CancellationToken cancellationToken)
    {
        ServiceResult<StationResponseDto> result =
            await _stationService.ReactivateAsync(id, cancellationToken).ConfigureAwait(false);

        return ToActionResult(result);
    }
}
