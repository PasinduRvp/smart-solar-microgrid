/*
 * ---------------------------------------------------------------------------
 * File        : IProfileService.cs
 * Project     : Smart Solar Microgrid Trading System — SE4040 Assignment 1
 * Author      : <Your Full Name> (<IT Number>)
 * Created     : 2026-09-20
 * Description : Contract for the account operations a signed in user performs
 *               on themselves: viewing their profile, editing it, and changing
 *               their password.
 *
 * Why separate from IUserService
 *               IUserService is administration: one person acting on another
 *               person's account, restricted to Backoffice officers. This is
 *               self service: any signed in user acting on their own account,
 *               whatever their role.
 *
 *               They are different concerns with different rules and different
 *               callers, so they are different interfaces. Interface
 *               Segregation — and it also means no endpoint here needs to take
 *               an account id at all, because the account is always the caller.
 *               An operation that cannot name another user cannot be aimed at
 *               one by mistake.
 * ---------------------------------------------------------------------------
 */

using SolarMicrogrid.Api.Common;
using SolarMicrogrid.Api.Dtos.Users;

namespace SolarMicrogrid.Api.Services;

/// <summary>
/// Self service account operations for the signed in user.
/// </summary>
public interface IProfileService
{
    /// <summary>Returns the signed in user's own profile.</summary>
    Task<ServiceResult<UserResponseDto>> GetMyProfileAsync(
        CancellationToken cancellationToken = default);

    /// <summary>Updates the signed in user's own editable details.</summary>
    Task<ServiceResult<UserResponseDto>> UpdateMyProfileAsync(
        UpdateMyProfileRequestDto request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Changes the signed in user's password, after checking the current one.
    /// </summary>
    Task<ServiceResult<bool>> ChangeMyPasswordAsync(
        ChangePasswordRequestDto request,
        CancellationToken cancellationToken = default);
}
