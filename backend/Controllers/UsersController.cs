/*
 * ---------------------------------------------------------------------------
 * File        : UsersController.cs
 * Project     : Smart Solar Microgrid Trading System — SE4040 Assignment 1
 * Author      : <Your Full Name> (<IT Number>)
 * Created     : 2026-09-19
 * Description : Web application user management. Backs the "Users" screens in
 *               the React back office, where a Backoffice officer creates and
 *               maintains Backoffice and Grid Operator accounts.
 *
 * Security    : [Authorize(Roles = Roles.Backoffice)] is applied to the whole
 *               controller rather than to each action. Applying it once means a
 *               new action added later is protected by default; protecting each
 *               action individually would make an omission easy and invisible.
 *               A Grid Operator calling any of these receives 403.
 * ---------------------------------------------------------------------------
 */

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SolarMicrogrid.Api.Common;
using SolarMicrogrid.Api.Dtos.Users;
using SolarMicrogrid.Api.Services;

namespace SolarMicrogrid.Api.Controllers;

/// <summary>
/// Administration of Backoffice and Grid Operator accounts.
/// </summary>
[Route("api/[controller]")]
[Authorize(Roles = Roles.Backoffice)]
public sealed class UsersController : ApiControllerBase
{
    private readonly IUserService _userService;

    /// <summary>Receives the user service from the DI container.</summary>
    public UsersController(IUserService userService)
    {
        _userService = userService ?? throw new ArgumentNullException(nameof(userService));
    }

    /// <summary>Lists all Backoffice and Grid Operator accounts.</summary>
    /// <response code="200">The list of staff accounts.</response>
    /// <response code="403">The caller is not a Backoffice officer.</response>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<UserResponseDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<UserResponseDto>>> GetAllAsync(
        CancellationToken cancellationToken)
    {
        ServiceResult<IReadOnlyList<UserResponseDto>> result =
            await _userService.GetStaffUsersAsync(cancellationToken).ConfigureAwait(false);

        return ToActionResult(result);
    }

    /// <summary>Returns a single account by its identifier.</summary>
    /// <param name="id">Document identifier of the account.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="200">The account.</response>
    /// <response code="404">No account with that identifier.</response>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(UserResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserResponseDto>> GetByIdAsync(
        string id,
        CancellationToken cancellationToken)
    {
        ServiceResult<UserResponseDto> result =
            await _userService.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);

        return ToActionResult(result);
    }

    /// <summary>Creates a Backoffice or Grid Operator account.</summary>
    /// <response code="201">The account was created.</response>
    /// <response code="400">The body failed validation, or the role was not a staff role.</response>
    /// <response code="409">The NIC or email address is already registered.</response>
    [HttpPost]
    [ProducesResponseType(typeof(UserResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<UserResponseDto>> CreateAsync(
        [FromBody] CreateStaffUserRequestDto request,
        CancellationToken cancellationToken)
    {
        ServiceResult<UserResponseDto> result =
            await _userService.CreateStaffUserAsync(request, cancellationToken).ConfigureAwait(false);

        // GetByIdAsync exists on this controller and takes an id, so the
        // Location header can be built safely here.
        return ToCreatedResult(result, nameof(GetByIdAsync), new { id = result.Value?.Id });
    }

    /// <summary>Updates the editable details of an account.</summary>
    /// <param name="id">Document identifier of the account.</param>
    /// <param name="request">New values.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="200">The updated account.</response>
    /// <response code="404">No account with that identifier.</response>
    /// <response code="409">Another account already uses that email address.</response>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(UserResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<UserResponseDto>> UpdateAsync(
        string id,
        [FromBody] UpdateUserRequestDto request,
        CancellationToken cancellationToken)
    {
        ServiceResult<UserResponseDto> result =
            await _userService.UpdateAsync(id, request, cancellationToken).ConfigureAwait(false);

        return ToActionResult(result);
    }

    /// <summary>Deletes a staff account.</summary>
    /// <param name="id">Document identifier of the account.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="200">True when the account was removed.</response>
    /// <response code="404">No account with that identifier.</response>
    /// <response code="409">The account is a prosumer, is your own, or is the last Backoffice officer.</response>
    [HttpDelete("{id}")]
    [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<bool>> DeleteAsync(
        string id,
        CancellationToken cancellationToken)
    {
        ServiceResult<bool> result =
            await _userService.DeleteStaffUserAsync(id, cancellationToken).ConfigureAwait(false);

        return ToActionResult(result);
    }
}
