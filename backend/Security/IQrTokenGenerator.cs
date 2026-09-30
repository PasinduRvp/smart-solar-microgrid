/*
 * ---------------------------------------------------------------------------
 * File        : IQrTokenGenerator.cs
 * Project     : Smart Solar Microgrid Trading System — SE4040 Assignment 1
 * Author      : <Your Full Name> (<IT Number>)
 * Created     : 2026-09-19
 * Description : Contract for producing the secret token that is encoded into a
 *               prosumer's transaction QR code when a booking is approved.
 *
 * SOLID       : Dependency Inversion — ReservationService asks for a token and
 *               does not know how one is produced. A test can supply a
 *               predictable generator instead of a random one.
 * ---------------------------------------------------------------------------
 */

namespace SolarMicrogrid.Api.Security;

/// <summary>
/// Produces the tokens carried by transaction QR codes.
/// </summary>
public interface IQrTokenGenerator
{
    /// <summary>
    /// Returns a new token that no one can predict or derive from a booking's
    /// visible details.
    /// </summary>
    string Generate();
}
