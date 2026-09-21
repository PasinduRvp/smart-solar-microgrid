/*
 * ---------------------------------------------------------------------------
 * File        : ISlotRepository.cs
 * Project     : Smart Solar Microgrid Trading System — SE4040 Assignment 1
 * Author      : <Your Full Name> (<IT Number>)
 * Created     : 2026-09-19
 * Description : Data access specific to bookable energy trading windows.
 * ---------------------------------------------------------------------------
 */

using SolarMicrogrid.Api.Models;

namespace SolarMicrogrid.Api.Repositories;

/// <summary>
/// Data access for energy booking slots.
/// </summary>
public interface ISlotRepository : IRepository<EnergyBookingSlot>
{
    /// <summary>
    /// Returns the windows at a node on a given UTC date, earliest first.
    /// </summary>
    Task<IReadOnlyList<EnergyBookingSlot>> GetByStationAndDateAsync(
        string stationId,
        DateTime slotDateUtc,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the windows at a node between two UTC dates inclusive.
    /// </summary>
    Task<IReadOnlyList<EnergyBookingSlot>> GetByStationBetweenAsync(
        string stationId,
        DateTime fromDateUtc,
        DateTime toDateUtc,
        CancellationToken cancellationToken = default);

    /// <summary>Inserts many windows in one round trip.</summary>
    Task InsertManyAsync(
        IEnumerable<EnergyBookingSlot> slots,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Atomically takes one place in a window, but only if the window is open
    /// and still has room. Returns false when it is full or closed.
    /// </summary>
    /// <remarks>
    /// This is how BR-9 is enforced without a transaction. The capacity check
    /// and the increment happen in a single MongoDB update, which the server
    /// applies atomically to the document. Reading the count, deciding in C#,
    /// and then writing would leave a window between the read and the write in
    /// which a second request could also decide there was room — and the window
    /// would end up overbooked. Doing both in one operation makes that
    /// impossible no matter how many requests arrive at once.
    /// </remarks>
    Task<bool> TryReserveCapacityAsync(string slotId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gives back one place in a window, used when a booking is cancelled or
    /// moved. Never lets the booked count fall below zero.
    /// </summary>
    Task<bool> ReleaseCapacityAsync(string slotId, CancellationToken cancellationToken = default);
}
