/*
 * ---------------------------------------------------------------------------
 * File        : IStationRepository.cs
 * Project     : Smart Solar Microgrid Trading System — SE4040 Assignment 1
 * Author      : <Your Full Name> (<IT Number>)
 * Created     : 2026-09-19
 * Description : Data access specific to microgrid nodes.
 *
 * SOLID       : Interface Segregation and Open/Closed, the same pattern as
 *               IUserRepository: the generic contract is extended with the two
 *               lookups only stations need, rather than those being added to
 *               IRepository where every other entity would inherit them.
 * ---------------------------------------------------------------------------
 */

using SolarMicrogrid.Api.Models;

namespace SolarMicrogrid.Api.Repositories;

/// <summary>
/// Data access for solar microgrid nodes.
/// </summary>
public interface IStationRepository : IRepository<SolarStationInfo>
{
    /// <summary>Finds a node by its reference code, or null when absent.</summary>
    Task<SolarStationInfo?> GetByCodeAsync(string stationCode, CancellationToken cancellationToken = default);

    /// <summary>Returns every node that has not been deactivated.</summary>
    Task<IReadOnlyList<SolarStationInfo>> GetActiveAsync(CancellationToken cancellationToken = default);
}
