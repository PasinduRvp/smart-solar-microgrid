/*
 * ---------------------------------------------------------------------------
 * File        : IAuthService.cs
 * Project     : Smart Solar Microgrid Trading System — SE4040 Assignment 1
 * Author      : <Your Full Name> (<IT Number>)
 * Created     : 2026-09-19
 * Description : Contract for authentication and prosumer self registration.
 *
 * SOLID       : Dependency Inversion — AuthController depends on this, not on
 *               AuthService, the repository, BCrypt or the JWT handler.
 * ---------------------------------------------------------------------------
 */

using SolarMicrogrid.Api.Common;
using SolarMicrogrid.Api.Dtos.Auth;

namespace SolarMicrogrid.Api.Services;

/// <summary>
/// Authenticates users and registers new prosumer accounts.
/// </summary>
public interface IAuthService
{
    /// <summary>
    /// Verifies credentials and issues an access token.
    /// </summary>
    Task<ServiceResult<AuthResponseDto>> LoginAsync(
        LoginRequestDto request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Registers a new prosumer. The account is created in the Pending state
    /// and cannot sign in until a Backoffice officer activates it.
    /// </summary>
    Task<ServiceResult<string>> RegisterProsumerAsync(
        RegisterProsumerRequestDto request,
        CancellationToken cancellationToken = default);
}
