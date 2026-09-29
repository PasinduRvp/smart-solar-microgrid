/*
 * ---------------------------------------------------------------------------
 * File        : ReservationService.cs
 * Project     : Smart Solar Microgrid Trading System — SE4040 Assignment 1
 * Author      : <Your Full Name> (<IT Number>)
 * Created     : 2026-09-19
 * Description : The reservation engine. Every rule the assignment states about
 *               energy slot bookings is enforced in this one class, so the
 *               React web application and the Android application are held to
 *               identical conditions. Neither client contains any of it.
 *
 * Business    : BR-1  A booking must be within 7 days and in the future.
 *               BR-2  A change requires at least 12 hours' notice.
 *               BR-3  A cancellation requires at least 12 hours' notice.
 *               BR-8  A QR token is issued only on approval, and a scanned
 *                     token is verified here rather than on the device.
 *               BR-9  A window cannot be overbooked.
 *
 * Consistency : MongoDB gives us atomic updates per document but no multi
 *               document transaction on a free tier cluster. A booking touches
 *               two documents: the slot's counter and the reservation itself.
 *               The order below is deliberate — capacity is claimed FIRST, and
 *               released again if the reservation cannot be written. Doing it
 *               the other way round would allow a booking to exist that was
 *               never counted, which would silently break BR-9 and let the
 *               window be overbooked.
 *
 * SOLID       : Single Responsibility — reservation rules only. Capacity
 *               arithmetic belongs to the slot repository, token generation to
 *               IQrTokenGenerator, and identity to ICurrentUser.
 * ---------------------------------------------------------------------------
 */

using System.Globalization;
using SolarMicrogrid.Api.Common;
using SolarMicrogrid.Api.Dtos.Reservations;
using SolarMicrogrid.Api.Mappings;
using SolarMicrogrid.Api.Models;
using SolarMicrogrid.Api.Models.Enums;
using SolarMicrogrid.Api.Repositories;
using SolarMicrogrid.Api.Security;

namespace SolarMicrogrid.Api.Services;

/// <inheritdoc cref="IReservationService" />
public sealed partial class ReservationService : IReservationService
{
    private const string ReservationNotFoundMessage = "No reservation found with that identifier.";
    private const string NotYoursMessage = "You may only act on your own reservations.";

    private readonly IReservationRepository _reservationRepository;
    private readonly ISlotRepository _slotRepository;
    private readonly IStationRepository _stationRepository;
    private readonly IUserRepository _userRepository;
    private readonly IQrTokenGenerator _qrTokenGenerator;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<ReservationService> _logger;

