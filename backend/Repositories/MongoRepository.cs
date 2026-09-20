/*
 * ---------------------------------------------------------------------------
 * File        : MongoRepository.cs
 * Project     : Smart Solar Microgrid Trading System — SE4040 Assignment 1
 * Author      : NIMSARA R V P P (IT 23215306)
 * Created     : 2026-09-17
 * Description : Single generic MongoDB implementation of IRepository, reused by
 *               all four collections. Written once, it removes what would
 *               otherwise be four near identical data access classes.
 *
 * SOLID       : Open/Closed — this class is closed for modification. An entity
 *               needing extra queries derives a new repository from it (see
 *               UserRepository) instead of this file being edited again.
 *               Liskov Substitution — it works for any EntityBase subtype.
 * OOP         : Generics and inheritance. The class is not sealed, and its
 *               Collection property is protected, specifically so derived
 *               repositories can build their own queries.
 * Design note : UpdateAsync always refreshes UpdatedAt, so no caller can forget
 *               to maintain the audit trail.
 * ---------------------------------------------------------------------------
 */

using System.Linq.Expressions;
using MongoDB.Driver;
using SolarMicrogrid.Api.Data;
using SolarMicrogrid.Api.Models;

namespace SolarMicrogrid.Api.Repositories;

/// <inheritdoc cref="IRepository{TEntity}" />
/// <typeparam name="TEntity">Entity type stored in the collection.</typeparam>
public class MongoRepository<TEntity> : IRepository<TEntity>
    where TEntity : EntityBase
{
    /// <summary>
    /// Typed handle to the underlying collection. Protected so derived
    /// repositories can express entity specific queries against it.
    /// </summary>
    protected IMongoCollection<TEntity> Collection { get; }

    /// <summary>
    /// Creates a repository bound to the named collection.
    /// </summary>
    /// <param name="context">Database connection abstraction.</param>
    /// <param name="collectionName">Collection name from <see cref="Common.CollectionNames"/>.</param>
    protected MongoRepository(IMongoContext context, string collectionName)
    {
        ArgumentNullException.ThrowIfNull(context);
        Collection = context.GetCollection<TEntity>(collectionName);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<TEntity>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await Collection
            .Find(FilterDefinition<TEntity>.Empty)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<TEntity?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        // An id that is not a valid ObjectId would make the driver throw. A
        // caller passing rubbish should get "not found", not a 500, so the
        // format is checked before the query is built.
        if (!MongoDB.Bson.ObjectId.TryParse(id, out _))
        {
            return null;
        }

        return await Collection
            .Find(entity => entity.Id == id)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<TEntity>> FindAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default)
    {
        return await Collection
            .Find(predicate)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<TEntity?> FindOneAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default)
    {
        return await Collection
            .Find(predicate)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<bool> ExistsAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default)
    {
        // Limit(1) stops the server counting past the first match.
        long matches = await Collection
            .Find(predicate)
            .Limit(1)
            .CountDocumentsAsync(cancellationToken)
            .ConfigureAwait(false);

        return matches > 0;
    }

    /// <inheritdoc />
    public async Task<long> CountAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default)
    {
        return await Collection
            .CountDocumentsAsync(predicate, cancellationToken: cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<TEntity> InsertAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entity);

        entity.CreatedAt = DateTime.UtcNow;
        entity.UpdatedAt = entity.CreatedAt;

        await Collection.InsertOneAsync(entity, options: null, cancellationToken).ConfigureAwait(false);

        // The driver writes the generated id back onto the instance.
        return entity;
    }

    /// <inheritdoc />
    public async Task<bool> UpdateAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entity);

        if (string.IsNullOrWhiteSpace(entity.Id))
        {
            return false;
        }

        // Stamped here so the audit trail cannot be skipped by a forgetful caller.
        entity.UpdatedAt = DateTime.UtcNow;

        ReplaceOneResult result = await Collection
            .ReplaceOneAsync(existing => existing.Id == entity.Id, entity, options: new ReplaceOptions(), cancellationToken)
            .ConfigureAwait(false);

        return result.MatchedCount > 0;
    }

    /// <inheritdoc />
    public async Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        if (!MongoDB.Bson.ObjectId.TryParse(id, out _))
        {
            return false;
        }

        DeleteResult result = await Collection
            .DeleteOneAsync(entity => entity.Id == id, cancellationToken)
            .ConfigureAwait(false);

        return result.DeletedCount > 0;
    }
}
