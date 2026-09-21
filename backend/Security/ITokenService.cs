/*
 * ---------------------------------------------------------------------------
 * File        : ITokenService.cs
 * Project     : Smart Solar Microgrid Trading System — SE4040 Assignment 1
 * Author      : <Your Full Name> (<IT Number>)
 * Created     : 2026-09-19
 * Description : Contract for issuing access tokens to authenticated users.
 *
 * SOLID       : Dependency Inversion and Interface Segregation — AuthService
 *               needs a token and nothing more, so that is all this interface
 *               offers. The JWT library is not visible beyond the
 *               implementation.
 * ---------------------------------------------------------------------------
 */

using SolarMicrogrid.Api.Models;

namespace SolarMicrogrid.Api.Security;

/// <summary>
/// Issues signed access tokens.
/// </summary>
public interface ITokenService
{
    /// <summary>
    /// Creates a signed token describing the given user.
    /// </summary>
    /// <param name="user">The authenticated account.</param>
    /// <returns>The encoded token and the UTC instant it expires.</returns>
    (string Token, DateTime ExpiresAtUtc) CreateToken(User user);
}
