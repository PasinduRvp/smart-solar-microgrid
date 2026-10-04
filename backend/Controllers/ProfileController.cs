/*
 * ---------------------------------------------------------------------------
 * File        : ProfileController.cs
 * Project     : Smart Solar Microgrid Trading System — SE4040 Assignment 1
 * Author      : <Your Full Name> (<IT Number>)
 * Created     : 2026-09-20
 * Description : "My account" endpoints. Every signed in user — Backoffice
 *               officer, Grid Operator or Prosumer — uses these to view and
 *               edit their own details and change their own password.
 *
 * Security    : [Authorize] with no role, because this is not about privilege:
 *               everybody may edit themselves and nobody else. None of these
 *               routes takes an account id. The account is always the one in
 *               the token, so there is no value a caller could change to aim
 *               an operation at another person's record.
 *
 *               That is a stronger guarantee than a permission check, because
 *               a check can be forgotten when a new endpoint is added here,
 *               whereas a route with no id simply cannot address anyone else.
 * ---------------------------------------------------------------------------
 */

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SolarMicrogrid.Api.Common;
using SolarMicrogrid.Api.Dtos.Users;
using SolarMicrogrid.Api.Services;

namespace SolarMicrogrid.Api.Controllers;

/// <summary>
/// The signed in user's own account.
/// </summary>
[Route("api/[controller]")]
[Authorize]
public sealed class ProfileController : ApiControllerBase
{
    private readonly IProfileService _profileService;

    /// <summary>Receives the profile service from the DI container.</summary>
    public ProfileController(IProfileService profileService)
    {
        _profileService = profileService ?? throw new ArgumentNullException(nameof(profileService));
    }

    /// <summary>Returns your own profile.</summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="200">Your account details.</response>
    /// <response code="401">You are not signed in.</response>
    [HttpGet]
    [ProducesResponseType(typeof(UserResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<UserResponseDto>> GetMyProfileAsync(
        CancellationToken cancellationToken)
    {
        ServiceResult<UserResponseDto> result =
            await _profileService.GetMyProfileAsync(cancellationToken).ConfigureAwait(false);

        return ToActionResult(result);
    }

    /// <summary>Updates your own details.</summary>
    /// <remarks>
    /// Your NIC, role and account status cannot be changed here. They are not
    /// fields on the request, so a profile edit can never alter who you are or
    /// what you are allowed to do.
    /// </remarks>
    /// <param name="request">The new values.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="200">Your updated profile.</response>
    /// <response code="400">The body failed validation.</response>
    /// <response code="409">Another account already uses that email address.</response>
    [HttpPut]
    [ProducesResponseType(typeof(UserResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<UserResponseDto>> UpdateMyProfileAsync(
        [FromBody] UpdateMyProfileRequestDto request,
        CancellationToken cancellationToken)
    {
        ServiceResult<UserResponseDto> result =
            await _profileService.UpdateMyProfileAsync(request, cancellationToken).ConfigureAwait(false);

        return ToActionResult(result);
    }

    /// <summary>Changes your own password.</summary>
    /// <remarks>
    /// Your current password must be supplied as well as the new one. A valid
    /// token alone is not accepted, so an unattended session cannot be used to
    /// lock the real owner out of their account.
    /// </remarks>
    /// <param name="request">Current and new passwords.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="200">The password was changed.</response>
    /// <response code="400">The new password is the same as the current one, or too short.</response>
    /// <response code="401">The current password was not correct.</response>
    [HttpPut("password")]
    [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<bool>> ChangeMyPasswordAsync(
        [FromBody] ChangePasswordRequestDto request,
        CancellationToken cancellationToken)
    {
        ServiceResult<bool> result =
            await _profileService.ChangeMyPasswordAsync(request, cancellationToken).ConfigureAwait(false);

        return ToActionResult(result);
    }
}
