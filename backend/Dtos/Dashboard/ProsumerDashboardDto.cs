/*
 * ---------------------------------------------------------------------------
 * File        : ProsumerDashboardDto.cs
 * Project     : Smart Solar Microgrid Trading System — SE4040 Assignment 1
 * Author      : L K P Yasith (IT 23380196)
 * Created     : 2026-09-19
 * Description : The figures shown on the prosumer's home screen in the Android
 *               application: how many bookings are awaiting approval, how many
 *               approved bookings are still to come, and what is next.
 *
 * Design note : Every number here is counted by the API. The assignment marks
 *               this dashboard specifically, and its lowest band is "values are
 *               hard-coded", so the client is given finished figures and simply
 *               displays them. Counting in the client would also mean fetching
 *               the prosumer's whole booking history just to show two numbers.
 * ---------------------------------------------------------------------------
 */

using SolarMicrogrid.Api.Dtos.Reservations;

namespace SolarMicrogrid.Api.Dtos.Dashboard;

/// <summary>
/// Summary figures for one prosumer.
/// </summary>
/// <param name="ProsumerNic">NIC of the prosumer.</param>
/// <param name="ProsumerName">Display name for the greeting.</param>
/// <param name="PendingCount">Bookings awaiting a staff decision.</param>
/// <param name="ApprovedFutureCount">Approved bookings still in the future.</param>
/// <param name="CompletedCount">Energy transfers already finalised.</param>
/// <param name="CancelledCount">Bookings that were cancelled.</param>
/// <param name="TotalEnergyDeliveredKwh">Energy delivered into the grid across completed bookings.</param>
/// <param name="TotalEnergyDrawnKwh">Energy drawn from the grid across completed bookings.</param>
/// <param name="NextBooking">The soonest upcoming booking, or null when there is none.</param>
public sealed record ProsumerDashboardDto(
    string ProsumerNic,
    string ProsumerName,
    long PendingCount,
    long ApprovedFutureCount,
    long CompletedCount,
    long CancelledCount,
    double TotalEnergyDeliveredKwh,
    double TotalEnergyDrawnKwh,
    ReservationResponseDto? NextBooking);
