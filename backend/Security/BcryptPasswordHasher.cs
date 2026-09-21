/*
 * ---------------------------------------------------------------------------
 * File        : BcryptPasswordHasher.cs
 * Project     : Smart Solar Microgrid Trading System — SE4040 Assignment 1
 * Author      : <Your Full Name> (<IT Number>)
 * Created     : 2026-09-19
 * Description : BCrypt implementation of IPasswordHasher.
 *
 * Security    : BCrypt is a deliberately slow, salted password hashing
 *               function. Three properties matter here:
 *                 1. It salts every password automatically, so two users with
 *                    the same password get different hashes and a stolen
 *                    database cannot be attacked with rainbow tables.
 *                 2. The work factor makes each guess expensive. At 12 a single
 *                    verification costs roughly a quarter of a second, which is
 *                    unnoticeable at sign in but makes large scale brute force
 *                    impractical.
 *                 3. The salt and work factor are stored inside the hash
 *                    string, so old hashes keep verifying if the factor is
 *                    raised later.
 *               A general purpose hash such as SHA-256 would be wrong here
 *               precisely because it is fast.
 * ---------------------------------------------------------------------------
 */

namespace SolarMicrogrid.Api.Security;

/// <inheritdoc cref="IPasswordHasher" />
public sealed class BcryptPasswordHasher : IPasswordHasher
{
    /// <summary>
    /// BCrypt cost factor. Each increment doubles the work required.
    /// 12 is the current widely recommended minimum for new applications.
    /// </summary>
    private const int WorkFactor = 12;

    /// <inheritdoc />
    public string Hash(string password)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(password);
        return BCrypt.Net.BCrypt.HashPassword(password, WorkFactor);
    }

    /// <inheritdoc />
    public bool Verify(string password, string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(passwordHash))
        {
            return false;
        }

        // A malformed or truncated hash in the database is a data problem, not
        // a reason to return 500 from a sign in attempt. Treat it as a failed
        // verification so the caller simply sees invalid credentials.
        try
        {
            return BCrypt.Net.BCrypt.Verify(password, passwordHash);
        }
        catch (BCrypt.Net.SaltParseException)
        {
            return false;
        }
    }
}
