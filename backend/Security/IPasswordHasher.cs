/*
 * ---------------------------------------------------------------------------
 * File        : IPasswordHasher.cs
 * Project     : Smart Solar Microgrid Trading System — SE4040 Assignment 1
 * Author      : <Your Full Name> (<IT Number>)
 * Created     : 2026-09-19
 * Description : Contract for hashing and verifying account passwords.
 *
 * SOLID       : Dependency Inversion — AuthService depends on this interface,
 *               not on the BCrypt library. If the algorithm ever needs to
 *               change, one implementation is swapped in Program.cs and no
 *               business code is touched.
 * Security    : Hashing is deliberately behind an interface so that the choice
 *               of algorithm and work factor lives in exactly one class and can
 *               be reviewed in one place.
 * ---------------------------------------------------------------------------
 */

namespace SolarMicrogrid.Api.Security;

/// <summary>
/// Hashes new passwords and verifies supplied ones against a stored hash.
/// </summary>
public interface IPasswordHasher
{
    /// <summary>Produces a salted hash suitable for storage.</summary>
    string Hash(string password);

    /// <summary>
    /// Checks a supplied password against a stored hash.
    /// Returns false rather than throwing when the stored hash is malformed.
    /// </summary>
    bool Verify(string password, string passwordHash);
}
