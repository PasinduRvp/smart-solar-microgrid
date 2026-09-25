/*
 * ---------------------------------------------------------------------------
 * File        : SlotService.cs
 * Project     : Smart Solar Microgrid Trading System — SE4040 Assignment 1
 * Author      : <Your Full Name> (<IT Number>)
 * Created     : 2026-09-19
 * Description : Rules for creating and maintaining bookable energy trading
 *               windows. Generation reads the node's weekly schedule and
 *               divides each open day into windows of the requested length, so
 *               a node's opening hours and its bookable times cannot disagree.
 *
 * Business    : BR-9  A window cannot be overbooked, and its capacity can never
 *                     be reduced below the bookings already taken.
 *               The generator is capped at seven days because BR-1 only permits
 *               a reservation within seven days; windows beyond that could
 *               never be booked.
 *
 * SOLID       : Single Responsibility — window rules only.
 *               Dependency Inversion — all collaborators are interfaces.
 * ---------------------------------------------------------------------------
 */

using System.Globalization;
using SolarMicrogrid.Api.Common;
using SolarMicrogrid.Api.Dtos.Slots;
using SolarMicrogrid.Api.Mappings;
using SolarMicrogrid.Api.Models;
using SolarMicrogrid.Api.Repositories;

namespace SolarMicrogrid.Api.Services;

/// <inheritdoc cref="ISlotService" />
public sealed partial class SlotService : ISlotService
{
    private const string SlotNotFoundMessage = "No booking window found with that identifier.";

    /// <summary>Days ahead the generator will produce windows for, matching BR-1.</summary>
    private const int MaximumGenerationDays = 7;

    private readonly ISlotRepository _slotRepository;
    private readonly IStationRepository _stationRepository;
    private readonly IReservationRepository _reservationRepository;
    private readonly ILogger<SlotService> _logger;

