/*
 * ---------------------------------------------------------------------------
 * File        : ProsumersController.cs
 * Project     : Smart Solar Microgrid Trading System — SE4040 Assignment 1
 * Author      : Vidvanga W A U (IT 23293694)
 * Created     : 2026-09-19
 * Description : Prosumer account administration. Backs the prosumer management
 *               and pending activation screens in the React back office, and
 *               the profile and "request deactivation" screens in the Android
 *               application.
 *
 * Security    : The controller requires a token, and each action then names the
 *               roles it admits. The two most sensitive transitions are the
 *               narrowest: activate is Backoffice only, which is BR-5.
 *               Where an action is open to prosumers, the service still checks
 *               that the caller owns the record, because the role alone does
 *               not establish ownership.
 * ---------------------------------------------------------------------------
 */

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SolarMicrogrid.Api.Common;
using SolarMicrogrid.Api.Dtos.Users;
using SolarMicrogrid.Api.Models.Enums;
using SolarMicrogrid.Api.Services;

namespace SolarMicrogrid.Api.Controllers;

/// <summary>
/// Administration of solar prosumer accounts.
/// </summary>
[Route("api/[controller]")]
[Authorize]
public sealed class ProsumersController : ApiControllerBase
{
    private readonly IProsumerService _prosumerService;

    /// <summary>Receives the prosumer service from the DI container.</summary>
    public ProsumersController(IProsumerService prosumerService)
    {
        _prosumerService = prosumerService ?? throw new ArgumentNullException(nameof(prosumerService));
    }

    /// <summary>Lists prosumer accounts, optionally filtered by state.</summary>
    /// <param name="status">Optional filter: Pending, Active or Deactivated.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="200">The matching prosumer accounts.</response>
    /// <response code="403">The caller is not a staff member.</response>
    [HttpGet]
    [Authorize(Roles = Roles.BackofficeOrGridOperator)]
    [ProducesResponseType(typeof(IReadOnlyList<UserResponseDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<UserResponseDto>>> GetAllAsync(
        [FromQuery] AccountStatus? status,
        CancellationToken cancellationToken)
    {
        ServiceResult<IReadOnlyList<UserResponseDto>> result =
            await _prosumerService.GetProsumersAsync(status, cancellationToken).ConfigureAwait(false);

        return ToActionResult(result);
    }

    /// <summary>
    /// Lists accounts awaiting activation, oldest request first.
    /// </summary>
    /// <remarks>
    /// This is the pending activation screen the assignment asks for. A
    /// prosumer who registers on the mobile application appears here until a
    /// Backoffice officer activates them.
    /// </remarks>
    /// <response code="200">Accounts awaiting activation.</response>
    /// <response code="403">The caller is not a Backoffice officer.</response>
    [HttpGet("pending")]
    [Authorize(Roles = Roles.Backoffice)]
    [ProducesResponseType(typeof(IReadOnlyList<UserResponseDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<UserResponseDto>>> GetPendingAsync(
        CancellationToken cancellationToken)
    {
        ServiceResult<IReadOnlyList<UserResponseDto>> result =
            await _prosumerService.GetPendingActivationsAsync(cancellationToken).ConfigureAwait(false);

        return ToActionResult(result);
    }

    /// <summary>Returns one prosumer by National Identity Card number.</summary>
    /// <param name="nic">NIC of the prosumer.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="200">The prosumer profile.</response>
    /// <response code="403">A prosumer asked for someone else's profile.</response>
    /// <response code="404">No prosumer with that NIC.</response>
    [HttpGet("{nic}")]
    [ProducesResponseType(typeof(UserResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserResponseDto>> GetByNicAsync(
        string nic,
        CancellationToken cancellationToken)
    {
        ServiceResult<UserResponseDto> result =
            await _prosumerService.GetByNicAsync(nic, cancellationToken).ConfigureAwait(false);

        return ToActionResult(result);
    }

    /// <summary>Updates a prosumer profile.</summary>
    /// <remarks>
    /// A prosumer may edit only their own profile; a Backoffice officer may
    /// edit any. NIC, role and status cannot be changed here.
    /// </remarks>
    /// <param name="nic">NIC of the prosumer.</param>
    /// <param name="request">New profile values.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="200">The updated profile.</response>
    /// <response code="403">A prosumer tried to edit someone else's profile.</response>
    /// <response code="404">No prosumer with that NIC.</response>
    [HttpPut("{nic}")]
    [ProducesResponseType(typeof(UserResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserResponseDto>> UpdateAsync(
        string nic,
        [FromBody] UpdateUserRequestDto request,
        CancellationToken cancellationToken)
    {
        ServiceResult<UserResponseDto> result =
            await _prosumerService.UpdateProfileAsync(nic, request, cancellationToken).ConfigureAwait(false);

        return ToActionResult(result);
    }

    /// <summary>
    /// Activates a pending or deactivated prosumer account. Backoffice only.
    /// </summary>
    /// <remarks>
    /// This is BR-5. A Grid Operator calling it receives 403, even though
    /// operators may use most other prosumer endpoints.
    /// </remarks>
    /// <param name="nic">NIC of the prosumer.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="200">The account is now active.</response>
    /// <response code="403">The caller is not a Backoffice officer.</response>
    /// <response code="404">No prosumer with that NIC.</response>
    /// <response code="409">The account is already active.</response>
    [HttpPatch("{nic}/activate")]
    [Authorize(Roles = Roles.Backoffice)]
    [ProducesResponseType(typeof(UserResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<UserResponseDto>> ActivateAsync(
        string nic,
        CancellationToken cancellationToken)
    {
        ServiceResult<UserResponseDto> result =
            await _prosumerService.ActivateAsync(nic, cancellationToken).ConfigureAwait(false);

        return ToActionResult(result);
    }

    /// <summary>Deactivates a prosumer account.</summary>
    /// <param name="nic">NIC of the prosumer.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="200">The account is now deactivated.</response>
    /// <response code="403">The caller is not a staff member.</response>
    /// <response code="404">No prosumer with that NIC.</response>
    /// <response code="409">The account is already deactivated.</response>
    [HttpPatch("{nic}/deactivate")]
    [Authorize(Roles = Roles.BackofficeOrGridOperator)]
    [ProducesResponseType(typeof(UserResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<UserResponseDto>> DeactivateAsync(
        string nic,
        CancellationToken cancellationToken)
    {
        ServiceResult<UserResponseDto> result =
            await _prosumerService.DeactivateAsync(nic, cancellationToken).ConfigureAwait(false);

        return ToActionResult(result);
    }

    /// <summary>
    /// Records the prosumer's own request to have their account deactivated.
    /// </summary>
    /// <remarks>
    /// This is BR-6. The account stays usable: the request only raises a flag
    /// for the back office to act on. A prosumer cannot deactivate themselves.
    /// </remarks>
    /// <param name="nic">NIC of the prosumer making the request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="200">The request was recorded.</response>
    /// <response code="403">A prosumer tried to raise this for another account.</response>
    /// <response code="404">No prosumer with that NIC.</response>
    /// <response code="409">The account is already deactivated.</response>
    [HttpPatch("{nic}/request-deactivation")]
    [Authorize(Roles = Roles.Prosumer)]
    [ProducesResponseType(typeof(UserResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserResponseDto>> RequestDeactivationAsync(
        string nic,
        CancellationToken cancellationToken)
    {
        ServiceResult<UserResponseDto> result =
            await _prosumerService.RequestDeactivationAsync(nic, cancellationToken).ConfigureAwait(false);

        return ToActionResult(result);
    }
}
