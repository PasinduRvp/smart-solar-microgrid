/*
 * ---------------------------------------------------------------------------
 * File        : StationLoadDto.cs
 * Project     : Smart Solar Microgrid Trading System — SE4040 Assignment 1
 * Author      : L K P Yasith (IT 23380196)
 * Created     : 2026-09-19
 * Description : How busy one microgrid node is, for the operator's dashboard.
 *
 * Two counts, not one
 *               "Today" answers what an operator standing at a node needs to
 *               know this morning. But bookings are made up to seven days
 *               ahead, so on a quiet day every node reads zero and the table
 *               looks broken rather than calm. The upcoming count shows the
 *               work that exists but has not arrived yet, which is what makes
 *               a new booking visible straight away.
 * ---------------------------------------------------------------------------
 */

namespace SolarMicrogrid.Api.Dtos.Dashboard;

/// <summary>
/// Today's load at a single node.
/// </summary>
/// <param name="StationId">Document identifier of the node.</param>
/// <param name="StationCode">Reference code of the node.</param>
/// <param name="StationName">Display name of the node.</param>
/// <param name="TodayBookingCount">Bookings scheduled at the node today.</param>
/// <param name="UpcomingBookingCount">Bookings scheduled in the next seven days, today included.</param>
/// <param name="AvailableBatterySlots">Battery storage slots currently free.</param>
/// <param name="TotalBatterySlots">Battery storage slots installed.</param>
public sealed record StationLoadDto(
    string StationId,
    string StationCode,
    string StationName,
    long TodayBookingCount,
    long UpcomingBookingCount,
    int AvailableBatterySlots,
    int TotalBatterySlots);
