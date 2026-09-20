/*
 * ---------------------------------------------------------------------------
 * File        : MongoContext.cs
 * Project     : Smart Solar Microgrid Trading System — SE4040 Assignment 1
 * Author      : NIMSARA R V P P (IT 23215306)
 * Created     : 2026-09-16
 * Description : Concrete MongoDB implementation of IMongoContext. Owns the
 *               single MongoClient instance for the application and hands out
 *               typed collection handles. This is the only class in the
 *               solution that references the MongoDB driver's client types.
 *
 * SOLID       : Single Responsibility — establishes and exposes the database
 *               connection, nothing more. It contains no business rules.
 *               Liskov Substitution — it can replace IMongoContext anywhere
 *               without changing caller behaviour.
 * Design note : MongoClient maintains its own internal connection pool and is
 *               thread safe, so exactly one instance is registered as a
 *               singleton. Creating a client per request is a well known
 *               MongoDB anti-pattern that exhausts connections under load.
 * Security    : The connection string carries credentials and is never logged.
 *               Only the database name is ever written to the log or returned
 *               to a caller.
 * ---------------------------------------------------------------------------
 */

using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using SolarMicrogrid.Api.Configuration;

namespace SolarMicrogrid.Api.Data;

/// <inheritdoc cref="IMongoContext" />
public sealed partial class MongoContext : IMongoContext
{
    private readonly IMongoDatabase _database;
    private readonly ILogger<MongoContext> _logger;

    /// <summary>
    /// Builds the MongoDB client from validated settings and resolves the
    /// application database. Called once by the DI container at startup.
    /// </summary>
    public MongoContext(IOptions<MongoDbSettings> options, ILogger<MongoContext> logger)
    {
        ArgumentNullException.ThrowIfNull(options);
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        MongoDbSettings settings = options.Value;

        // Parse the connection string so we can override the server selection
        // timeout; the driver's 30 second default makes an unreachable cluster
        // look like a hung request rather than a failed connection.
        MongoClientSettings clientSettings =
            MongoClientSettings.FromConnectionString(settings.ConnectionString);
        clientSettings.ServerSelectionTimeout =
            TimeSpan.FromSeconds(settings.ServerSelectionTimeoutSeconds);
        clientSettings.ApplicationName = "SolarMicrogrid.Api";

        MongoClient client = new(clientSettings);
        _database = client.GetDatabase(settings.DatabaseName);
        DatabaseName = settings.DatabaseName;

        // Log the database name only. The connection string holds the password.
        LogContextInitialised(DatabaseName);
    }

    /// <inheritdoc />
    public string DatabaseName { get; }

    /// <inheritdoc />
    public IMongoCollection<TDocument> GetCollection<TDocument>(string collectionName)
    {
        // Guard against an empty collection name, which MongoDB would otherwise
        // reject with a far less obvious error deeper in the driver.
        ArgumentException.ThrowIfNullOrWhiteSpace(collectionName);
        return _database.GetCollection<TDocument>(collectionName);
    }

    /// <inheritdoc />
    public async Task<bool> PingAsync(CancellationToken cancellationToken = default)
    {
        // A failed ping is an expected, reportable state rather than a fault,
        // so the exception is swallowed here and surfaced to the caller as false.
        try
        {
            await _database
                .RunCommandAsync<BsonDocument>(new BsonDocument("ping", 1), cancellationToken: cancellationToken)
                .ConfigureAwait(false);
            return true;
        }
        catch (MongoException ex)
        {
            LogPingFailed(ex, DatabaseName);
            return false;
        }
        catch (TimeoutException ex)
        {
            LogPingTimedOut(ex, DatabaseName);
            return false;
        }
    }

    // -----------------------------------------------------------------------
    // Source generated log methods.
    //
    // The [LoggerMessage] generator turns each of these into a cached, strongly
    // typed delegate at compile time. That avoids re-parsing the message
    // template and boxing the arguments on every call, which is what analyser
    // rule CA1848 asks for. Every class in this project that logs follows this
    // same convention: the class is declared partial and its log messages are
    // grouped here at the bottom.
    // -----------------------------------------------------------------------

    [LoggerMessage(
        EventId = 1001,
        Level = LogLevel.Information,
        Message = "MongoDB context initialised for database {DatabaseName}.")]
    private partial void LogContextInitialised(string databaseName);

    [LoggerMessage(
        EventId = 1002,
        Level = LogLevel.Error,
        Message = "Ping to database {DatabaseName} failed.")]
    private partial void LogPingFailed(Exception exception, string databaseName);

    [LoggerMessage(
        EventId = 1003,
        Level = LogLevel.Error,
        Message = "Ping to database {DatabaseName} timed out.")]
    private partial void LogPingTimedOut(Exception exception, string databaseName);
}
