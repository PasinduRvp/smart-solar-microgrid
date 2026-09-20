/*
 * ---------------------------------------------------------------------------
 * File        : IMongoContext.cs
 * Project     : Smart Solar Microgrid Trading System — SE4040 Assignment 1
 * Author      : <Your Full Name> (<IT Number>)
 * Created     : 2026-09-16
 * Description : Abstraction over the MongoDB database connection. Every layer
 *               above data access depends on this interface rather than on the
 *               MongoDB driver, so the driver stays replaceable and the
 *               services above can be unit tested with a fake implementation.
 *
 * SOLID       : Dependency Inversion — higher level modules (services) depend
 *               on this abstraction, not on the concrete MongoContext.
 *               Interface Segregation — the interface exposes only the two
 *               operations callers actually need; it is not a wrapper around
 *               the driver's entire surface.
 * ---------------------------------------------------------------------------
 */

using MongoDB.Driver;

namespace SolarMicrogrid.Api.Data;

/// <summary>
/// Provides access to the collections of the application database.
/// </summary>
public interface IMongoContext
{
    /// <summary>Name of the database currently in use. Safe to expose — carries no credentials.</summary>
    string DatabaseName { get; }

    /// <summary>
    /// Returns a typed handle to a collection. The handle is cheap to create
    /// and thread safe, so callers may request one per operation.
    /// </summary>
    /// <typeparam name="TDocument">CLR type the documents map to.</typeparam>
    /// <param name="collectionName">Name of the collection in MongoDB.</param>
    IMongoCollection<TDocument> GetCollection<TDocument>(string collectionName);

    /// <summary>
    /// Verifies that the database is reachable by issuing a lightweight ping.
    /// Returns false rather than throwing, so callers can report connection
    /// state as data instead of handling exceptions for a routine check.
    /// </summary>
    Task<bool> PingAsync(CancellationToken cancellationToken = default);
}
