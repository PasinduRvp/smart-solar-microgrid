/*
 * ---------------------------------------------------------------------------
 * File        : SlotsController.cs
 * Project     : Smart Solar Microgrid Trading System — SE4040 Assignment 1
 * Author      : <Your Full Name> (<IT Number>)
 * Created     : 2026-09-19
 * Description : Energy booking window management. Prosumers read windows when
 *               choosing a time on the mobile application; Backoffice officers
 *               generate them, and Grid Operators open and close them, which is
 *               the "update battery slot availability" duty in the assignment.
 *
 * Route note  : Listing and generating windows are addressed under the node
 *               they belong to (/api/stations/{stationId}/slots), because a
 *               window has no meaning apart from its node. Acting on one
 *               window uses its own identifier (/api/slots/{id}).
 * ---------------------------------------------------------------------------
 */

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SolarMicrogrid.Api.Common;
using SolarMicrogrid.Api.Dtos.Slots;
using SolarMicrogrid.Api.Services;

namespace SolarMicrogrid.Api.Controllers;

/// <summary>
/// Management of bookable energy trading windows.
/// </summary>
[Route("api")]
[Authorize]
public sealed class SlotsController : ApiControllerBase
{
    private readonly ISlotService _slotService;

    /// <summary>Receives the slot service from the DI container.</summary>
    public SlotsController(ISlotService slotService)
    {
        _slotService = slotService ?? throw new ArgumentNullException(nameof(slotService));
    }

    /// <summary>Lists the booking windows at a node.</summary>
    /// <param name="stationId">Identifier of the node.</param>
    /// <param name="date">Optional UTC date. Omit to see the next seven days.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="200">The windows, with their remaining capacity.</response>
    /// <response code="404">No node with that identifier.</response>
    [HttpGet("stations/{stationId}/slots")]
    [ProducesResponseType(typeof(IReadOnlyList<SlotResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<SlotResponseDto>>> GetForStationAsync(
        string stationId,
        [FromQuery] DateTime? date,
        CancellationToken cancellationToken)
    {
        ServiceResult<IReadOnlyList<SlotResponseDto>> result =
            await _slotService.GetForStationAsync(stationId, date, cancellationToken).ConfigureAwait(false);

        return ToActionResult(result);
    }

    /// <summary>
    /// Generates booking windows for a node from its weekly schedule.
    /// </summary>
    /// <remarks>
    /// Each open day in the schedule is divided into windows of the requested
    /// length. Days that already have windows are skipped, so running this more
    /// than once is safe and will not create duplicates.
    /// </remarks>
    /// <param name="stationId">Identifier of the node.</param>
    /// <param name="request">Generation parameters.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="201">The windows that were created.</response>
    /// <response code="400">The start date was in the past, or a value was out of range.</response>
    /// <response code="404">No node with that identifier.</response>
    /// <response code="409">The node is deactivated, has no schedule, or already has windows for every day.</response>
    [HttpPost("stations/{stationId}/slots/generate")]
    [Authorize(Roles = Roles.Backoffice)]
    [ProducesResponseType(typeof(IReadOnlyList<SlotResponseDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<IReadOnlyList<SlotResponseDto>>> GenerateAsync(
        string stationId,
        [FromBody] GenerateSlotsRequestDto request,
        CancellationToken cancellationToken)
    {
        ServiceResult<IReadOnlyList<SlotResponseDto>> result =
            await _slotService.GenerateAsync(stationId, request, cancellationToken).ConfigureAwait(false);

        return ToCreatedResult(result);
    }

    /// <summary>Returns a single booking window.</summary>
    /// <param name="id">Document identifier of the window.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="200">The window.</response>
    /// <response code="404">No window with that identifier.</response>
    [HttpGet("slots/{id}")]
    [ProducesResponseType(typeof(SlotResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SlotResponseDto>> GetByIdAsync(
        string id,
        CancellationToken cancellationToken)
    {
        ServiceResult<SlotResponseDto> result =
            await _slotService.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);

        return ToActionResult(result);
    }

    /// <summary>
    /// Opens or closes a booking window, and optionally changes its capacity.
    /// </summary>
    /// <remarks>
    /// This is the operator's "update battery slot availability" action. BR-9
    /// applies: capacity cannot be reduced below the bookings already taken,
    /// and a window holding active reservations cannot be closed.
    /// </remarks>
    /// <param name="id">Document identifier of the window.</param>
    /// <param name="request">New availability and optional capacity.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="200">The updated window.</response>
    /// <response code="403">The caller is not a staff member.</response>
    /// <response code="404">No window with that identifier.</response>
    /// <response code="409">Capacity below bookings taken, or the window holds active reservations.</response>
    [HttpPatch("slots/{id}/availability")]
    [Authorize(Roles = Roles.BackofficeOrGridOperator)]
    [ProducesResponseType(typeof(SlotResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<SlotResponseDto>> UpdateAvailabilityAsync(
        string id,
        [FromBody] UpdateSlotAvailabilityRequestDto request,
        CancellationToken cancellationToken)
    {
        ServiceResult<SlotResponseDto> result =
            await _slotService.UpdateAvailabilityAsync(id, request, cancellationToken).ConfigureAwait(false);

        return ToActionResult(result);
    }
}
