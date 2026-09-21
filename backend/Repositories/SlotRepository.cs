/*
 * ---------------------------------------------------------------------------
 * File        : SlotRepository.cs
 * Project     : Smart Solar Microgrid Trading System — SE4040 Assignment 1
 * Author      : <Your Full Name> (<IT Number>)
 * Created     : 2026-09-19
 * Description : MongoDB implementation of ISlotRepository.
 *
 * Design note : InsertManyAsync is provided because slot generation creates
 *               dozens of documents at once. Inserting them one at a time would
 *               mean one network round trip each, which against a cloud cluster
 *               is the difference between a moment and several seconds.
 * ---------------------------------------------------------------------------
 */

using MongoDB.Driver;
using SolarMicrogrid.Api.Common;
using SolarMicrogrid.Api.Data;
using SolarMicrogrid.Api.Models;

namespace SolarMicrogrid.Api.Repositories;

/// <inheritdoc cref="ISlotRepository" />
public sealed class SlotRepository : MongoRepository<EnergyBookingSlot>, ISlotRepository
{
    /// <summary>Binds the repository to the EnergyBookingSlots collection.</summary>
    public SlotRepository(IMongoContext context)
        : base(context, CollectionNames.EnergyBookingSlots)
    {
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<EnergyBookingSlot>> GetByStationAndDateAsync(
        string stationId,
        DateTime slotDateUtc,
        CancellationToken cancellationToken = default)
    {
        // Matched as a whole day rather than an exact instant, so a slot stored
        // with a time component still matches the date the caller asked for.
        DateTime dayStart = slotDateUtc.Date;
        DateTime dayEnd = dayStart.AddDays(1);

        IReadOnlyList<EnergyBookingSlot> slots = await FindAsync(
                slot => slot.StationId == stationId && slot.SlotDate >= dayStart && slot.SlotDate < dayEnd,
                cancellationToken)
            .ConfigureAwait(false);

        return slots.OrderBy(slot => slot.StartTime, StringComparer.Ordinal).ToList();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<EnergyBookingSlot>> GetByStationBetweenAsync(
        string stationId,
        DateTime fromDateUtc,
        DateTime toDateUtc,
        CancellationToken cancellationToken = default)
    {
        DateTime start = fromDateUtc.Date;
        DateTime end = toDateUtc.Date.AddDays(1);

        IReadOnlyList<EnergyBookingSlot> slots = await FindAsync(
                slot => slot.StationId == stationId && slot.SlotDate >= start && slot.SlotDate < end,
                cancellationToken)
            .ConfigureAwait(false);

        return slots
            .OrderBy(slot => slot.SlotDate)
            .ThenBy(slot => slot.StartTime, StringComparer.Ordinal)
            .ToList();
    }

    /// <inheritdoc />
    public async Task InsertManyAsync(
        IEnumerable<EnergyBookingSlot> slots,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(slots);

        List<EnergyBookingSlot> materialised = slots.ToList();
        if (materialised.Count == 0)
        {
            return;
        }

        DateTime now = DateTime.UtcNow;
        foreach (EnergyBookingSlot slot in materialised)
        {
            slot.CreatedAt = now;
            slot.UpdatedAt = now;
        }

        await Collection.InsertManyAsync(materialised, options: null, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<bool> TryReserveCapacityAsync(
        string slotId,
        CancellationToken cancellationToken = default)
    {
        if (!MongoDB.Bson.ObjectId.TryParse(slotId, out _))
        {
            return false;
        }

        // The capacity test lives inside the FILTER, not in C#. MongoDB applies
        // the filter and the increment as one atomic operation on the document,
        // so two simultaneous bookings cannot both see the last free place.
        // If the filter does not match, nothing is written and null comes back.
        FilterDefinition<EnergyBookingSlot> filter = Builders<EnergyBookingSlot>.Filter.And(
            Builders<EnergyBookingSlot>.Filter.Eq(slot => slot.Id, slotId),
            Builders<EnergyBookingSlot>.Filter.Eq(slot => slot.IsAvailable, true),
            Builders<EnergyBookingSlot>.Filter.Where(slot => slot.BookedCount < slot.TotalCapacitySlots));

        UpdateDefinition<EnergyBookingSlot> update = Builders<EnergyBookingSlot>.Update
            .Inc(slot => slot.BookedCount, 1)
            .Set(slot => slot.UpdatedAt, DateTime.UtcNow);

        EnergyBookingSlot? updated = await Collection
            .FindOneAndUpdateAsync(
                filter,
                update,
                new FindOneAndUpdateOptions<EnergyBookingSlot> { ReturnDocument = ReturnDocument.After },
                cancellationToken)
            .ConfigureAwait(false);

        return updated is not null;
    }

    /// <inheritdoc />
    public async Task<bool> ReleaseCapacityAsync(
        string slotId,
        CancellationToken cancellationToken = default)
    {
        if (!MongoDB.Bson.ObjectId.TryParse(slotId, out _))
        {
            return false;
        }

        // The BookedCount > 0 test prevents a double cancellation from driving
        // the count negative, which would let the window be overbooked later.
        FilterDefinition<EnergyBookingSlot> filter = Builders<EnergyBookingSlot>.Filter.And(
            Builders<EnergyBookingSlot>.Filter.Eq(slot => slot.Id, slotId),
            Builders<EnergyBookingSlot>.Filter.Where(slot => slot.BookedCount > 0));

        UpdateDefinition<EnergyBookingSlot> update = Builders<EnergyBookingSlot>.Update
            .Inc(slot => slot.BookedCount, -1)
            .Set(slot => slot.UpdatedAt, DateTime.UtcNow);

        UpdateResult result = await Collection
            .UpdateOneAsync(filter, update, options: null, cancellationToken)
            .ConfigureAwait(false);

        return result.ModifiedCount > 0;
    }
}
