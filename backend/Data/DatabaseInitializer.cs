/*
 * ---------------------------------------------------------------------------
 * File        : DatabaseInitializer.cs
 * Project     : Smart Solar Microgrid Trading System — SE4040 Assignment 1
 * Author      : NIMSARA R V P P (IT 23215306)
 * Created     : 2026-09-17
 * Description : Creates the MongoDB indexes the application relies on, once, at
 *               startup. Runs as a hosted service so the work happens as part
 *               of the host starting rather than on a user's first request.
 *
 * Why this    : The assignment states NIC is the primary key for prosumers.
 *               Enforcing that only in C# would leave the door open to
 *               duplicates arriving through a concurrent request, a seed
 *               script or MongoDB Compass. A unique index makes the database
 *               itself reject the second insert, so the rule holds no matter
 *               how the data arrives.
 * SOLID       : Single Responsibility — schema setup only, kept out of
 *               MongoContext, whose job is connecting.
 * Design note : CreateManyAsync is idempotent. Re-running it on an existing
 *               index is a no-op, so restarting the service is always safe.
 * ---------------------------------------------------------------------------
 */

using MongoDB.Bson;
using MongoDB.Driver;
using SolarMicrogrid.Api.Common;
using SolarMicrogrid.Api.Models;

namespace SolarMicrogrid.Api.Data;

/// <summary>
/// Ensures required indexes exist when the application starts.
/// </summary>
public sealed partial class DatabaseInitializer : IHostedService
{
    /// <summary>Name of the unique index over issued QR tokens.</summary>
    private const string QrTokenIndexName = "ux_reservation_qrtoken";

    private readonly IMongoContext _context;
    private readonly ILogger<DatabaseInitializer> _logger;

