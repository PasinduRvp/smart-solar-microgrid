/*
 * ---------------------------------------------------------------------------
 * File        : IProsumerService.cs
 * Project     : Smart Solar Microgrid Trading System — SE4040 Assignment 1
 * Author      : Vidvanga W A U (IT 23293694)
 * Created     : 2026-09-19
 * Description : Contract for prosumer account administration: listing, the
 *               pending activation queue, profile editing, and the activate,
 *               deactivate and request deactivation transitions.
 *
 * Business    : BR-5  Only a Backoffice officer may reactivate an account.
 *               BR-6  A prosumer may REQUEST deactivation from the mobile
 *                     application but cannot deactivate themselves.
 *               BR-7  A prosumer registers in the Pending state; activation is
 *                     what lets them sign in.
 * ---------------------------------------------------------------------------
 */

using SolarMicrogrid.Api.Common;
using SolarMicrogrid.Api.Dtos.Users;
using SolarMicrogrid.Api.Models.Enums;

namespace SolarMicrogrid.Api.Services;

/// <summary>
/// Administers solar prosumer accounts.
/// </summary>
public interface IProsumerService
{
    /// <summary>
    /// Returns prosumer accounts, optionally filtered by lifecycle state.
    /// </summary>
    /// <param name="status">State to filter by, or null for all prosumers.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<ServiceResult<IReadOnlyList<UserResponseDto>>> GetProsumersAsync(
        AccountStatus? status,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the accounts awaiting activation. This backs the pending
    /// activation screen in the web application.
    /// </summary>
    Task<ServiceResult<IReadOnlyList<UserResponseDto>>> GetPendingActivationsAsync(
        CancellationToken cancellationToken = default);

    /// <summary>Returns one prosumer by National Identity Card number.</summary>
    Task<ServiceResult<UserResponseDto>> GetByNicAsync(
        string nic,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates a prosumer profile. A prosumer may update only their own
    /// profile; a Backoffice officer may update any.
    /// </summary>
    Task<ServiceResult<UserResponseDto>> UpdateProfileAsync(
        string nic,
        UpdateUserRequestDto request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Activates a pending or deactivated account. BR-5: Backoffice only.
    /// </summary>
    Task<ServiceResult<UserResponseDto>> ActivateAsync(
        string nic,
        CancellationToken cancellationToken = default);

    /// <summary>Deactivates an account. Performed by a Backoffice officer.</summary>
    Task<ServiceResult<UserResponseDto>> DeactivateAsync(
        string nic,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Records a prosumer's own request to have their account deactivated.
    /// BR-6: this only raises a flag for the back office to act on; it does not
    /// change the account state.
    /// </summary>
    Task<ServiceResult<UserResponseDto>> RequestDeactivationAsync(
        string nic,
        CancellationToken cancellationToken = default);
}