    /// <summary>Receives its collaborators from the DI container.</summary>
    public ReservationService(
        IReservationRepository reservationRepository,
        ISlotRepository slotRepository,
        IStationRepository stationRepository,
        IUserRepository userRepository,
        IQrTokenGenerator qrTokenGenerator,
        ICurrentUser currentUser,
        ILogger<ReservationService> logger)
    {
        _reservationRepository = reservationRepository ?? throw new ArgumentNullException(nameof(reservationRepository));
        _slotRepository = slotRepository ?? throw new ArgumentNullException(nameof(slotRepository));
        _stationRepository = stationRepository ?? throw new ArgumentNullException(nameof(stationRepository));
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _qrTokenGenerator = qrTokenGenerator ?? throw new ArgumentNullException(nameof(qrTokenGenerator));
        _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<ServiceResult<IReadOnlyList<ReservationResponseDto>>> SearchAsync(
        string? prosumerNic,
        ReservationStatus? status,
        DateTime? fromUtc,
        DateTime? toUtc,
        string? stationId,
        CancellationToken cancellationToken = default)
    {
        // A prosumer sees only their own history, whatever NIC they ask for.
        // Overriding the filter rather than rejecting the request means a
        // prosumer cannot probe for other people's bookings at all.
        string? effectiveNic = _currentUser.Role == UserRole.Prosumer
            ? _currentUser.Nic
            : prosumerNic;

        IReadOnlyList<EnergyReservation> reservations = await _reservationRepository
            .SearchAsync(effectiveNic, status, fromUtc, toUtc, stationId, cancellationToken)
            .ConfigureAwait(false);

        IReadOnlyList<ReservationResponseDto> dtos = await ToDtosAsync(reservations, cancellationToken)
            .ConfigureAwait(false);

        return ServiceResult.Success(dtos);
    }

    /// <inheritdoc />
    public async Task<ServiceResult<IReadOnlyList<ReservationResponseDto>>> GetPendingAsync(
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<EnergyReservation> pending = await _reservationRepository
            .FindAsync(reservation => reservation.Status == ReservationStatus.Pending, cancellationToken)
            .ConfigureAwait(false);

        // Soonest booking first: the one happening next needs approving first.
        List<EnergyReservation> ordered = pending
            .OrderBy(reservation => reservation.ReservationDateTime)
            .ToList();

        IReadOnlyList<ReservationResponseDto> dtos = await ToDtosAsync(ordered, cancellationToken)
            .ConfigureAwait(false);

        return ServiceResult.Success(dtos);
    }

    /// <inheritdoc />
    public async Task<ServiceResult<ReservationResponseDto>> GetByIdAsync(
        string id,
        CancellationToken cancellationToken = default)
    {
        EnergyReservation? reservation = await _reservationRepository
            .GetByIdAsync(id, cancellationToken)
            .ConfigureAwait(false);

        if (reservation is null)
        {
            return ServiceResult.Failure<ReservationResponseDto>(
                ServiceErrorType.NotFound, ReservationNotFoundMessage);
        }

        if (!CallerOwnsOrIsStaff(reservation.ProsumerNic))
        {
            return ServiceResult.Failure<ReservationResponseDto>(ServiceErrorType.Forbidden, NotYoursMessage);
        }

        return ServiceResult.Success(await ToDtoAsync(reservation, cancellationToken).ConfigureAwait(false));
    }

    /// <inheritdoc />
    public async Task<ServiceResult<ReservationResponseDto>> CreateAsync(
        CreateReservationRequestDto request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        // Whose booking is this? A prosumer always books for themselves; only
        // staff may name someone else, and that is how a counter booking works.
        string? targetNic = _currentUser.Role == UserRole.Prosumer
            ? _currentUser.Nic
            : request.ProsumerNic?.Trim();

        if (string.IsNullOrWhiteSpace(targetNic))
        {
            return ServiceResult.Failure<ReservationResponseDto>(
                ServiceErrorType.Validation,
                "A prosumer NIC is required when booking on someone's behalf.");
        }

        User? prosumer = await _userRepository.GetByNicAsync(targetNic, cancellationToken).ConfigureAwait(false);

        if (prosumer is null || prosumer.Role != UserRole.Prosumer)
        {
            return ServiceResult.Failure<ReservationResponseDto>(
                ServiceErrorType.NotFound, "No prosumer found with that NIC.");
        }

        // A suspended account should not be able to take capacity at a node.
        if (prosumer.Status != AccountStatus.Active)
        {
            return ServiceResult.Failure<ReservationResponseDto>(
                ServiceErrorType.Forbidden,
                "This prosumer account is not active and cannot make reservations.");
        }

        EnergyBookingSlot? slot = await _slotRepository
            .GetByIdAsync(request.SlotId, cancellationToken)
            .ConfigureAwait(false);

        if (slot is null)
        {
            return ServiceResult.Failure<ReservationResponseDto>(
                ServiceErrorType.NotFound, "No booking window found with that identifier.");
        }

        SolarStationInfo? station = await _stationRepository
            .GetByIdAsync(slot.StationId, cancellationToken)
            .ConfigureAwait(false);

        if (station is null || !station.IsActive)
        {
            return ServiceResult.Failure<ReservationResponseDto>(
                ServiceErrorType.Conflict, "This microgrid node is not currently in service.");
        }

        DateTime slotStart = slot.SlotStartUtc();
        DateTime now = DateTime.UtcNow;

        // BR-1. The time comes from the chosen window, not from the request, so
        // a caller cannot claim a booking is at a time the node is not open.
        if (!BookingRules.IsWithinBookingWindow(slotStart, now))
        {
            return ServiceResult.Failure<ReservationResponseDto>(
                ServiceErrorType.Conflict,
                slotStart <= now
                    ? "That booking window has already passed."
                    : $"Reservations must be within {BookingRules.MaximumBookingWindowDays} days. " +
                      $"That window is {(slotStart - now).TotalDays:F1} days away.");
        }

        // BR-9, claimed atomically. See the Consistency note in the file header
        // for why capacity is taken BEFORE the reservation is written.
        bool capacityTaken = await _slotRepository
            .TryReserveCapacityAsync(slot.Id ?? string.Empty, cancellationToken)
            .ConfigureAwait(false);

        if (!capacityTaken)
        {
            return ServiceResult.Failure<ReservationResponseDto>(
                ServiceErrorType.Conflict,
                "That booking window is full or has been closed. Please choose another time.");
        }

        EnergyReservation reservation = new()
        {
            ReservationNo = BuildReservationNumber(now),
            ProsumerNic = prosumer.Nic,
            StationId = slot.StationId,
            SlotId = slot.Id ?? string.Empty,
            ReservationDateTime = slotStart,
            EnergyKwh = request.EnergyKwh,
            Direction = Enum.Parse<EnergyDirection>(request.Direction, ignoreCase: false),
            Status = ReservationStatus.Pending,
        };

        try
        {
            EnergyReservation created = await _reservationRepository
                .InsertAsync(reservation, cancellationToken)
                .ConfigureAwait(false);

            LogReservationCreated(created.ReservationNo, prosumer.Nic, station.StationCode);

            return ServiceResult.Success(
                await ToDtoAsync(created, cancellationToken).ConfigureAwait(false));
        }
        catch (Exception ex)
        {
            // Compensating action. The place was claimed a moment ago but the
            // booking could not be stored, so the place is given back. Without
            // this the window would leak capacity on every failed write and
            // would eventually refuse bookings it could actually honour.
            await _slotRepository
                .ReleaseCapacityAsync(slot.Id ?? string.Empty, CancellationToken.None)
                .ConfigureAwait(false);

            LogCapacityReleasedAfterFailure(slot.Id ?? string.Empty, ex);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<ServiceResult<ReservationResponseDto>> UpdateAsync(
        string id,
        UpdateReservationRequestDto request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        EnergyReservation? reservation = await _reservationRepository
            .GetByIdAsync(id, cancellationToken)
            .ConfigureAwait(false);

        if (reservation is null)
        {
            return ServiceResult.Failure<ReservationResponseDto>(
                ServiceErrorType.NotFound, ReservationNotFoundMessage);
        }

        if (!CallerOwnsOrIsStaff(reservation.ProsumerNic))
        {
            return ServiceResult.Failure<ReservationResponseDto>(ServiceErrorType.Forbidden, NotYoursMessage);
        }

        if (reservation.Status is ReservationStatus.Cancelled or ReservationStatus.Completed)
        {
            return ServiceResult.Failure<ReservationResponseDto>(
                ServiceErrorType.Conflict,
                $"A {reservation.Status.ToString().ToLowerInvariant()} reservation cannot be changed.");
        }

        DateTime now = DateTime.UtcNow;

        // BR-2, measured against the booking's CURRENT time. Checking the new
        // time instead would let someone inside the notice period escape the
        // rule simply by moving the booking further into the future.
        if (!BookingRules.IsOutsideNoticePeriod(reservation.ReservationDateTime, now))
        {
            return ServiceResult.Failure<ReservationResponseDto>(
                ServiceErrorType.Conflict,
                $"Reservations can only be changed at least {BookingRules.MinimumChangeNoticeHours} hours " +
                $"beforehand. This one is in {BookingRules.HoursUntil(reservation.ReservationDateTime, now):F1} hours.");
        }

        string originalSlotId = reservation.SlotId;
        bool movingWindow = !string.IsNullOrWhiteSpace(request.SlotId)
            && !string.Equals(request.SlotId, originalSlotId, StringComparison.Ordinal);

        if (movingWindow)
        {
            ServiceResult<ReservationResponseDto>? moveError =
                await MoveToSlotAsync(reservation, request.SlotId!, now, cancellationToken).ConfigureAwait(false);

            if (moveError is not null)
            {
                return moveError;
            }
        }

        reservation.EnergyKwh = request.EnergyKwh;
        reservation.Direction = Enum.Parse<EnergyDirection>(request.Direction, ignoreCase: false);

        // Changing a booking withdraws its approval: the details an officer
        // approved are no longer the details on file, so it must be reviewed
        // again. Any QR token already issued is retired at the same time, so a
        // code printed before the change cannot still be scanned.
        if (reservation.Status == ReservationStatus.Approved)
        {
            reservation.Status = ReservationStatus.Pending;
            reservation.QrToken = null;
            reservation.QrIssuedAt = null;
            reservation.ApprovedBy = null;
        }

        await _reservationRepository.UpdateAsync(reservation, cancellationToken).ConfigureAwait(false);
        LogReservationUpdated(reservation.ReservationNo);

        return ServiceResult.Success(
            await ToDtoAsync(reservation, cancellationToken).ConfigureAwait(false));
    }

    /// <inheritdoc />
    public async Task<ServiceResult<ReservationResponseDto>> CancelAsync(
        string id,
        CancelReservationRequestDto request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        EnergyReservation? reservation = await _reservationRepository
            .GetByIdAsync(id, cancellationToken)
            .ConfigureAwait(false);

        if (reservation is null)
        {
            return ServiceResult.Failure<ReservationResponseDto>(
                ServiceErrorType.NotFound, ReservationNotFoundMessage);
        }

        if (!CallerOwnsOrIsStaff(reservation.ProsumerNic))
        {
            return ServiceResult.Failure<ReservationResponseDto>(ServiceErrorType.Forbidden, NotYoursMessage);
        }

        if (reservation.Status == ReservationStatus.Cancelled)
        {
            return ServiceResult.Failure<ReservationResponseDto>(
                ServiceErrorType.Conflict, "This reservation has already been cancelled.");
        }

        if (reservation.Status == ReservationStatus.Completed)
        {
            return ServiceResult.Failure<ReservationResponseDto>(
                ServiceErrorType.Conflict, "A completed energy transfer cannot be cancelled.");
        }

        DateTime now = DateTime.UtcNow;

        // BR-3.
        if (!BookingRules.IsOutsideNoticePeriod(reservation.ReservationDateTime, now))
        {
            return ServiceResult.Failure<ReservationResponseDto>(
                ServiceErrorType.Conflict,
                $"Reservations can only be cancelled at least {BookingRules.MinimumChangeNoticeHours} hours " +
                $"beforehand. This one is in {BookingRules.HoursUntil(reservation.ReservationDateTime, now):F1} hours.");
        }

        reservation.Status = ReservationStatus.Cancelled;
        reservation.CancelReason = request.Reason.Trim();

        // A cancelled booking must not keep holding a QR token, or the code
        // could still be scanned at the node.
        reservation.QrToken = null;
        reservation.QrIssuedAt = null;

        await _reservationRepository.UpdateAsync(reservation, cancellationToken).ConfigureAwait(false);

        // Give the place back so someone else can book it.
        await _slotRepository.ReleaseCapacityAsync(reservation.SlotId, cancellationToken).ConfigureAwait(false);

        LogReservationCancelled(reservation.ReservationNo, reservation.CancelReason);

        return ServiceResult.Success(
            await ToDtoAsync(reservation, cancellationToken).ConfigureAwait(false));
    }

    /// <inheritdoc />
    public async Task<ServiceResult<ReservationResponseDto>> ApproveAsync(
        string id,
        CancellationToken cancellationToken = default)
    {
        EnergyReservation? reservation = await _reservationRepository
            .GetByIdAsync(id, cancellationToken)
            .ConfigureAwait(false);

        if (reservation is null)
        {
            return ServiceResult.Failure<ReservationResponseDto>(
                ServiceErrorType.NotFound, ReservationNotFoundMessage);
        }

        if (reservation.Status != ReservationStatus.Pending)
        {
            return ServiceResult.Failure<ReservationResponseDto>(
                ServiceErrorType.Conflict,
                $"Only a pending reservation can be approved. This one is {reservation.Status}.");
        }

        if (reservation.ReservationDateTime <= DateTime.UtcNow)
        {
            return ServiceResult.Failure<ReservationResponseDto>(
                ServiceErrorType.Conflict, "That booking time has already passed.");
        }

        // BR-8. The token is created here and nowhere else, so a booking cannot
        // carry a scannable code until a human has approved it.
        reservation.Status = ReservationStatus.Approved;
        reservation.QrToken = _qrTokenGenerator.Generate();
        reservation.QrIssuedAt = DateTime.UtcNow;
        reservation.ApprovedBy = _currentUser.UserId;

        await _reservationRepository.UpdateAsync(reservation, cancellationToken).ConfigureAwait(false);
        LogReservationApproved(reservation.ReservationNo, _currentUser.UserId ?? "system");

        return ServiceResult.Success(
            await ToDtoAsync(reservation, cancellationToken).ConfigureAwait(false));
    }

    /// <inheritdoc />
    public async Task<ServiceResult<QrTokenResponseDto>> GetQrTokenAsync(
        string id,
        CancellationToken cancellationToken = default)
    {
        EnergyReservation? reservation = await _reservationRepository
            .GetByIdAsync(id, cancellationToken)
            .ConfigureAwait(false);

        if (reservation is null)
        {
            return ServiceResult.Failure<QrTokenResponseDto>(
                ServiceErrorType.NotFound, ReservationNotFoundMessage);
        }

        // Only the owner may fetch the token. Staff are deliberately excluded:
        // an operator has no reason to obtain a prosumer's code, and allowing
        // it would make the code forgeable by anyone with a staff account.
        if (!string.Equals(_currentUser.Nic, reservation.ProsumerNic, StringComparison.Ordinal))
        {
            return ServiceResult.Failure<QrTokenResponseDto>(
                ServiceErrorType.Forbidden, "You may only view the QR code for your own reservation.");
        }

        if (reservation.Status != ReservationStatus.Approved || string.IsNullOrWhiteSpace(reservation.QrToken))
        {
            return ServiceResult.Failure<QrTokenResponseDto>(
                ServiceErrorType.Conflict,
                "A QR code is available once the reservation has been approved.");
        }

        SolarStationInfo? station = await _stationRepository
            .GetByIdAsync(reservation.StationId, cancellationToken)
            .ConfigureAwait(false);

        return ServiceResult.Success(new QrTokenResponseDto(
            ReservationNo: reservation.ReservationNo,
            QrToken: reservation.QrToken,
            IssuedAtUtc: reservation.QrIssuedAt ?? reservation.UpdatedAt,
            ReservationDateTime: reservation.ReservationDateTime,
            StationName: station?.Name ?? string.Empty));
    }

    /// <inheritdoc />
    public async Task<ServiceResult<ReservationResponseDto>> VerifyQrAsync(
        VerifyQrRequestDto request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        EnergyReservation? reservation = await _reservationRepository
            .GetByQrTokenAsync(request.QrToken.Trim(), cancellationToken)
            .ConfigureAwait(false);

        // Every failure below returns the same message. Telling the operator
        // which check failed would also tell someone presenting a forged code
        // how close they were, and the operator's action is the same either
        // way: refuse the transfer.
        if (reservation is null)
        {
            LogQrVerificationFailed("token not found");
            return ServiceResult.Failure<ReservationResponseDto>(
                ServiceErrorType.NotFound, "This QR code is not valid.");
        }

        if (reservation.Status != ReservationStatus.Approved)
        {
            LogQrVerificationFailed($"status was {reservation.Status}");
            return ServiceResult.Failure<ReservationResponseDto>(
                ServiceErrorType.Conflict, "This QR code is not valid.");
        }

        LogQrVerified(reservation.ReservationNo, _currentUser.UserId ?? "unknown");

        return ServiceResult.Success(
            await ToDtoAsync(reservation, cancellationToken).ConfigureAwait(false));
    }

    /// <inheritdoc />
    public async Task<ServiceResult<ReservationResponseDto>> CompleteAsync(
        string id,
        CancellationToken cancellationToken = default)
    {
        EnergyReservation? reservation = await _reservationRepository
            .GetByIdAsync(id, cancellationToken)
            .ConfigureAwait(false);

        if (reservation is null)
        {
            return ServiceResult.Failure<ReservationResponseDto>(
                ServiceErrorType.NotFound, ReservationNotFoundMessage);
        }

        if (reservation.Status != ReservationStatus.Approved)
        {
            return ServiceResult.Failure<ReservationResponseDto>(
                ServiceErrorType.Conflict,
                $"Only an approved reservation can be completed. This one is {reservation.Status}.");
        }

        reservation.Status = ReservationStatus.Completed;
        reservation.CompletedBy = _currentUser.UserId;
        reservation.CompletedAt = DateTime.UtcNow;

        // The token is retired on completion, so the same QR code cannot be
        // presented a second time to claim another transfer.
        reservation.QrToken = null;

        await _reservationRepository.UpdateAsync(reservation, cancellationToken).ConfigureAwait(false);
        LogReservationCompleted(reservation.ReservationNo, _currentUser.UserId ?? "system");

        return ServiceResult.Success(
            await ToDtoAsync(reservation, cancellationToken).ConfigureAwait(false));
    }

    /// <summary>
    /// Moves a booking to a different window, applying BR-1 and BR-9 to the new
    /// one and giving back the place held in the old one. Returns null on
    /// success, or the failure to hand back to the caller.
    /// </summary>
    private async Task<ServiceResult<ReservationResponseDto>?> MoveToSlotAsync(
        EnergyReservation reservation,
        string newSlotId,
        DateTime now,
        CancellationToken cancellationToken)
    {
        EnergyBookingSlot? newSlot = await _slotRepository
            .GetByIdAsync(newSlotId, cancellationToken)
            .ConfigureAwait(false);

        if (newSlot is null)
        {
            return ServiceResult.Failure<ReservationResponseDto>(
                ServiceErrorType.NotFound, "No booking window found with that identifier.");
        }

        DateTime newStart = newSlot.SlotStartUtc();

        // BR-1 again: the new time must also sit inside the seven day window.
        if (!BookingRules.IsWithinBookingWindow(newStart, now))
        {
            return ServiceResult.Failure<ReservationResponseDto>(
                ServiceErrorType.Conflict,
                $"The new window must be in the future and within {BookingRules.MaximumBookingWindowDays} days.");
        }

        // Claim the new place before releasing the old one. If this is done the
        // other way round and the new window turns out to be full, the booking
        // is left holding nothing at all.
        bool taken = await _slotRepository
            .TryReserveCapacityAsync(newSlotId, cancellationToken)
            .ConfigureAwait(false);

        if (!taken)
        {
            return ServiceResult.Failure<ReservationResponseDto>(
                ServiceErrorType.Conflict,
                "That booking window is full or has been closed. Please choose another time.");
        }

        string previousSlotId = reservation.SlotId;

        reservation.SlotId = newSlotId;
        reservation.StationId = newSlot.StationId;
        reservation.ReservationDateTime = newStart;

        await _slotRepository.ReleaseCapacityAsync(previousSlotId, cancellationToken).ConfigureAwait(false);

        return null;
    }

    /// <summary>
    /// True when the caller is staff, or is the prosumer who owns the booking.
    /// </summary>
    private bool CallerOwnsOrIsStaff(string ownerNic)
    {
        if (_currentUser.Role is null)
        {
            return false;
        }

        if (_currentUser.Role != UserRole.Prosumer)
        {
            return true;
        }

        return string.Equals(_currentUser.Nic, ownerNic, StringComparison.Ordinal);
    }

    /// <summary>
    /// Builds a readable booking reference, for example "RSV-20260919-4F2A".
    /// The random suffix keeps two bookings made in the same second distinct.
    /// </summary>
    private static string BuildReservationNumber(DateTime nowUtc)
    {
        string datePart = nowUtc.ToString("yyyyMMdd", CultureInfo.InvariantCulture);
        string suffix = Guid.NewGuid().ToString("N")[..4].ToUpperInvariant();
        return $"RSV-{datePart}-{suffix}";
    }

    /// <summary>Builds the DTO for one booking, loading its node and prosumer.</summary>
    private async Task<ReservationResponseDto> ToDtoAsync(
        EnergyReservation reservation,
        CancellationToken cancellationToken)
    {
        SolarStationInfo? station = await _stationRepository
            .GetByIdAsync(reservation.StationId, cancellationToken)
            .ConfigureAwait(false);

        User? prosumer = await _userRepository
            .GetByNicAsync(reservation.ProsumerNic, cancellationToken)
            .ConfigureAwait(false);

        return reservation.ToResponseDto(station, prosumer?.FullName ?? string.Empty, DateTime.UtcNow);
    }

    /// <summary>
    /// Builds DTOs for a list of bookings, loading each node and prosumer once.
    /// </summary>
    /// <remarks>
    /// Calling ToDtoAsync in a loop would query the database twice per booking —
    /// the N+1 query problem. Here the distinct nodes and prosumers are fetched
    /// once each and reused, so a hundred bookings across four nodes costs a
    /// handful of queries rather than two hundred.
    /// </remarks>
    private async Task<IReadOnlyList<ReservationResponseDto>> ToDtosAsync(
        IReadOnlyList<EnergyReservation> reservations,
        CancellationToken cancellationToken)
    {
        if (reservations.Count == 0)
        {
            return [];
        }

        Dictionary<string, SolarStationInfo> stations = [];
        foreach (string stationId in reservations.Select(r => r.StationId).Distinct(StringComparer.Ordinal))
        {
            SolarStationInfo? station = await _stationRepository
                .GetByIdAsync(stationId, cancellationToken)
                .ConfigureAwait(false);

            if (station is not null)
            {
                stations[stationId] = station;
            }
        }

        Dictionary<string, string> prosumerNames = [];
        foreach (string nic in reservations.Select(r => r.ProsumerNic).Distinct(StringComparer.Ordinal))
        {
            User? prosumer = await _userRepository.GetByNicAsync(nic, cancellationToken).ConfigureAwait(false);
            prosumerNames[nic] = prosumer?.FullName ?? string.Empty;
        }

        DateTime now = DateTime.UtcNow;

        return reservations
            .Select(reservation => reservation.ToResponseDto(
                stations.GetValueOrDefault(reservation.StationId),
                prosumerNames.GetValueOrDefault(reservation.ProsumerNic, string.Empty),
                now))
            .ToList();
    }

    // -----------------------------------------------------------------------
    // Source generated log methods. See the note in MongoContext.cs.
    // -----------------------------------------------------------------------

    [LoggerMessage(
        EventId = 6001,
        Level = LogLevel.Information,
        Message = "Reservation {ReservationNo} created for prosumer {Nic} at node {StationCode}.")]
    private partial void LogReservationCreated(string reservationNo, string nic, string stationCode);

    [LoggerMessage(
        EventId = 6002,
        Level = LogLevel.Information,
        Message = "Reservation {ReservationNo} updated.")]
    private partial void LogReservationUpdated(string reservationNo);

    [LoggerMessage(
        EventId = 6003,
        Level = LogLevel.Information,
        Message = "Reservation {ReservationNo} cancelled. Reason: {Reason}")]
    private partial void LogReservationCancelled(string reservationNo, string reason);

    [LoggerMessage(
        EventId = 6004,
        Level = LogLevel.Information,
        Message = "Reservation {ReservationNo} approved by {ActorId}; QR token issued.")]
    private partial void LogReservationApproved(string reservationNo, string actorId);

    [LoggerMessage(
        EventId = 6005,
        Level = LogLevel.Information,
        Message = "QR verified for reservation {ReservationNo} by operator {ActorId}.")]
    private partial void LogQrVerified(string reservationNo, string actorId);

    [LoggerMessage(
        EventId = 6006,
        Level = LogLevel.Warning,
        Message = "QR verification refused: {Reason}")]
    private partial void LogQrVerificationFailed(string reason);

    [LoggerMessage(
        EventId = 6007,
        Level = LogLevel.Information,
        Message = "Reservation {ReservationNo} completed by operator {ActorId}.")]
    private partial void LogReservationCompleted(string reservationNo, string actorId);

    [LoggerMessage(
        EventId = 6008,
        Level = LogLevel.Error,
        Message = "Reservation write failed after capacity was claimed; released the place in window {SlotId}.")]
    private partial void LogCapacityReleasedAfterFailure(string slotId, Exception exception);
}
