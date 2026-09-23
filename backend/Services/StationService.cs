/*
 * ---------------------------------------------------------------------------
 * File        : StationService.cs
 * Project     : Smart Solar Microgrid Trading System — SE4040 Assignment 1
 * Author      : <Your Full Name> (<IT Number>)
 * Created     : 2026-09-19
 * Description : Every rule governing solar microgrid nodes. Called from the
 *               station management screens in the React back office and from
 *               the nearby nodes map in the Android application.
 *
 * Business    : BR-4  A node cannot be deactivated while it still has active
 *                     reservations. The refusal names how many are blocking it,
 *                     so the officer knows what to do next instead of being
 *                     told only that it failed.
 *
 * SOLID       : Single Responsibility — node rules only; slot generation lives
 *               in SlotService and reservation rules in ReservationService.
 *               Dependency Inversion — all three collaborators are interfaces.
 * ---------------------------------------------------------------------------
 */

using System.Globalization;
using SolarMicrogrid.Api.Common;
using SolarMicrogrid.Api.Dtos.Stations;
using SolarMicrogrid.Api.Mappings;
using SolarMicrogrid.Api.Models;
using SolarMicrogrid.Api.Repositories;

namespace SolarMicrogrid.Api.Services;

/// <inheritdoc cref="IStationService" />
public sealed partial class StationService : IStationService
{
    private const string StationNotFoundMessage = "No microgrid node found with that identifier.";

    /// <summary>Largest radius the nearby search will accept, in kilometres.</summary>
    private const double MaximumSearchRadiusKm = 500;

    private readonly IStationRepository _stationRepository;
    private readonly IReservationRepository _reservationRepository;
    private readonly ILogger<StationService> _logger;

