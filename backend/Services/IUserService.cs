/*
 * ---------------------------------------------------------------------------
 * File        : IUserService.cs
 * Project     : Smart Solar Microgrid Trading System — SE4040 Assignment 1
 * Author      : <Your Full Name> (<IT Number>)
 * Created     : 2026-09-19
 * Description : Contract for managing web application users, which the
 *               assignment defines as Backoffice officers and Grid Operators.
 *               Prosumer accounts are handled by IProsumerService.
 *
 * SOLID       : Interface Segregation — staff administration and prosumer
 *               administration are separate concerns with different rules and
 *               different callers, so they are separate interfaces rather than
 *               one large IUserManagementService.
 * ---------------------------------------------------------------------------
 */

using SolarMicrogrid.Api.Common;
using SolarMicrogrid.Api.Dtos.Users;

namespace SolarMicrogrid.Api.Services;

/// <summary>
/// Creates and maintains Backoffice and Grid Operator accounts.
/// </summary>
public interface IUserService
{
    /// <summary>Returns every staff account, newest first.</summary>
    Task<ServiceResult<IReadOnlyList<UserResponseDto>>> GetStaffUsersAsync(
        CancellationToken cancellationToken = default);

    /// <summary>Returns one account by its document identifier.</summary>
    Task<ServiceResult<UserResponseDto>> GetByIdAsync(
        string id,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a Backoffice or Grid Operator account. Staff accounts are
    /// created Active, because a Backoffice officer has already vetted them.
    /// </summary>
    Task<ServiceResult<UserResponseDto>> CreateStaffUserAsync(
        CreateStaffUserRequestDto request,
        CancellationToken cancellationToken = default);

    /// <summary>Updates the editable details of an account.</summary>
    Task<ServiceResult<UserResponseDto>> UpdateAsync(
        string id,
        UpdateUserRequestDto request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Permanently removes a staff account. Prosumer accounts are deactivated
    /// rather than deleted, so that their reservation history still resolves.
    /// </summary>
    Task<ServiceResult<bool>> DeleteStaffUserAsync(
        string id,
        CancellationToken cancellationToken = default);
}
