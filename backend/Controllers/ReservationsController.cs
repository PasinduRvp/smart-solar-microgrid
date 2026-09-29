/*
 * ---------------------------------------------------------------------------
 * File        : ReservationsController.cs
 * Project     : Smart Solar Microgrid Trading System — SE4040 Assignment 1
 * Author      : <Your Full Name> (<IT Number>)
 * Created     : 2026-09-19
 * Description : The reservation endpoints. These back the booking screens in
 *               the Android application, the reservation management and
 *               approval screens in the React back office, and the operator's
 *               QR scanner flow.
 *
 * Security    : Roles are narrowed per action. Approval is a staff decision;
 *               verifying a scanned code and finalising a transfer are operator
 *               duties; fetching a QR payload is restricted to prosumers and
 *               then further restricted by the service to the owner alone.
 *               Where an action is open to prosumers, the service still checks
 *               ownership, because a role does not establish whose booking it is.
 * ---------------------------------------------------------------------------
 */

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SolarMicrogrid.Api.Common;
using SolarMicrogrid.Api.Dtos.Reservations;
using SolarMicrogrid.Api.Models.Enums;
using SolarMicrogrid.Api.Services;

namespace SolarMicrogrid.Api.Controllers;

/// <summary>
/// Energy slot reservations.
/// </summary>
[Route("api/[controller]")]
[Authorize]
public sealed class ReservationsController : ApiControllerBase
{
    private readonly IReservationService _reservationService;

    /// <summary>Receives the reservation service from the DI container.</summary>
    public ReservationsController(IReservationService reservationService)
    {
        _reservationService = reservationService ?? throw new ArgumentNullException(nameof(reservationService));
    }

