/*
 * ---------------------------------------------------------------------------
 * File        : IUserRepository.cs
 * Project     : Smart Solar Microgrid Trading System — SE4040 Assignment 1
 * Author      : <Your Full Name> (<IT Number>)
 * Created     : 2026-09-17
 * Description : User specific data access, on top of the generic contract.
 *
 * SOLID       : Interface Segregation — lookups that only make sense for users
 *               live here rather than being pushed onto IRepository, where
 *               stations and reservations would inherit methods they can never
 *               use.
 *               Open/Closed — the generic repository gains user behaviour
 *               through extension rather than modification.
 * ---------------------------------------------------------------------------
 */

using SolarMicrogrid.Api.Models;
using SolarMicrogrid.Api.Models.Enums;

namespace SolarMicrogrid.Api.Repositories;

/// <summary>
/// Data access for user accounts.
/// </summary>
public interface IUserRepository : IRepository<User>
{
    /// <summary>
    /// Finds a user by National Identity Card number, the business key for
    /// prosumers. Returns null when no such account exists.
    /// </summary>
    Task<User?> GetByNicAsync(string nic, CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds a user by email address, used when staff sign in.
    /// The comparison is case insensitive, because email addresses are.
    /// </summary>
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);

    /// <summary>Returns every account currently in the given lifecycle state.</summary>
    Task<IReadOnlyList<User>> GetByStatusAsync(
        AccountStatus status,
        CancellationToken cancellationToken = default);

    /// <summary>Returns every account holding the given role.</summary>
    Task<IReadOnlyList<User>> GetByRoleAsync(
        UserRole role,
        CancellationToken cancellationToken = default);
}
