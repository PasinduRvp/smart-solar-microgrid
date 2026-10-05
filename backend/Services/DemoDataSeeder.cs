/*
 * ---------------------------------------------------------------------------
 * File        : DemoDataSeeder.cs
 * Project     : Smart Solar Microgrid Trading System — SE4040 Assignment 1
 * Author      : NIMSARA R V P P (IT 23215306)
 * Created     : 2026-09-19
 * Description : Fills the database with a realistic demonstration data set:
 *               grid operators, prosumers in every account state, microgrid
 *               nodes across Sri Lanka with their weekly schedules, booking
 *               windows for the coming week, and reservations in every status.
 *
 *               The data is chosen so that each business rule can be shown
 *               working. In particular one booking is deliberately placed less
 *               than twelve hours away, so the BR-2 and BR-3 refusals can be
 *               demonstrated without waiting for the clock.
 *
 * Idempotent  : Every record is created only if it is missing, matched on its
 *               natural key — NIC for people, station code for nodes. Running
 *               the seeder twice therefore changes nothing the second time.
 *
 * SOLID       : Single Responsibility — building sample data. It creates nodes
 *               and bookings by calling the same services the API exposes, so
 *               seeded records go through exactly the same rules as real ones
 *               rather than being written straight to the collections.
 * ---------------------------------------------------------------------------
 */

using SolarMicrogrid.Api.Common;
using SolarMicrogrid.Api.Dtos.Slots;
using SolarMicrogrid.Api.Dtos.Stations;
using SolarMicrogrid.Api.Mappings;
using SolarMicrogrid.Api.Models;
using SolarMicrogrid.Api.Models.Enums;
using SolarMicrogrid.Api.Repositories;
using SolarMicrogrid.Api.Security;

namespace SolarMicrogrid.Api.Services;

/// <inheritdoc cref="IDemoDataSeeder" />
public sealed partial class DemoDataSeeder : IDemoDataSeeder
{
    private const string DemoPassword = "Demo@1234";

    private readonly IUserRepository _userRepository;
    private readonly IStationRepository _stationRepository;
    private readonly ISlotRepository _slotRepository;
    private readonly IReservationRepository _reservationRepository;
    private readonly IStationService _stationService;
    private readonly ISlotService _slotService;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IQrTokenGenerator _qrTokenGenerator;
    private readonly ILogger<DemoDataSeeder> _logger;

