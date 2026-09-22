/*
 * ---------------------------------------------------------------------------
 * File        : RateLimitPolicies.cs
 * Project     : Smart Solar Microgrid Trading System — SE4040 Assignment 1
 * Author      : <Your Full Name> (<IT Number>)
 * Created     : 2026-09-19
 * Description : Names of the rate limiting policies registered in Program.cs.
 *
 * Code smell  : As with Roles, attribute arguments must be compile time
 *               constants. Declaring the policy names here stops the same
 *               string being retyped on every protected endpoint, where a typo
 *               would silently leave that endpoint unlimited.
 * Security    : BCrypt makes each password guess expensive for an attacker, but
 *               it makes it expensive for our server too. Rate limiting is the
 *               complementary control: it caps how many attempts a single
 *               address can make at all.
 * ---------------------------------------------------------------------------
 */

namespace SolarMicrogrid.Api.Common;

/// <summary>
/// Rate limiting policy names used by the [EnableRateLimiting] attribute.
/// </summary>
public static class RateLimitPolicies
{
    /// <summary>
    /// Applied to sign in and registration. Limits password guessing and bulk
    /// account creation from any single client address.
    /// </summary>
    public const string Authentication = "authentication";
}
