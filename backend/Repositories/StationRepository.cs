/*
 * ---------------------------------------------------------------------------
 * File        : StationRepository.cs
 * Project     : Smart Solar Microgrid Trading System — SE4040 Assignment 1
 * Author      : <Your Full Name> (<IT Number>)
 * Created     : 2026-09-19
 * Description : MongoDB implementation of IStationRepository. All CRUD is
 *               inherited from MongoRepository; only the two station specific
 *               lookups appear here.
 * ---------------------------------------------------------------------------
 */

using SolarMicrogrid.Api.Common;
using SolarMicrogrid.Api.Data;
using SolarMicrogrid.Api.Models;

namespace SolarMicrogrid.Api.Repositories;

/// <inheritdoc cref="IStationRepository" />
public sealed class StationRepository : MongoRepository<SolarStationInfo>, IStationRepository
{
    /// <summary>Binds the repository to the SolarStationInfo collection.</summary>
    public StationRepository(IMongoContext context)
        : base(context, CollectionNames.SolarStationInfo)
    {
    }

    /// <inheritdoc />
    public async Task<SolarStationInfo?> GetByCodeAsync(
        string stationCode,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(stationCode))
        {
            return null;
        }

        return await FindOneAsync(station => station.StationCode == stationCode, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<SolarStationInfo>> GetActiveAsync(
        CancellationToken cancellationToken = default)
    {
        return await FindAsync(station => station.IsActive, cancellationToken).ConfigureAwait(false);
    }
}