    /// <summary>Receives its collaborators from the DI container.</summary>
    public StationService(
        IStationRepository stationRepository,
        IReservationRepository reservationRepository,
        ILogger<StationService> logger)
    {
        _stationRepository = stationRepository ?? throw new ArgumentNullException(nameof(stationRepository));
        _reservationRepository = reservationRepository ?? throw new ArgumentNullException(nameof(reservationRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<ServiceResult<IReadOnlyList<StationResponseDto>>> GetAllAsync(
        bool activeOnly,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<SolarStationInfo> stations = activeOnly
            ? await _stationRepository.GetActiveAsync(cancellationToken).ConfigureAwait(false)
            : await _stationRepository.GetAllAsync(cancellationToken).ConfigureAwait(false);

        IReadOnlyList<StationResponseDto> dtos = stations
            .OrderBy(station => station.StationCode, StringComparer.Ordinal)
            .ToResponseDtos();

        return ServiceResult.Success(dtos);
    }

    /// <inheritdoc />
    public async Task<ServiceResult<StationResponseDto>> GetByIdAsync(
        string id,
        CancellationToken cancellationToken = default)
    {
        SolarStationInfo? station = await _stationRepository
            .GetByIdAsync(id, cancellationToken)
            .ConfigureAwait(false);

        return station is null
            ? ServiceResult.Failure<StationResponseDto>(ServiceErrorType.NotFound, StationNotFoundMessage)
            : ServiceResult.Success(station.ToResponseDto());
    }

    /// <inheritdoc />
    public async Task<ServiceResult<IReadOnlyList<NearbyStationDto>>> GetNearbyAsync(
        double latitude,
        double longitude,
        double radiusKm,
        CancellationToken cancellationToken = default)
    {
        // Validated here rather than only on the DTO, because these arrive as
        // query string values and a service must be correct on its own terms.
        if (latitude is < -90 or > 90)
        {
            return ServiceResult.Failure<IReadOnlyList<NearbyStationDto>>(
                ServiceErrorType.Validation, "Latitude must be between -90 and 90.");
        }

        if (longitude is < -180 or > 180)
        {
            return ServiceResult.Failure<IReadOnlyList<NearbyStationDto>>(
                ServiceErrorType.Validation, "Longitude must be between -180 and 180.");
        }

        if (radiusKm is <= 0 or > MaximumSearchRadiusKm)
        {
            return ServiceResult.Failure<IReadOnlyList<NearbyStationDto>>(
                ServiceErrorType.Validation,
                $"Search radius must be greater than 0 and no more than {MaximumSearchRadiusKm} km.");
        }

        // Only nodes in service are offered: a prosumer should not be sent to a
        // node that has been taken offline.
        IReadOnlyList<SolarStationInfo> active = await _stationRepository
            .GetActiveAsync(cancellationToken)
            .ConfigureAwait(false);

        List<NearbyStationDto> nearby = active
            .Select(station => new
            {
                Station = station,
                DistanceKm = GeoDistance.KilometresBetween(
                    latitude,
                    longitude,
                    station.Location.Latitude,
                    station.Location.Longitude),
            })
            .Where(candidate => candidate.DistanceKm <= radiusKm)
            .OrderBy(candidate => candidate.DistanceKm)
            .Select(candidate => new NearbyStationDto(
                candidate.Station.ToResponseDto(),
                Math.Round(candidate.DistanceKm, 2)))
            .ToList();

        return ServiceResult.Success<IReadOnlyList<NearbyStationDto>>(nearby);
    }

    /// <inheritdoc />
    public async Task<ServiceResult<StationResponseDto>> CreateAsync(
        CreateStationRequestDto request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        string code = request.StationCode.Trim().ToUpperInvariant();

        SolarStationInfo? existing = await _stationRepository
            .GetByCodeAsync(code, cancellationToken)
            .ConfigureAwait(false);

        if (existing is not null)
        {
            return ServiceResult.Failure<StationResponseDto>(
                ServiceErrorType.Conflict, $"A microgrid node with code {code} already exists.");
        }

        ServiceResult<StationResponseDto>? scheduleError =
            ValidateSchedule<StationResponseDto>(request.Schedule);

        if (scheduleError is not null)
        {
            return scheduleError;
        }

        SolarStationInfo station = new()
        {
            StationCode = code,
            Name = request.Name.Trim(),
            Location = request.Location.ToEntity(),
            CapacityKwh = request.CapacityKwh,
            TotalBatterySlots = request.TotalBatterySlots,

            // A newly registered node starts with every slot free.
            AvailableBatterySlots = request.TotalBatterySlots,
            Schedule = request.Schedule.Select(entry => entry.ToEntity()).ToList(),
            IsActive = true,
        };

        SolarStationInfo created = await _stationRepository
            .InsertAsync(station, cancellationToken)
            .ConfigureAwait(false);

        LogStationCreated(created.StationCode, created.Id ?? string.Empty);
        return ServiceResult.Success(created.ToResponseDto());
    }

    /// <inheritdoc />
    public async Task<ServiceResult<StationResponseDto>> UpdateAsync(
        string id,
        UpdateStationRequestDto request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        SolarStationInfo? station = await _stationRepository
            .GetByIdAsync(id, cancellationToken)
            .ConfigureAwait(false);

        if (station is null)
        {
            return ServiceResult.Failure<StationResponseDto>(ServiceErrorType.NotFound, StationNotFoundMessage);
        }

        // Free slots can never exceed installed slots. Without this a typo
        // would let the node advertise capacity that does not physically exist.
        if (request.AvailableBatterySlots > request.TotalBatterySlots)
        {
            return ServiceResult.Failure<StationResponseDto>(
                ServiceErrorType.Validation,
                "Available battery slots cannot exceed the total number of battery slots installed.");
        }

        station.Name = request.Name.Trim();
        station.Location = request.Location.ToEntity();
        station.CapacityKwh = request.CapacityKwh;
        station.TotalBatterySlots = request.TotalBatterySlots;
        station.AvailableBatterySlots = request.AvailableBatterySlots;

        await _stationRepository.UpdateAsync(station, cancellationToken).ConfigureAwait(false);
        LogStationUpdated(station.StationCode);

        return ServiceResult.Success(station.ToResponseDto());
    }

    /// <inheritdoc />
    public async Task<ServiceResult<StationResponseDto>> UpdateScheduleAsync(
        string id,
        UpdateScheduleRequestDto request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        SolarStationInfo? station = await _stationRepository
            .GetByIdAsync(id, cancellationToken)
            .ConfigureAwait(false);

        if (station is null)
        {
            return ServiceResult.Failure<StationResponseDto>(ServiceErrorType.NotFound, StationNotFoundMessage);
        }

        ServiceResult<StationResponseDto>? scheduleError =
            ValidateSchedule<StationResponseDto>(request.Schedule);

        if (scheduleError is not null)
        {
            return scheduleError;
        }

        station.Schedule = request.Schedule.Select(entry => entry.ToEntity()).ToList();

        await _stationRepository.UpdateAsync(station, cancellationToken).ConfigureAwait(false);
        LogScheduleUpdated(station.StationCode, station.Schedule.Count);

        return ServiceResult.Success(station.ToResponseDto());
    }

    /// <inheritdoc />
    public async Task<ServiceResult<StationResponseDto>> DeactivateAsync(
        string id,
        CancellationToken cancellationToken = default)
    {
        SolarStationInfo? station = await _stationRepository
            .GetByIdAsync(id, cancellationToken)
            .ConfigureAwait(false);

        if (station is null)
        {
            return ServiceResult.Failure<StationResponseDto>(ServiceErrorType.NotFound, StationNotFoundMessage);
        }

        if (!station.IsActive)
        {
            return ServiceResult.Failure<StationResponseDto>(
                ServiceErrorType.Conflict, "This microgrid node is already deactivated.");
        }

        // BR-4. Prosumers hold bookings at this node that have not happened yet.
        // Taking it offline would leave them with a reservation nobody can
        // honour, so the request is refused and the count is reported back.
        long activeReservations = await _reservationRepository
            .CountActiveForStationAsync(station.Id ?? string.Empty, cancellationToken)
            .ConfigureAwait(false);

        if (activeReservations > 0)
        {
            LogDeactivationBlocked(station.StationCode, activeReservations);

            return ServiceResult.Failure<StationResponseDto>(
                ServiceErrorType.Conflict,
                $"{activeReservations} active reservation(s) must be cancelled or completed " +
                $"before node {station.StationCode} can be deactivated.");
        }

        station.IsActive = false;

        await _stationRepository.UpdateAsync(station, cancellationToken).ConfigureAwait(false);
        LogStationDeactivated(station.StationCode);

        return ServiceResult.Success(station.ToResponseDto());
    }

    /// <inheritdoc />
    public async Task<ServiceResult<StationResponseDto>> ReactivateAsync(
        string id,
        CancellationToken cancellationToken = default)
    {
        SolarStationInfo? station = await _stationRepository
            .GetByIdAsync(id, cancellationToken)
            .ConfigureAwait(false);

        if (station is null)
        {
            return ServiceResult.Failure<StationResponseDto>(ServiceErrorType.NotFound, StationNotFoundMessage);
        }

        if (station.IsActive)
        {
            return ServiceResult.Failure<StationResponseDto>(
                ServiceErrorType.Conflict, "This microgrid node is already active.");
        }

        station.IsActive = true;

        await _stationRepository.UpdateAsync(station, cancellationToken).ConfigureAwait(false);
        LogStationReactivated(station.StationCode);

        return ServiceResult.Success(station.ToResponseDto());
    }

    /// <summary>
    /// Checks a schedule for duplicate days and for closing times that are not
    /// after their opening time. Returns null when the schedule is valid.
    /// </summary>
    /// <remarks>
    /// Generic in the result type so the same check serves both create and
    /// schedule update without either duplicating it.
    /// </remarks>
    private static ServiceResult<TValue>? ValidateSchedule<TValue>(IList<ScheduleEntryDto> schedule)
    {
        if (schedule is null || schedule.Count == 0)
        {
            return null;
        }

        // One set of hours per day. Two entries for Monday would make slot
        // generation produce overlapping windows.
        IEnumerable<string> duplicateDays = schedule
            .GroupBy(entry => entry.DayOfWeek, StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key);

        string? firstDuplicate = duplicateDays.FirstOrDefault();
        if (firstDuplicate is not null)
        {
            return ServiceResult.Failure<TValue>(
                ServiceErrorType.Validation,
                $"The schedule has more than one entry for {firstDuplicate}. Use one entry per day.");
        }

        foreach (ScheduleEntryDto entry in schedule)
        {
            bool openParsed = TimeOnly.TryParseExact(entry.OpenTime, "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out TimeOnly open);
            bool closeParsed = TimeOnly.TryParseExact(entry.CloseTime, "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out TimeOnly close);

            if (!openParsed || !closeParsed)
            {
                return ServiceResult.Failure<TValue>(
                    ServiceErrorType.Validation,
                    $"The times for {entry.DayOfWeek} must be in 24 hour HH:mm format.");
            }

            if (close <= open)
            {
                return ServiceResult.Failure<TValue>(
                    ServiceErrorType.Validation,
                    $"The closing time for {entry.DayOfWeek} must be later than its opening time.");
            }
        }

        return null;
    }

    // -----------------------------------------------------------------------
    // Source generated log methods. See the note in MongoContext.cs.
    // -----------------------------------------------------------------------

    [LoggerMessage(
        EventId = 5001,
        Level = LogLevel.Information,
        Message = "Microgrid node {StationCode} created with id {StationId}.")]
    private partial void LogStationCreated(string stationCode, string stationId);

    [LoggerMessage(
        EventId = 5002,
        Level = LogLevel.Information,
        Message = "Microgrid node {StationCode} updated.")]
    private partial void LogStationUpdated(string stationCode);

    [LoggerMessage(
        EventId = 5003,
        Level = LogLevel.Information,
        Message = "Schedule for node {StationCode} replaced with {EntryCount} entries.")]
    private partial void LogScheduleUpdated(string stationCode, int entryCount);

    [LoggerMessage(
        EventId = 5004,
        Level = LogLevel.Warning,
        Message = "Microgrid node {StationCode} deactivated.")]
    private partial void LogStationDeactivated(string stationCode);

    [LoggerMessage(
        EventId = 5005,
        Level = LogLevel.Information,
        Message = "Microgrid node {StationCode} returned to service.")]
    private partial void LogStationReactivated(string stationCode);

    [LoggerMessage(
        EventId = 5006,
        Level = LogLevel.Information,
        Message = "Deactivation of node {StationCode} refused: {ActiveCount} active reservation(s) (BR-4).")]
    private partial void LogDeactivationBlocked(string stationCode, long activeCount);
}
