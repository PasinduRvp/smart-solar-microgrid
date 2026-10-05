/*
 * ---------------------------------------------------------------------------
 * File        : OperatorDashboardDto.cs
 * Project     : Smart Solar Microgrid Trading System — SE4040 Assignment 1
 * Author      : L K P Yasith (IT 23380196)
 * Created     : 2026-09-19
 * Description : The figures shown on the operator and back office home screens
 *               in the React web application: what needs attention now, what is
 *               scheduled today, and how busy each node is.
 *
 * Design note : PendingActivationCount ties the prosumer approval queue into
 *               the same view, so an officer signing in sees both kinds of
 *               outstanding work — bookings to approve and accounts to
 *               activate — without navigating anywhere first.
 * ---------------------------------------------------------------------------
 */

namespace SolarMicrogrid.Api.Dtos.Dashboard;

/// <summary>
/// Summary figures for staff.
/// </summary>
/// <param name="PendingApprovalCount">Bookings awaiting approval.</param>
/// <param name="ApprovedFutureCount">Approved bookings still to come, across all prosumers.</param>
/// <param name="TodayScheduledCount">Bookings scheduled for today.</param>
/// <param name="TodayCompletedCount">Energy transfers finalised today.</param>
/// <param name="ActiveStationCount">Nodes currently in service.</param>
/// <param name="InactiveStationCount">Nodes taken out of service.</param>
/// <param name="ActiveProsumerCount">Prosumer accounts that can sign in.</param>
/// <param name="PendingActivationCount">Prosumer accounts awaiting activation.</param>
/// <param name="DeactivationRequestCount">Prosumers who have asked to be deactivated.</param>
/// <param name="StationLoads">Today's load at each node in service.</param>
public sealed record OperatorDashboardDto(
    long PendingApprovalCount,
    long ApprovedFutureCount,
    long TodayScheduledCount,
    long TodayCompletedCount,
    int ActiveStationCount,
    int InactiveStationCount,
    long ActiveProsumerCount,
    long PendingActivationCount,
    long DeactivationRequestCount,
    IReadOnlyList<StationLoadDto> StationLoads);
