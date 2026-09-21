/*
 * ---------------------------------------------------------------------------
 * File        : UserRepository.cs
 * Project     : Smart Solar Microgrid Trading System — SE4040 Assignment 1
 * Author      : <Your Full Name> (<IT Number>)
 * Created     : 2026-09-17
 * Description : MongoDB implementation of IUserRepository. Inherits all of its
 *               CRUD behaviour from MongoRepository and adds only the four
 *               lookups that are specific to user accounts.
 *
 * OOP         : Inheritance used for genuine reuse — no CRUD code is repeated
 *               here. The class body contains user behaviour only.
 * SOLID       : Open/Closed in practice. MongoRepository was extended, not
 *               edited, to support these queries.
 * ---------------------------------------------------------------------------
 */

using MongoDB.Driver;
using SolarMicrogrid.Api.Common;
using SolarMicrogrid.Api.Data;
using SolarMicrogrid.Api.Models;
using SolarMicrogrid.Api.Models.Enums;

namespace SolarMicrogrid.Api.Repositories;

/// <inheritdoc cref="IUserRepository" />
public sealed class UserRepository : MongoRepository<User>, IUserRepository
{
    /// <summary>Binds the repository to the Users collection.</summary>
    public UserRepository(IMongoContext context)
        : base(context, CollectionNames.Users)
    {
    }

    /// <inheritdoc />
    public async Task<User?> GetByNicAsync(string nic, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(nic))
        {
            return null;
        }

        // NIC is stored exactly as supplied, so it is matched exactly. The
        // unique index created at startup guarantees at most one match.
        return await FindOneAsync(user => user.Nic == nic, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return null;
        }

        // Email addresses are case insensitive, so a regex with the "i" option
        // is used. Regex.Escape prevents a value such as ".*@.*" from being
        // interpreted as a pattern that would match every account — a real
        // injection risk on a sign in endpoint.
        FilterDefinition<User> filter = Builders<User>.Filter.Regex(
            user => user.Email,
            new MongoDB.Bson.BsonRegularExpression($"^{System.Text.RegularExpressions.Regex.Escape(email)}$", "i"));

        return await Collection
            .Find(filter)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<User>> GetByStatusAsync(
        AccountStatus status,
        CancellationToken cancellationToken = default)
    {
        return await FindAsync(user => user.Status == status, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<User>> GetByRoleAsync(
        UserRole role,
        CancellationToken cancellationToken = default)
    {
        return await FindAsync(user => user.Role == role, cancellationToken).ConfigureAwait(false);
    }
}
