/*
 * ---------------------------------------------------------------------------
 * File        : IRepository.cs
 * Project     : Smart Solar Microgrid Trading System — SE4040 Assignment 1
 * Author      : NIMSARA R V P P (IT 23215306)
 * Created     : 2026-09-17
 * Description : Generic data access contract shared by every entity in the
 *               system. Services depend on this interface, so they can be
 *               tested against a fake repository with no database present.
 *
 * SOLID       : Dependency Inversion — services depend on this abstraction
 *               rather than on MongoDB.
 *               Interface Segregation — it declares only the operations every
 *               entity genuinely needs. Queries specific to one entity go on a
 *               derived interface such as IUserRepository instead of being
 *               added here, where they would burden all the others.
 * Code smell  : Prevents the duplicated CRUD code that would otherwise appear
 *               once per collection.
 * ---------------------------------------------------------------------------
 */

using System.Linq.Expressions;
using SolarMicrogrid.Api.Models;

namespace SolarMicrogrid.Api.Repositories;

/// <summary>
/// Data access operations available for any persisted entity.
/// </summary>
/// <typeparam name="TEntity">
/// Entity type handled by this repository. Constrained to EntityBase so the
/// implementation can rely on the Id and audit fields being present.
/// </typeparam>
public interface IRepository<TEntity>
    where TEntity : EntityBase
{
    /// <summary>Returns every document in the collection.</summary>
    Task<IReadOnlyList<TEntity>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>Returns one document by its identifier, or null when not found.</summary>
    Task<TEntity?> GetByIdAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>Returns every document matching the supplied predicate.</summary>
    Task<IReadOnlyList<TEntity>> FindAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default);

    /// <summary>Returns the first document matching the predicate, or null.</summary>
    Task<TEntity?> FindOneAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default);

    /// <summary>True when at least one document matches the predicate.</summary>
    Task<bool> ExistsAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default);

    /// <summary>Counts the documents matching the predicate.</summary>
    Task<long> CountAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default);

    /// <summary>Inserts a new document and returns it with its generated id.</summary>
    Task<TEntity> InsertAsync(TEntity entity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Replaces an existing document. Returns false when no document with that
    /// id exists, so callers can map the outcome to a 404 response.
    /// </summary>
    Task<bool> UpdateAsync(TEntity entity, CancellationToken cancellationToken = default);

    /// <summary>Deletes a document by id. Returns false when it did not exist.</summary>
    Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default);
}
