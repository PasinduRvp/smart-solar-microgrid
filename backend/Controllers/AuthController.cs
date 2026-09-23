/*
 * ---------------------------------------------------------------------------
 * File        : AuthController.cs
 * Project     : Smart Solar Microgrid Trading System — SE4040 Assignment 1
 * Author      : <Your Full Name> (<IT Number>)
 * Created     : 2026-09-19
 * Description : Sign in and prosumer self registration endpoints. Used by the
 *               React web application (Backoffice and Grid Operator sign in)
 *               and by the Android application (prosumer registration and sign
 *               in, and Grid Operator sign in for the scanner screens).
 *
 *               Both actions are thin: they hand the request to IAuthService
 *               and translate the outcome. No credential checking, no rule
 *               about pending accounts and no token building happens here.
 *
 * SOLID       : Single Responsibility — HTTP only.
 *               Dependency Inversion — depends on IAuthService.
 * Security    : These are the only two endpoints that may be reached without a
 *               token, so they carry [AllowAnonymous] explicitly rather than
 *               relying on the absence of an attribute. Both are rate limited
 *               to blunt password guessing and bulk account creation.
 * ---------------------------------------------------------------------------
 */

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using SolarMicrogrid.Api.Common;
using SolarMicrogrid.Api.Dtos.Auth;
using SolarMicrogrid.Api.Services;

namespace SolarMicrogrid.Api.Controllers;

/// <summary>
/// Authentication and prosumer registration.
/// </summary>
[Route("api/[controller]")]
public sealed class AuthController : ApiControllerBase
{
    private readonly IAuthService _authService;

    /// <summary>Receives the authentication service from the DI container.</summary>
    public AuthController(IAuthService authService)
    {
        _authService = authService ?? throw new ArgumentNullException(nameof(authService));
    }

    /// <summary>
    /// Signs a user in and returns an access token.
    /// </summary>
    /// <remarks>
    /// Staff sign in with their email address; prosumers use their NIC. The
    /// returned role tells the client which home screen to open.
    /// </remarks>
    /// <response code="200">Credentials accepted. The response carries the token.</response>
    /// <response code="400">The request body failed validation.</response>
    /// <response code="401">The identifier or password was wrong.</response>
    /// <response code="403">The account is pending activation or deactivated.</response>
    /// <response code="429">Too many sign in attempts from this address.</response>
    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting(RateLimitPolicies.Authentication)]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<AuthResponseDto>> LoginAsync(
        [FromBody] LoginRequestDto request,
        CancellationToken cancellationToken)
    {
        // [ApiController] has already rejected a body that fails the DTO's
        // validation attributes, so anything arriving here is well formed.
        ServiceResult<AuthResponseDto> result =
            await _authService.LoginAsync(request, cancellationToken).ConfigureAwait(false);

        return ToActionResult(result);
    }

    /// <summary>
    /// Registers a new solar prosumer from the mobile application.
    /// </summary>
    /// <remarks>
    /// The account is created in the Pending state and cannot sign in until a
    /// Backoffice officer activates it from the web application. The role is
    /// always Prosumer and cannot be chosen by the caller.
    /// </remarks>
    /// <response code="201">The account was created and is awaiting activation.</response>
    /// <response code="400">The request body failed validation.</response>
    /// <response code="409">The NIC or email address is already registered.</response>
    /// <response code="429">Too many registration attempts from this address.</response>
    [HttpPost("register")]
    [AllowAnonymous]
    [EnableRateLimiting(RateLimitPolicies.Authentication)]
    [ProducesResponseType(typeof(string), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<string>> RegisterAsync(
        [FromBody] RegisterProsumerRequestDto request,
        CancellationToken cancellationToken)
    {
        ServiceResult<string> result =
            await _authService.RegisterProsumerAsync(request, cancellationToken).ConfigureAwait(false);

        // 201 with the new id in the body, and no Location header: the account
        // is Pending and only staff may read it, so an anonymous caller has no
        // endpoint to be pointed at.
        return ToCreatedResult(result);
    }
}
