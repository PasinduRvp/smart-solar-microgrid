/*
 * ---------------------------------------------------------------------------
 * File        : DashboardService.cs
 * Project     : Smart Solar Microgrid Trading System — SE4040 Assignment 1
 * Author      : L K P Yasith (IT 23380196)
 * Created     : 2026-09-19
 * Description : Calculates every figure shown on the two dashboards. The
 *               clients receive finished numbers and display them; neither of
 *               them counts anything for itself.
 *
 * Why here    : The assignment marks the dashboards explicitly, and the lowest
 *               band is "values are hard-coded". Counting in the API also keeps
 *               the mobile client fast: showing two numbers would otherwise
 *               mean downloading a prosumer's entire booking history over a
 *               mobile connection just to call .length on it.
 *
 * SOLID       : Single Responsibility — reporting only. It changes no state and
 *               enforces no booking rules.
 *               Dependency Inversion — every collaborator is an interface.
 * ---------------------------------------------------------------------------
 */

using SolarMicrogrid.Api.Common;
using SolarMicrogrid.Api.Dtos.Dashboard;
using SolarMicrogrid.Api.Dtos.Reservations;
using SolarMicrogrid.Api.Mappings;
using SolarMicrogrid.Api.Models;
using SolarMicrogrid.Api.Models.Enums;
using SolarMicrogrid.Api.Repositories;
using SolarMicrogrid.Api.Security;

namespace SolarMicrogrid.Api.Services;

/// <inheritdoc cref="IDashboardService" />
public sealed class DashboardService : IDashboardService
{
    private readonly IReservationRepository _reservationRepository;
    private readonly IStationRepository _stationRepository;
    private readonly IUserRepository _userRepository;
    private readonly ICurrentUser _currentUser;

    /// <summary>Receives its collaborators from the DI container.</summary>
    public DashboardService(
        IReservationRepository reservationRepository,
        IStationRepository stationRepository,
        IUserRepository userRepository,
        ICurrentUser currentUser)
    {
        _reservationRepository = reservationRepository ?? throw new ArgumentNullException(nameof(reservationRepository));
        _stationRepository = stationRepository ?? throw new ArgumentNullException(nameof(stationRepository));
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
    }

    /// <inheritdoc />
    public async Task<ServiceResult<ProsumerDashboardDto>> GetProsumerDashboardAsync(
        string nic,
        CancellationToken cancellationToken = default)
    {
        string requestedNic = nic?.Trim() ?? string.Empty;

        // A prosumer may only see their own figures. Checked from the signed
        // token, so asking for another NIC in the URL achieves nothing.
        if (_currentUser.Role == UserRole.Prosumer
            && !string.Equals(_currentUser.Nic, requestedNic, StringComparison.Ordinal))
        {
            return ServiceResult.Failure<ProsumerDashboardDto>(
                ServiceErrorType.Forbidden, "You may only view your own dashboard.");
        }

        User? prosumer = await _userRepository
            .GetByNicAsync(requestedNic, cancellationToken)
            .ConfigureAwait(false);

        if (prosumer is null || prosumer.Role != UserRole.Prosumer)
        {
            return ServiceResult.Failure<ProsumerDashboardDto>(
                ServiceErrorType.NotFound, "No prosumer found with that NIC.");
        }

        // The two figures the assignment names explicitly.
        long pendingCount = await _reservationRepository
            .CountForProsumerAsync(requestedNic, ReservationStatus.Pending, futureOnly: false, cancellationToken)
            .ConfigureAwait(false);

        long approvedFutureCount = await _reservationRepository
            .CountForProsumerAsync(requestedNic, ReservationStatus.Approved, futureOnly: true, cancellationToken)
            .ConfigureAwait(false);

        long completedCount = await _reservationRepository
            .CountForProsumerAsync(requestedNic, ReservationStatus.Completed, futureOnly: false, cancellationToken)
            .ConfigureAwait(false);

        long cancelledCount = await _reservationRepository
            .CountForProsumerAsync(requestedNic, ReservationStatus.Cancelled, futureOnly: false, cancellationToken)
            .ConfigureAwait(false);

        // Completed transfers are loaded because the energy totals have to be
        // summed from them. The counts above are answered by the database
        // without loading anything, which is why they are separate queries
        // rather than being derived from one big fetch.
        IReadOnlyList<EnergyReservation> completed = await _reservationRepository
            .SearchAsync(requestedNic, ReservationStatus.Completed, null, null, null, cancellationToken)
            .ConfigureAwait(false);

        double delivered = completed
            .Where(reservation => reservation.Direction == EnergyDirection.Deliver)
            .Sum(reservation => reservation.EnergyKwh);

        double drawn = completed
            .Where(reservation => reservation.Direction == EnergyDirection.Draw)
            .Sum(reservation => reservation.EnergyKwh);

        ReservationResponseDto? nextBooking = await FindNextBookingAsync(requestedNic, cancellationToken)
            .ConfigureAwait(false);

        return ServiceResult.Success(new ProsumerDashboardDto(
            ProsumerNic: prosumer.Nic,
            ProsumerName: prosumer.FullName,
            PendingCount: pendingCount,
            ApprovedFutureCount: approvedFutureCount,
            CompletedCount: completedCount,
            CancelledCount: cancelledCount,
            TotalEnergyDeliveredKwh: Math.Round(delivered, 2),
            TotalEnergyDrawnKwh: Math.Round(drawn, 2),
            NextBooking: nextBooking));
    }