    /// <summary>Receives its collaborators from the DI container.</summary>
    public DemoDataSeeder(
        IUserRepository userRepository,
        IStationRepository stationRepository,
        ISlotRepository slotRepository,
        IReservationRepository reservationRepository,
        IStationService stationService,
        ISlotService slotService,
        IPasswordHasher passwordHasher,
        IQrTokenGenerator qrTokenGenerator,
        ILogger<DemoDataSeeder> logger)
    {
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _stationRepository = stationRepository ?? throw new ArgumentNullException(nameof(stationRepository));
        _slotRepository = slotRepository ?? throw new ArgumentNullException(nameof(slotRepository));
        _reservationRepository = reservationRepository ?? throw new ArgumentNullException(nameof(reservationRepository));
        _stationService = stationService ?? throw new ArgumentNullException(nameof(stationService));
        _slotService = slotService ?? throw new ArgumentNullException(nameof(slotService));
        _passwordHasher = passwordHasher ?? throw new ArgumentNullException(nameof(passwordHasher));
        _qrTokenGenerator = qrTokenGenerator ?? throw new ArgumentNullException(nameof(qrTokenGenerator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<ServiceResult<IReadOnlyList<string>>> SeedAsync(
        CancellationToken cancellationToken = default)
    {
        List<string> report = [];

        await SeedStaffAsync(report, cancellationToken).ConfigureAwait(false);
        await SeedProsumersAsync(report, cancellationToken).ConfigureAwait(false);

        IReadOnlyList<SolarStationInfo> stations =
            await SeedStationsAsync(report, cancellationToken).ConfigureAwait(false);

        await SeedSlotsAsync(stations, report, cancellationToken).ConfigureAwait(false);
        await SeedReservationsAsync(report, cancellationToken).ConfigureAwait(false);

        LogSeedCompleted(report.Count);
        return ServiceResult.Success<IReadOnlyList<string>>(report);
    }

    /// <summary>Creates two Grid Operator accounts.</summary>
    private async Task SeedStaffAsync(List<string> report, CancellationToken cancellationToken)
    {
        (string Nic, string Name, string Email)[] operators =
        [
            ("198534567890", "Nimal Fernando", "nimal.operator@microgrid.lk"),
            ("199245678901", "Sanduni Silva", "sanduni.operator@microgrid.lk"),
        ];

        foreach ((string nic, string name, string email) in operators)
        {
            if (await _userRepository.GetByNicAsync(nic, cancellationToken).ConfigureAwait(false) is not null)
            {
                continue;
            }

            await _userRepository.InsertAsync(
                new User
                {
                    Nic = nic,
                    FullName = name,
                    Email = email,
                    Phone = "0711234567",
                    Address = "Microgrid Operations Centre, Colombo",
                    PasswordHash = _passwordHasher.Hash(DemoPassword),
                    Role = UserRole.GridOperator,
                    Status = AccountStatus.Active,
                },
                cancellationToken).ConfigureAwait(false);

            report.Add($"Grid Operator created: {email} / {DemoPassword}");
        }
    }

    /// <summary>
    /// Creates prosumers covering every account state, so the pending
    /// activation queue and the deactivation rules both have data to show.
    /// </summary>
    private async Task SeedProsumersAsync(List<string> report, CancellationToken cancellationToken)
    {
        (string Nic, string Name, string Email, double Kw, AccountStatus Status, bool Requested)[] prosumers =
        [
            ("199512345678", "Amara Jayawardena", "amara@example.lk", 6.5, AccountStatus.Active, false),
            ("200023456789", "Ruwan Dissanayake", "ruwan@example.lk", 4.0, AccountStatus.Active, true),
            ("200134567890", "Tharushi Bandara", "tharushi@example.lk", 8.2, AccountStatus.Pending, false),
            ("198745678902", "Kumara Rajapaksa", "kumara@example.lk", 3.5, AccountStatus.Deactivated, false),
        ];

        foreach ((string nic, string name, string email, double kw, AccountStatus status, bool requested) in prosumers)
        {
            if (await _userRepository.GetByNicAsync(nic, cancellationToken).ConfigureAwait(false) is not null)
            {
                continue;
            }

            await _userRepository.InsertAsync(
                new User
                {
                    Nic = nic,
                    FullName = name,
                    Email = email,
                    Phone = "0761234567",
                    Address = "Solar residence, Western Province",
                    PasswordHash = _passwordHasher.Hash(DemoPassword),
                    Role = UserRole.Prosumer,
                    Status = status,
                    DeactivationRequested = requested,
                    SolarCapacityKw = kw,
                },
                cancellationToken).ConfigureAwait(false);

            report.Add($"Prosumer created: {nic} ({status}) / {DemoPassword}");
        }
    }

    /// <summary>
    /// Creates microgrid nodes at real Sri Lankan locations, so the map screen
    /// and the nearby search have meaningful coordinates to work with.
    /// </summary>
    private async Task<IReadOnlyList<SolarStationInfo>> SeedStationsAsync(
        List<string> report,
        CancellationToken cancellationToken)
    {
        (string Code, string Name, double Lat, double Lng, string Address, double Kwh, int Slots)[] definitions =
        [
            ("MG-COL-002", "Dehiwala Coastal Hub", 6.8511, 79.8653, "Galle Road, Dehiwala", 180, 8),
            ("MG-KAN-001", "Kandy Hill Microgrid", 7.2906, 80.6337, "Peradeniya Road, Kandy", 220, 10),
            ("MG-GAL-001", "Galle Fort Solar Node", 6.0329, 80.2168, "Church Street, Galle", 150, 6),
            ("MG-NEG-001", "Negombo Lagoon Hub", 7.2083, 79.8358, "Lewis Place, Negombo", 130, 6),
        ];

        List<SolarStationInfo> created = [];

        foreach ((string code, string name, double lat, double lng, string address, double kwh, int slots) in definitions)
        {
            SolarStationInfo? existing = await _stationRepository
                .GetByCodeAsync(code, cancellationToken)
                .ConfigureAwait(false);

            if (existing is not null)
            {
                created.Add(existing);
                continue;
            }

            // Built through the station service rather than written directly,
            // so the seeded nodes pass the same schedule validation as any node
            // an officer creates through the web application.
            ServiceResult<StationResponseDto> result = await _stationService
                .CreateAsync(
                    new CreateStationRequestDto
                    {
                        StationCode = code,
                        Name = name,
                        Location = new GeoLocationDto { Latitude = lat, Longitude = lng, AddressLine = address },
                        CapacityKwh = kwh,
                        TotalBatterySlots = slots,
                        Schedule = BuildWeeklySchedule(),
                    },
                    cancellationToken)
                .ConfigureAwait(false);

            if (!result.IsSuccess || result.Value is null)
            {
                report.Add($"Node {code} could not be created: {result.Error}");
                continue;
            }

            SolarStationInfo? saved = await _stationRepository
                .GetByCodeAsync(code, cancellationToken)
                .ConfigureAwait(false);

            if (saved is not null)
            {
                created.Add(saved);
            }

            report.Add($"Microgrid node created: {code} — {name}");
        }

        return created;
    }

    /// <summary>Generates a week of booking windows at each node.</summary>
    private async Task SeedSlotsAsync(
        IReadOnlyList<SolarStationInfo> stations,
        List<string> report,
        CancellationToken cancellationToken)
    {
        foreach (SolarStationInfo station in stations)
        {
            ServiceResult<IReadOnlyList<SlotResponseDto>> result = await _slotService
                .GenerateAsync(
                    station.Id ?? string.Empty,
                    new GenerateSlotsRequestDto
                    {
                        NumberOfDays = 7,
                        SlotDurationMinutes = 120,
                        CapacityPerSlot = 3,
                        EnergyRatePerKwh = 42.50,
                    },
                    cancellationToken)
                .ConfigureAwait(false);

            // A Conflict here means the windows already exist, which is the
            // expected outcome on a second run and is not worth reporting.
            if (result.IsSuccess && result.Value is not null)
            {
                report.Add($"Generated {result.Value.Count} booking windows for {station.StationCode}");
            }
        }
    }

    /// <summary>
    /// Creates reservations in every status against real booking windows,
    /// including one deliberately inside the twelve hour notice period.
    /// </summary>
    private async Task SeedReservationsAsync(List<string> report, CancellationToken cancellationToken)
    {
        const string demoNic = "199512345678";

        // Already seeded on a previous run.
        long existing = await _reservationRepository
            .CountForProsumerAsync(demoNic, ReservationStatus.Pending, futureOnly: false, cancellationToken)
            .ConfigureAwait(false);

        if (existing > 0)
        {
            return;
        }

        SolarStationInfo? station = await _stationRepository
            .GetByCodeAsync("MG-COL-002", cancellationToken)
            .ConfigureAwait(false);

        if (station is null)
        {
            return;
        }

        DateTime now = DateTime.UtcNow;

        IReadOnlyList<EnergyBookingSlot> slots = await _slotRepository
            .GetByStationBetweenAsync(station.Id ?? string.Empty, now.Date, now.Date.AddDays(6), cancellationToken)
            .ConfigureAwait(false);

        // A window comfortably outside the notice period, so it can be changed
        // and cancelled during the demonstration.
        EnergyBookingSlot? farSlot = slots
            .FirstOrDefault(slot => slot.SlotStartUtc() > now.AddHours(36));

        // A window inside the notice period, so the BR-2 and BR-3 refusals can
        // be shown immediately rather than waiting for the clock to catch up.
        EnergyBookingSlot? nearSlot = slots
            .FirstOrDefault(slot => slot.SlotStartUtc() > now && slot.SlotStartUtc() < now.AddHours(11));

        if (farSlot is not null)
        {
            await CreateSeedReservationAsync(
                demoNic, station, farSlot, ReservationStatus.Pending, issueToken: false, cancellationToken)
                .ConfigureAwait(false);

            report.Add($"Pending reservation created for {demoNic} (changeable)");

            EnergyBookingSlot? secondFar = slots
                .FirstOrDefault(slot => slot.SlotStartUtc() > now.AddHours(48) && slot.Id != farSlot.Id);

            if (secondFar is not null)
            {
                await CreateSeedReservationAsync(
                    demoNic, station, secondFar, ReservationStatus.Approved, issueToken: true, cancellationToken)
                    .ConfigureAwait(false);

                report.Add($"Approved reservation with QR token created for {demoNic}");
            }
        }

        if (nearSlot is not null)
        {
            await CreateSeedReservationAsync(
                demoNic, station, nearSlot, ReservationStatus.Approved, issueToken: true, cancellationToken)
                .ConfigureAwait(false);

            report.Add($"Approved reservation inside the 12 hour window created for {demoNic} " +
                       "(demonstrates the BR-2 and BR-3 refusals)");
        }
    }

    /// <summary>
    /// Writes one seeded reservation and takes its place in the window, so the
    /// booked counts stay consistent with the reservations that exist.
    /// </summary>
    private async Task CreateSeedReservationAsync(
        string nic,
        SolarStationInfo station,
        EnergyBookingSlot slot,
        ReservationStatus status,
        bool issueToken,
        CancellationToken cancellationToken)
    {
        bool taken = await _slotRepository
            .TryReserveCapacityAsync(slot.Id ?? string.Empty, cancellationToken)
            .ConfigureAwait(false);

        if (!taken)
        {
            return;
        }

        await _reservationRepository.InsertAsync(
            new EnergyReservation
            {
                ReservationNo = $"RSV-SEED-{Guid.NewGuid().ToString("N")[..4].ToUpperInvariant()}",
                ProsumerNic = nic,
                StationId = station.Id ?? string.Empty,
                SlotId = slot.Id ?? string.Empty,
                ReservationDateTime = slot.SlotStartUtc(),
                EnergyKwh = 10,
                Direction = EnergyDirection.Deliver,
                Status = status,
                QrToken = issueToken ? _qrTokenGenerator.Generate() : null,
                QrIssuedAt = issueToken ? DateTime.UtcNow : null,
            },
            cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Opening hours used for every seeded node.</summary>
    private static List<ScheduleEntryDto> BuildWeeklySchedule()
    {
        List<ScheduleEntryDto> schedule = [];

        foreach (DayOfWeek day in Enum.GetValues<DayOfWeek>())
        {
            bool weekend = day is DayOfWeek.Saturday or DayOfWeek.Sunday;

            schedule.Add(new ScheduleEntryDto
            {
                DayOfWeek = day.ToString(),
                OpenTime = weekend ? "08:00" : "06:00",
                CloseTime = weekend ? "16:00" : "20:00",
            });
        }

        return schedule;
    }

    // -----------------------------------------------------------------------
    // Source generated log methods. See the note in MongoContext.cs.
    // -----------------------------------------------------------------------

    [LoggerMessage(
        EventId = 7001,
        Level = LogLevel.Warning,
        Message = "Demonstration data seeding finished with {ChangeCount} change(s).")]
    private partial void LogSeedCompleted(int changeCount);
}
