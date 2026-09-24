/*
 * ---------------------------------------------------------------------------
 * File        : IReservationRepository.cs
 * Project     : Smart Solar Microgrid Trading System — SE4040 Assignment 1
 * Author      : <Your Full Name> (<IT Number>)
 * Created     : 2026-09-19
 * Description : Data access specific to energy slot reservations.
 *
 * Why here    : The reservation FEATURE is built in the next step, but BR-4
 *               already needs it now: a node cannot be deactivated while it
 *               still has active reservations, and answering that question
 *               means querying this collection. Introducing the repository
 *               here lets the station rule be enforced properly straight away
 *               rather than being left as a gap to remember later.
 * ---------------------------------------------------------------------------
 */

using SolarMicrogrid.Api.Models;
using SolarMicrogrid.Api.Models.Enums;

namespace SolarMicrogrid.Api.Repositories;

/// <summary>
/// Data access for energy slot reservations.
/// </summary>
public interface IReservationRepository : IRepository<EnergyReservation>
{
    /// <summary>
    /// Counts the reservations at a node that are still live: Pending or
    /// Approved, and scheduled in the future. This is the question BR-4 asks
    /// before allowing a node to be deactivated.
    /// </summary>
    Task<long> CountActiveForStationAsync(
        string stationId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Counts the live reservations that hold a particular booking window.
    /// Used before a window is closed or its capacity is reduced.
    /// </summary>
    Task<long> CountActiveForSlotAsync(
        string slotId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds the reservation carrying a QR token, or null when no reservation
    /// holds it. This is the lookup behind server side QR verification.
    /// </summary>
    Task<EnergyReservation?> GetByQrTokenAsync(
        string qrToken,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches reservations, applying only the filters that were supplied.
    /// Backs the booking history and the search screens on both clients.
    /// </summary>
    /// <param name="prosumerNic">Limit to one prosumer, or null for all.</param>
    /// <param name="status">Limit to one lifecycle state, or null for all.</param>
    /// <param name="fromUtc">Earliest reservation time to include, or null.</param>
    /// <param name="toUtc">Latest reservation time to include, or null.</param>
    /// <param name="stationId">Limit to one node, or null for all.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<IReadOnlyList<EnergyReservation>> SearchAsync(
        string? prosumerNic,
        ReservationStatus? status,
        DateTime? fromUtc,
        DateTime? toUtc,
        string? stationId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Counts a prosumer's reservations in a given state, optionally only those
    /// still in the future. Backs the dashboard figures.
    /// </summary>
    Task<long> CountForProsumerAsync(
        string prosumerNic,
        ReservationStatus status,
        bool futureOnly,
        CancellationToken cancellationToken = default);
}
