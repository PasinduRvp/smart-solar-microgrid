/*
 * ---------------------------------------------------------------------------
 * File        : QrTokenGenerator.cs
 * Project     : Smart Solar Microgrid Trading System — SE4040 Assignment 1
 * Author      : <Your Full Name> (<IT Number>)
 * Created     : 2026-09-19
 * Description : Produces the secret token encoded into a prosumer's transaction
 *               QR code.
 *
 * Security    : Three properties matter, and the choice of generator follows
 *               from them.
 *
 *               1. Unpredictable. RandomNumberGenerator is a cryptographically
 *                  secure source. System.Random must NOT be used here: its
 *                  output is derived from a seed, so observing a few tokens
 *                  would let an attacker work out the next ones and present a
 *                  QR code for a booking that is not theirs.
 *
 *               2. Unguessable by brute force. 32 bytes is 256 bits of entropy,
 *                  which cannot be searched exhaustively.
 *
 *               3. Not derived from the booking. The token is random rather
 *                  than a hash of the reservation number or the NIC, so knowing
 *                  a booking's details reveals nothing about its token.
 *
 *               The token is only ever a LOOKUP KEY. Possessing it proves
 *               nothing by itself: the API still checks that the reservation
 *               exists, is Approved, is not already Completed and belongs to
 *               the node the operator is standing at. That is why the operator
 *               device never decides validity for itself.
 * ---------------------------------------------------------------------------
 */

using System.Security.Cryptography;

namespace SolarMicrogrid.Api.Security;

/// <inheritdoc cref="IQrTokenGenerator" />
public sealed class QrTokenGenerator : IQrTokenGenerator
{
    /// <summary>Number of random bytes per token. 32 bytes is 256 bits.</summary>
    private const int TokenBytes = 32;

    /// <inheritdoc />
    public string Generate()
    {
        byte[] buffer = RandomNumberGenerator.GetBytes(TokenBytes);

        // Base64Url keeps the token safe to place in a URL or a QR payload
        // without escaping, and avoids the "+", "/" and "=" characters that
        // some QR scanners handle inconsistently.
        return Base64UrlEncode(buffer);
    }

    /// <summary>
    /// Encodes bytes using the URL safe Base64 alphabet, without padding.
    /// </summary>
    private static string Base64UrlEncode(byte[] input)
    {
        return Convert.ToBase64String(input)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }
}
