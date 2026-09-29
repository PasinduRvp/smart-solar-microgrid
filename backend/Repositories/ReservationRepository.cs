/*
 * ---------------------------------------------------------------------------
 * File        : ReservationRepository.cs
 * Project     : Smart Solar Microgrid Trading System — SE4040 Assignment 1
 * Author      : <Your Full Name> (<IT Number>)
 * Created     : 2026-09-19
 * Description : MongoDB implementation of IReservationRepository.
 *
 * Design note : Both counts are answered by the database rather than by loading
 *               the documents and counting them in memory. The station and slot
 *               indexes created at startup make these queries cheap, and BR-4
 *               runs on every deactivation attempt.
 * ---------------------------------------------------------------------------
 */

using MongoDB.Driver;
using SolarMicrogrid.Api.Common;
using SolarMicrogrid.Api.Data;
using SolarMicrogrid.Api.Models;
using SolarMicrogrid.Api.Models.Enums;

namespace SolarMicrogrid.Api.Repositories;

/// <inheritdoc cref="IReservationRepository" />
public sealed class ReservationRepository : MongoRepository<EnergyReservation>, IReservationRepository
{
    /// <summary>Binds the repository to the EnergyReservation collection.</summary>
    public ReservationRepository(IMongoContext context)
        : base(context, CollectionNames.EnergyReservation)
    {
    }

    /// <inheritdoc />
    public async Task<long> CountActiveForStationAsync(
        string stationId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(stationId))
        {
            return 0;
        }

        DateTime now = DateTime.UtcNow;

        // "Active" means the booking still expects something to happen: it is
        // awaiting approval or already approved, and its time has not passed.
        // Completed and Cancelled bookings are history and do not block a
        // node from being taken out of service.
        return await CountAsync(
                reservation => reservation.StationId == stationId
                    && reservation.ReservationDateTime > now
                    && (reservation.Status == ReservationStatus.Pending
                        || reservation.Status == ReservationStatus.Approved),
                cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<long> CountActiveForSlotAsync(
        string slotId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(slotId))
        {
            return 0;
        }

        DateTime now = DateTime.UtcNow;

        return await CountAsync(
                reservation => reservation.SlotId == slotId
                    && reservation.ReservationDateTime > now
                    && (reservation.Status == ReservationStatus.Pending
                        || reservation.Status == ReservationStatus.Approved),
                cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<EnergyReservation?> GetByQrTokenAsync(
        string qrToken,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(qrToken))
        {
            return null;
        }

        // Matched exactly. The unique sparse index on qrToken guarantees at
        // most one reservation can hold any given token.
        return await FindOneAsync(reservation => reservation.QrToken == qrToken, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<EnergyReservation>> SearchAsync(
        string? prosumerNic,
        ReservationStatus? status,
        DateTime? fromUtc,
        DateTime? toUtc,
        string? stationId,
        CancellationToken cancellationToken = default)
    {
        // Filters are composed only for the arguments that were supplied, so an
        // omitted filter widens the search rather than excluding everything.
        FilterDefinitionBuilder<EnergyReservation> build = Builders<EnergyReservation>.Filter;
        List<FilterDefinition<EnergyReservation>> clauses = [];

        if (!string.IsNullOrWhiteSpace(prosumerNic))
        {
            clauses.Add(build.Eq(reservation => reservation.ProsumerNic, prosumerNic.Trim()));
        }

        if (status is not null)
        {
            clauses.Add(build.Eq(reservation => reservation.Status, status.Value));
        }

        if (fromUtc is not null)
        {
            clauses.Add(build.Gte(reservation => reservation.ReservationDateTime, fromUtc.Value));
        }

        if (toUtc is not null)
        {
            clauses.Add(build.Lte(reservation => reservation.ReservationDateTime, toUtc.Value));
        }

        if (!string.IsNullOrWhiteSpace(stationId))
        {
            clauses.Add(build.Eq(reservation => reservation.StationId, stationId.Trim()));
        }

        FilterDefinition<EnergyReservation> filter =
            clauses.Count == 0 ? build.Empty : build.And(clauses);

        // Newest booking first, which is the order both clients display.
        return await Collection
            .Find(filter)
            .SortByDescending(reservation => reservation.ReservationDateTime)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<long> CountForProsumerAsync(
        string prosumerNic,
        ReservationStatus status,
        bool futureOnly,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(prosumerNic))
        {
            return 0;
        }

        string nic = prosumerNic.Trim();
        DateTime now = DateTime.UtcNow;

        // Counted by the database rather than by loading the documents, so the
        // dashboard stays fast as a prosumer's history grows.
        return futureOnly
            ? await CountAsync(
                    reservation => reservation.ProsumerNic == nic
                        && reservation.Status == status
                        && reservation.ReservationDateTime > now,
                    cancellationToken)
                .ConfigureAwait(false)
            : await CountAsync(
                    reservation => reservation.ProsumerNic == nic && reservation.Status == status,
                    cancellationToken)
                .ConfigureAwait(false);
    }
}