    /// <summary>Receives the database context and a logger from the container.</summary>
    public DatabaseInitializer(IMongoContext context, ILogger<DatabaseInitializer> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Creates every index the application depends on.
    /// </summary>
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            await CreateUserIndexesAsync(cancellationToken).ConfigureAwait(false);
            await CreateReservationIndexesAsync(cancellationToken).ConfigureAwait(false);
            await CreateSlotIndexesAsync(cancellationToken).ConfigureAwait(false);

            LogIndexesReady();
        }
        catch (Exception ex) when (ex is MongoException or TimeoutException)
        {
            // Index creation failing must not stop the service from starting —
            // the health endpoint still needs to be reachable so the problem
            // can be diagnosed on the deployed machine, which matters most on
            // the IIS deployment where there is no console to read.
            //
            // TimeoutException is caught explicitly alongside MongoException:
            // the driver throws a plain System.TimeoutException when it cannot
            // select a server at all, and that type does NOT derive from
            // MongoException. Catching only MongoException here would let an
            // unreachable cluster terminate the host at startup.
            LogIndexCreationFailed(ex);
        }
    }

    /// <summary>Nothing to release on shutdown.</summary>
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    /// <summary>
    /// Unique index on NIC, plus a unique index on email for staff sign in.
    /// </summary>
    private async Task CreateUserIndexesAsync(CancellationToken cancellationToken)
    {
        IMongoCollection<User> users = _context.GetCollection<User>(CollectionNames.Users);

        CreateIndexModel<User> nicIndex = new(
            Builders<User>.IndexKeys.Ascending(user => user.Nic),
            new CreateIndexOptions { Name = "ux_users_nic", Unique = true });

        CreateIndexModel<User> emailIndex = new(
            Builders<User>.IndexKeys.Ascending(user => user.Email),
            new CreateIndexOptions { Name = "ux_users_email", Unique = true });

        await users.Indexes.CreateManyAsync([nicIndex, emailIndex], cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Indexes supporting the booking history, the prosumer dashboard counts
    /// and the check that blocks deactivating a station with live bookings.
    /// </summary>
    private async Task CreateReservationIndexesAsync(CancellationToken cancellationToken)
    {
        IMongoCollection<EnergyReservation> reservations =
            _context.GetCollection<EnergyReservation>(CollectionNames.EnergyReservation);

        CreateIndexModel<EnergyReservation> byProsumer = new(
            Builders<EnergyReservation>.IndexKeys
                .Ascending(reservation => reservation.ProsumerNic)
                .Descending(reservation => reservation.ReservationDateTime),
            new CreateIndexOptions { Name = "ix_reservation_prosumer_date" });

        CreateIndexModel<EnergyReservation> byStationStatus = new(
            Builders<EnergyReservation>.IndexKeys
                .Ascending(reservation => reservation.StationId)
                .Ascending(reservation => reservation.Status),
            new CreateIndexOptions { Name = "ix_reservation_station_status" });

        // Only approved bookings carry a token; every other reservation stores
        // qrToken as null. The index must therefore be unique ONLY across the
        // documents that actually hold a token.
        //
        // A sparse index is NOT sufficient here, and this is a subtle trap.
        // "Sparse" excludes documents where the field is ABSENT, not documents
        // where it is present and null. Our C# model always serialises the
        // property, so every unapproved reservation writes qrToken: null — the
        // field exists, the sparse index includes it, and the second such
        // reservation collides with the first on the duplicate value null.
        //
        // A PARTIAL index filters on the value instead of on mere presence.
        // Restricting it to documents whose qrToken is a string leaves the
        // nulls out of the index altogether, so unapproved bookings never
        // collide while issued tokens remain guaranteed unique.
        CreateIndexModel<EnergyReservation> byQrToken = new(
            Builders<EnergyReservation>.IndexKeys.Ascending(reservation => reservation.QrToken),
            new CreateIndexOptions<EnergyReservation>
            {
                Name = QrTokenIndexName,
                Unique = true,
                PartialFilterExpression =
                    Builders<EnergyReservation>.Filter.Type(reservation => reservation.QrToken, BsonType.String),
            });

        // An index cannot be redefined in place: creating one with an existing
        // name but different options fails with IndexOptionsConflict. The old
        // sparse version is therefore dropped first. Dropping is safe because
        // an index carries no data of its own, only a view of the documents.
        await DropIndexIfExistsAsync(reservations, QrTokenIndexName, cancellationToken).ConfigureAwait(false);

        await reservations.Indexes
            .CreateManyAsync([byProsumer, byStationStatus, byQrToken], cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Drops an index by name, ignoring the case where it does not exist.
    /// </summary>
    /// <remarks>
    /// Used when an index definition changes, because MongoDB will not alter an
    /// existing index in place. The "IndexNotFound" error is expected on a
    /// fresh database and is swallowed; anything else is allowed to propagate
    /// to the caller, which logs it and lets the service start regardless.
    /// </remarks>
    private static async Task DropIndexIfExistsAsync<TDocument>(
        IMongoCollection<TDocument> collection,
        string indexName,
        CancellationToken cancellationToken)
    {
        try
        {
            await collection.Indexes.DropOneAsync(indexName, cancellationToken).ConfigureAwait(false);
        }
        catch (MongoCommandException ex) when (ex.CodeName == "IndexNotFound")
        {
            // Nothing to drop. Normal on a new database.
        }
    }

    /// <summary>Index supporting the "slots for this station on this date" query.</summary>
    private async Task CreateSlotIndexesAsync(CancellationToken cancellationToken)
    {
        IMongoCollection<EnergyBookingSlot> slots =
            _context.GetCollection<EnergyBookingSlot>(CollectionNames.EnergyBookingSlots);

        CreateIndexModel<EnergyBookingSlot> byStationDate = new(
            Builders<EnergyBookingSlot>.IndexKeys
                .Ascending(slot => slot.StationId)
                .Ascending(slot => slot.SlotDate),
            new CreateIndexOptions { Name = "ix_slot_station_date" });

        await slots.Indexes.CreateManyAsync([byStationDate], cancellationToken).ConfigureAwait(false);
    }

    // -----------------------------------------------------------------------
    // Source generated log methods. See the note in MongoContext.cs.
    // -----------------------------------------------------------------------

    [LoggerMessage(
        EventId = 1101,
        Level = LogLevel.Information,
        Message = "Database indexes verified.")]
    private partial void LogIndexesReady();

    [LoggerMessage(
        EventId = 1102,
        Level = LogLevel.Error,
        Message = "Index creation failed. The service will continue to start.")]
    private partial void LogIndexCreationFailed(Exception exception);
}