    /// <summary>Receives its collaborators from the DI container.</summary>
    public SlotService(
        ISlotRepository slotRepository,
        IStationRepository stationRepository,
        IReservationRepository reservationRepository,
        ILogger<SlotService> logger)
    {
        _slotRepository = slotRepository ?? throw new ArgumentNullException(nameof(slotRepository));
        _stationRepository = stationRepository ?? throw new ArgumentNullException(nameof(stationRepository));
        _reservationRepository = reservationRepository ?? throw new ArgumentNullException(nameof(reservationRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<ServiceResult<IReadOnlyList<SlotResponseDto>>> GetForStationAsync(
        string stationId,
        DateTime? slotDate,
        CancellationToken cancellationToken = default)
    {
        SolarStationInfo? station = await _stationRepository
            .GetByIdAsync(stationId, cancellationToken)
            .ConfigureAwait(false);

        if (station is null)
        {
            return ServiceResult.Failure<IReadOnlyList<SlotResponseDto>>(
                ServiceErrorType.NotFound, "No microgrid node found with that identifier.");
        }

        IReadOnlyList<EnergyBookingSlot> slots;

        if (slotDate is null)
        {
            // No date given: show the whole bookable horizon, which is the same
            // seven days BR-1 allows a reservation to fall within.
            DateTime today = DateTime.UtcNow.Date;
            slots = await _slotRepository
                .GetByStationBetweenAsync(stationId, today, today.AddDays(MaximumGenerationDays), cancellationToken)
                .ConfigureAwait(false);
        }
        else
        {
            slots = await _slotRepository
                .GetByStationAndDateAsync(stationId, slotDate.Value, cancellationToken)
                .ConfigureAwait(false);
        }

        return ServiceResult.Success(slots.ToResponseDtos());
    }

    /// <inheritdoc />
    public async Task<ServiceResult<SlotResponseDto>> GetByIdAsync(
        string id,
        CancellationToken cancellationToken = default)
    {
        EnergyBookingSlot? slot = await _slotRepository
            .GetByIdAsync(id, cancellationToken)
            .ConfigureAwait(false);

        return slot is null
            ? ServiceResult.Failure<SlotResponseDto>(ServiceErrorType.NotFound, SlotNotFoundMessage)
            : ServiceResult.Success(slot.ToResponseDto());
    }

    /// <inheritdoc />
    public async Task<ServiceResult<IReadOnlyList<SlotResponseDto>>> GenerateAsync(
        string stationId,
        GenerateSlotsRequestDto request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        SolarStationInfo? station = await _stationRepository
            .GetByIdAsync(stationId, cancellationToken)
            .ConfigureAwait(false);

        if (station is null)
        {
            return ServiceResult.Failure<IReadOnlyList<SlotResponseDto>>(
                ServiceErrorType.NotFound, "No microgrid node found with that identifier.");
        }

        if (!station.IsActive)
        {
            return ServiceResult.Failure<IReadOnlyList<SlotResponseDto>>(
                ServiceErrorType.Conflict,
                "Booking windows cannot be generated for a deactivated node.");
        }

        if (station.Schedule.Count == 0)
        {
            return ServiceResult.Failure<IReadOnlyList<SlotResponseDto>>(
                ServiceErrorType.Conflict,
                "Set the node's weekly schedule before generating booking windows.");
        }

        DateTime startDate = (request.StartDate ?? DateTime.UtcNow).Date;

        // Generating into the past would create windows that are already
        // unbookable, which only clutters the prosumer's list.
        if (startDate < DateTime.UtcNow.Date)
        {
            return ServiceResult.Failure<IReadOnlyList<SlotResponseDto>>(
                ServiceErrorType.Validation, "Start date cannot be in the past.");
        }

        List<EnergyBookingSlot> generated = [];

        for (int dayOffset = 0; dayOffset < request.NumberOfDays; dayOffset++)
        {
            DateTime day = startDate.AddDays(dayOffset);

            // Does the node open on this weekday at all?
            ScheduleEntry? hours = station.Schedule
                .FirstOrDefault(entry => entry.DayOfWeek == day.DayOfWeek);

            if (hours is null)
            {
                continue;
            }

            // Skip days that already have windows, so running the generator
            // twice does not produce duplicates. This makes the operation safe
            // to repeat, which matters when an officer re-runs it after
            // extending the schedule.
            IReadOnlyList<EnergyBookingSlot> existing = await _slotRepository
                .GetByStationAndDateAsync(stationId, day, cancellationToken)
                .ConfigureAwait(false);

            if (existing.Count > 0)
            {
                continue;
            }

            generated.AddRange(BuildWindowsForDay(stationId, day, hours, request));
        }

        if (generated.Count == 0)
        {
            return ServiceResult.Failure<IReadOnlyList<SlotResponseDto>>(
                ServiceErrorType.Conflict,
                "No windows were generated. Every day in the range is either closed in the schedule " +
                "or already has windows.");
        }

        await _slotRepository.InsertManyAsync(generated, cancellationToken).ConfigureAwait(false);
        LogSlotsGenerated(station.StationCode, generated.Count);

        return ServiceResult.Success(generated.ToResponseDtos());
    }

    /// <inheritdoc />
    public async Task<ServiceResult<SlotResponseDto>> UpdateAvailabilityAsync(
        string id,
        UpdateSlotAvailabilityRequestDto request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        EnergyBookingSlot? slot = await _slotRepository
            .GetByIdAsync(id, cancellationToken)
            .ConfigureAwait(false);

        if (slot is null)
        {
            return ServiceResult.Failure<SlotResponseDto>(ServiceErrorType.NotFound, SlotNotFoundMessage);
        }

        if (request.TotalCapacitySlots is int newCapacity)
        {
            // BR-9 in its second form. Reducing capacity below the bookings
            // already taken would leave the window overbooked, with prosumers
            // holding reservations the node cannot honour.
            if (newCapacity < slot.BookedCount)
            {
                return ServiceResult.Failure<SlotResponseDto>(
                    ServiceErrorType.Conflict,
                    $"Capacity cannot be set to {newCapacity}: {slot.BookedCount} booking(s) " +
                    "have already been taken for this window.");
            }

            slot.TotalCapacitySlots = newCapacity;
        }

        // Closing a window that people have already booked would strand them,
        // so the existing bookings must be dealt with first.
        if (!request.IsAvailable && slot.IsAvailable)
        {
            long activeBookings = await _reservationRepository
                .CountActiveForSlotAsync(slot.Id ?? string.Empty, cancellationToken)
                .ConfigureAwait(false);

            if (activeBookings > 0)
            {
                return ServiceResult.Failure<SlotResponseDto>(
                    ServiceErrorType.Conflict,
                    $"This window cannot be closed: {activeBookings} active reservation(s) hold it. " +
                    "Cancel them first.");
            }
        }

        slot.IsAvailable = request.IsAvailable;

        await _slotRepository.UpdateAsync(slot, cancellationToken).ConfigureAwait(false);
        LogSlotAvailabilityChanged(slot.Id ?? string.Empty, slot.IsAvailable);

        return ServiceResult.Success(slot.ToResponseDto());
    }

    /// <summary>
    /// Divides one open day into consecutive windows of the requested length.
    /// </summary>
    /// <remarks>
    /// A partial window at the end of the day is not created: if the node
    /// closes at 18:00 and 20 minutes remain, no 20 minute window is produced,
    /// because a trading window shorter than advertised would mislead the
    /// prosumer who booked it.
    /// </remarks>
    private static List<EnergyBookingSlot> BuildWindowsForDay(
        string stationId,
        DateTime day,
        ScheduleEntry hours,
        GenerateSlotsRequestDto request)
    {
        List<EnergyBookingSlot> windows = [];

        if (!TimeOnly.TryParseExact(hours.OpenTime, "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out TimeOnly open)
            || !TimeOnly.TryParseExact(hours.CloseTime, "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out TimeOnly close)
            || close <= open)
        {
            return windows;
        }

        TimeSpan duration = TimeSpan.FromMinutes(request.SlotDurationMinutes);
        TimeOnly cursor = open;

        while (cursor.Add(duration) <= close)
        {
            TimeOnly windowEnd = cursor.Add(duration);

            windows.Add(new EnergyBookingSlot
            {
                StationId = stationId,
                SlotDate = DateTime.SpecifyKind(day.Date, DateTimeKind.Utc),
                StartTime = cursor.ToString("HH:mm", CultureInfo.InvariantCulture),
                EndTime = windowEnd.ToString("HH:mm", CultureInfo.InvariantCulture),
                TotalCapacitySlots = request.CapacityPerSlot,
                BookedCount = 0,
                EnergyRatePerKwh = request.EnergyRatePerKwh,
                IsAvailable = true,
            });

            cursor = windowEnd;
        }

        return windows;
    }

    // -----------------------------------------------------------------------
    // Source generated log methods. See the note in MongoContext.cs.
    // -----------------------------------------------------------------------

    [LoggerMessage(
        EventId = 5101,
        Level = LogLevel.Information,
        Message = "Generated {SlotCount} booking window(s) for node {StationCode}.")]
    private partial void LogSlotsGenerated(string stationCode, int slotCount);

    [LoggerMessage(
        EventId = 5102,
        Level = LogLevel.Information,
        Message = "Booking window {SlotId} availability set to {IsAvailable}.")]
    private partial void LogSlotAvailabilityChanged(string slotId, bool isAvailable);
}