    /// <inheritdoc />
    public async Task<ServiceResult<OperatorDashboardDto>> GetOperatorDashboardAsync(
        CancellationToken cancellationToken = default)
    {
        DateTime now = DateTime.UtcNow;
        DateTime todayStart = now.Date;
        DateTime todayEnd = todayStart.AddDays(1);

        // The same seven day horizon BR-1 allows a booking to fall within.
        DateTime horizonEnd = todayStart.AddDays(BookingRules.MaximumBookingWindowDays + 1);

        long pendingApprovals = await _reservationRepository
            .CountAsync(reservation => reservation.Status == ReservationStatus.Pending, cancellationToken)
            .ConfigureAwait(false);

        long approvedFuture = await _reservationRepository
            .CountAsync(
                reservation => reservation.Status == ReservationStatus.Approved
                    && reservation.ReservationDateTime > now,
                cancellationToken)
            .ConfigureAwait(false);

        long todayScheduled = await _reservationRepository
            .CountAsync(
                reservation => reservation.ReservationDateTime >= todayStart
                    && reservation.ReservationDateTime < todayEnd
                    && (reservation.Status == ReservationStatus.Approved
                        || reservation.Status == ReservationStatus.Pending),
                cancellationToken)
            .ConfigureAwait(false);

        long todayCompleted = await _reservationRepository
            .CountAsync(
                reservation => reservation.Status == ReservationStatus.Completed
                    && reservation.CompletedAt >= todayStart
                    && reservation.CompletedAt < todayEnd,
                cancellationToken)
            .ConfigureAwait(false);

        long activeProsumers = await _userRepository
            .CountAsync(
                user => user.Role == UserRole.Prosumer && user.Status == AccountStatus.Active,
                cancellationToken)
            .ConfigureAwait(false);

        long pendingActivations = await _userRepository
            .CountAsync(
                user => user.Role == UserRole.Prosumer && user.Status == AccountStatus.Pending,
                cancellationToken)
            .ConfigureAwait(false);

        long deactivationRequests = await _userRepository
            .CountAsync(
                user => user.Role == UserRole.Prosumer && user.DeactivationRequested,
                cancellationToken)
            .ConfigureAwait(false);

        IReadOnlyList<SolarStationInfo> allStations = await _stationRepository
            .GetAllAsync(cancellationToken)
            .ConfigureAwait(false);

        List<SolarStationInfo> activeStations = allStations.Where(station => station.IsActive).ToList();

        List<StationLoadDto> loads = [];
        foreach (SolarStationInfo station in activeStations.OrderBy(s => s.StationCode, StringComparer.Ordinal))
        {
            string stationId = station.Id ?? string.Empty;

            long todayAtStation = await _reservationRepository
                .CountAsync(
                    reservation => reservation.StationId == stationId
                        && reservation.ReservationDateTime >= todayStart
                        && reservation.ReservationDateTime < todayEnd
                        && reservation.Status != ReservationStatus.Cancelled,
                    cancellationToken)
                .ConfigureAwait(false);

            // Everything still to happen at this node inside the seven day
            // booking window. A reservation made for three days' time shows up
            // here immediately, where the "today" count would leave the row at
            // zero and make the dashboard look as though nothing had happened.
            long upcomingAtStation = await _reservationRepository
                .CountAsync(
                    reservation => reservation.StationId == stationId
                        && reservation.ReservationDateTime >= todayStart
                        && reservation.ReservationDateTime < horizonEnd
                        && reservation.Status != ReservationStatus.Cancelled,
                    cancellationToken)
                .ConfigureAwait(false);

            loads.Add(new StationLoadDto(
                StationId: stationId,
                StationCode: station.StationCode,
                StationName: station.Name,
                TodayBookingCount: todayAtStation,
                UpcomingBookingCount: upcomingAtStation,
                AvailableBatterySlots: station.AvailableBatterySlots,
                TotalBatterySlots: station.TotalBatterySlots));
        }

        return ServiceResult.Success(new OperatorDashboardDto(
            PendingApprovalCount: pendingApprovals,
            ApprovedFutureCount: approvedFuture,
            TodayScheduledCount: todayScheduled,
            TodayCompletedCount: todayCompleted,
            ActiveStationCount: activeStations.Count,
            InactiveStationCount: allStations.Count - activeStations.Count,
            ActiveProsumerCount: activeProsumers,
            PendingActivationCount: pendingActivations,
            DeactivationRequestCount: deactivationRequests,
            StationLoads: loads));
    }

    /// <summary>
    /// Finds the prosumer's soonest upcoming booking that has not been
    /// cancelled, so the home screen can show "your next transfer".
    /// </summary>
    private async Task<ReservationResponseDto?> FindNextBookingAsync(
        string nic,
        CancellationToken cancellationToken)
    {
        DateTime now = DateTime.UtcNow;

        IReadOnlyList<EnergyReservation> upcoming = await _reservationRepository
            .SearchAsync(nic, status: null, fromUtc: now, toUtc: null, stationId: null, cancellationToken)
            .ConfigureAwait(false);

        EnergyReservation? next = upcoming
            .Where(reservation => reservation.Status is ReservationStatus.Pending or ReservationStatus.Approved)
            .OrderBy(reservation => reservation.ReservationDateTime)
            .FirstOrDefault();

        if (next is null)
        {
            return null;
        }

        SolarStationInfo? station = await _stationRepository
            .GetByIdAsync(next.StationId, cancellationToken)
            .ConfigureAwait(false);

        User? prosumer = await _userRepository.GetByNicAsync(nic, cancellationToken).ConfigureAwait(false);

        return next.ToResponseDto(station, prosumer?.FullName ?? string.Empty, now);
    }
}