    /// <summary>
    /// Searches reservations. Backs the booking history and filter screens.
    /// </summary>
    /// <remarks>
    /// A prosumer always sees only their own bookings: the NIC filter is
    /// replaced with their own by the service, whatever they supply.
    /// </remarks>
    /// <param name="nic">Limit to one prosumer. Ignored for prosumer callers.</param>
    /// <param name="status">Pending, Approved, Completed or Cancelled.</param>
    /// <param name="from">Earliest reservation time to include, UTC.</param>
    /// <param name="to">Latest reservation time to include, UTC.</param>
    /// <param name="stationId">Limit to one microgrid node.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="200">The matching reservations, newest first.</response>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<ReservationResponseDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ReservationResponseDto>>> SearchAsync(
        [FromQuery] string? nic,
        [FromQuery] ReservationStatus? status,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] string? stationId,
        CancellationToken cancellationToken)
    {
        ServiceResult<IReadOnlyList<ReservationResponseDto>> result = await _reservationService
            .SearchAsync(nic, status, from, to, stationId, cancellationToken)
            .ConfigureAwait(false);

        return ToActionResult(result);
    }

    /// <summary>Lists reservations awaiting approval, soonest booking first.</summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="200">The pending reservations.</response>
    /// <response code="403">The caller is not a staff member.</response>
    [HttpGet("pending")]
    [Authorize(Roles = Roles.BackofficeOrGridOperator)]
    [ProducesResponseType(typeof(IReadOnlyList<ReservationResponseDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ReservationResponseDto>>> GetPendingAsync(
        CancellationToken cancellationToken)
    {
        ServiceResult<IReadOnlyList<ReservationResponseDto>> result =
            await _reservationService.GetPendingAsync(cancellationToken).ConfigureAwait(false);

        return ToActionResult(result);
    }

    /// <summary>Returns a single reservation.</summary>
    /// <param name="id">Document identifier of the reservation.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="200">The reservation.</response>
    /// <response code="403">A prosumer asked for someone else's booking.</response>
    /// <response code="404">No reservation with that identifier.</response>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ReservationResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ReservationResponseDto>> GetByIdAsync(
        string id,
        CancellationToken cancellationToken)
    {
        ServiceResult<ReservationResponseDto> result =
            await _reservationService.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);

        return ToActionResult(result);
    }

    /// <summary>Books an energy trading window.</summary>
    /// <remarks>
    /// BR-1 applies: the chosen window must be in the future and within seven
    /// days. BR-9 applies: the window must still have a free place, claimed
    /// atomically so two simultaneous bookings cannot overfill it.
    /// </remarks>
    /// <param name="request">The booking request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="201">The reservation, for the summary screen.</response>
    /// <response code="400">The body failed validation, or no NIC was supplied by staff.</response>
    /// <response code="403">The prosumer account is not active.</response>
    /// <response code="404">The window or the prosumer does not exist.</response>
    /// <response code="409">Outside the 7 day window, node out of service, or the window is full.</response>
    [HttpPost]
    [ProducesResponseType(typeof(ReservationResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ReservationResponseDto>> CreateAsync(
        [FromBody] CreateReservationRequestDto request,
        CancellationToken cancellationToken)
    {
        ServiceResult<ReservationResponseDto> result =
            await _reservationService.CreateAsync(request, cancellationToken).ConfigureAwait(false);

        return ToCreatedResult(result, nameof(GetByIdAsync), new { id = result.Value?.Id });
    }

    /// <summary>Changes a reservation.</summary>
    /// <remarks>
    /// BR-2 applies: refused unless at least twelve hours remain before the
    /// booking's current time. An approved booking returns to Pending, and any
    /// QR code already issued is retired.
    /// </remarks>
    /// <param name="id">Document identifier of the reservation.</param>
    /// <param name="request">The requested changes.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="200">The updated reservation, for the summary screen.</response>
    /// <response code="403">A prosumer tried to change someone else's booking.</response>
    /// <response code="404">The reservation or the new window does not exist.</response>
    /// <response code="409">Inside the 12 hour notice period, already finished, or the new window is full.</response>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ReservationResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ReservationResponseDto>> UpdateAsync(
        string id,
        [FromBody] UpdateReservationRequestDto request,
        CancellationToken cancellationToken)
    {
        ServiceResult<ReservationResponseDto> result =
            await _reservationService.UpdateAsync(id, request, cancellationToken).ConfigureAwait(false);

        return ToActionResult(result);
    }

    /// <summary>Cancels a reservation.</summary>
    /// <remarks>
    /// BR-3 applies: refused unless at least twelve hours remain before the
    /// booking. The place is returned to the window so someone else can take it.
    /// </remarks>
    /// <param name="id">Document identifier of the reservation.</param>
    /// <param name="request">The cancellation reason.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="200">The cancelled reservation, for the summary screen.</response>
    /// <response code="403">A prosumer tried to cancel someone else's booking.</response>
    /// <response code="404">No reservation with that identifier.</response>
    /// <response code="409">Inside the 12 hour notice period, or already finished.</response>
    [HttpPatch("{id}/cancel")]
    [ProducesResponseType(typeof(ReservationResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ReservationResponseDto>> CancelAsync(
        string id,
        [FromBody] CancelReservationRequestDto request,
        CancellationToken cancellationToken)
    {
        ServiceResult<ReservationResponseDto> result =
            await _reservationService.CancelAsync(id, request, cancellationToken).ConfigureAwait(false);

        return ToActionResult(result);
    }

    /// <summary>Approves a reservation and issues its QR token.</summary>
    /// <remarks>This is BR-8. A booking carries no scannable code until this succeeds.</remarks>
    /// <param name="id">Document identifier of the reservation.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="200">The approved reservation.</response>
    /// <response code="403">The caller is not a staff member.</response>
    /// <response code="404">No reservation with that identifier.</response>
    /// <response code="409">The reservation is not pending, or its time has passed.</response>
    [HttpPatch("{id}/approve")]
    [Authorize(Roles = Roles.BackofficeOrGridOperator)]
    [ProducesResponseType(typeof(ReservationResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ReservationResponseDto>> ApproveAsync(
        string id,
        CancellationToken cancellationToken)
    {
        ServiceResult<ReservationResponseDto> result =
            await _reservationService.ApproveAsync(id, cancellationToken).ConfigureAwait(false);

        return ToActionResult(result);
    }

    /// <summary>
    /// Returns the QR payload for an approved reservation, for the prosumer's
    /// application to render as a QR image.
    /// </summary>
    /// <param name="id">Document identifier of the reservation.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="200">The token and the details shown beside the code.</response>
    /// <response code="403">The caller does not own this reservation.</response>
    /// <response code="404">No reservation with that identifier.</response>
    /// <response code="409">The reservation has not been approved.</response>
    [HttpGet("{id}/qr")]
    [Authorize(Roles = Roles.Prosumer)]
    [ProducesResponseType(typeof(QrTokenResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<QrTokenResponseDto>> GetQrTokenAsync(
        string id,
        CancellationToken cancellationToken)
    {
        ServiceResult<QrTokenResponseDto> result =
            await _reservationService.GetQrTokenAsync(id, cancellationToken).ConfigureAwait(false);

        return ToActionResult(result);
    }

    /// <summary>Verifies a scanned QR code against the stored reservation.</summary>
    /// <remarks>
    /// The server side half of BR-8. The operator's device sends what it
    /// scanned and is told whether it is genuine; it never decides for itself.
    /// </remarks>
    /// <param name="request">The scanned token.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="200">The reservation the code belongs to.</response>
    /// <response code="403">The caller is not a grid operator.</response>
    /// <response code="404">The code is not valid.</response>
    /// <response code="409">The code is not valid.</response>
    [HttpPost("verify-qr")]
    [Authorize(Roles = Roles.BackofficeOrGridOperator)]
    [ProducesResponseType(typeof(ReservationResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ReservationResponseDto>> VerifyQrAsync(
        [FromBody] VerifyQrRequestDto request,
        CancellationToken cancellationToken)
    {
        ServiceResult<ReservationResponseDto> result =
            await _reservationService.VerifyQrAsync(request, cancellationToken).ConfigureAwait(false);

        return ToActionResult(result);
    }

    /// <summary>Finalises the energy transfer after a successful scan.</summary>
    /// <param name="id">Document identifier of the reservation.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="200">The completed reservation.</response>
    /// <response code="403">The caller is not a grid operator.</response>
    /// <response code="404">No reservation with that identifier.</response>
    /// <response code="409">The reservation is not approved.</response>
    [HttpPatch("{id}/complete")]
    [Authorize(Roles = Roles.BackofficeOrGridOperator)]
    [ProducesResponseType(typeof(ReservationResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ReservationResponseDto>> CompleteAsync(
        string id,
        CancellationToken cancellationToken)
    {
        ServiceResult<ReservationResponseDto> result =
            await _reservationService.CompleteAsync(id, cancellationToken).ConfigureAwait(false);

        return ToActionResult(result);
    }
}
