/*
 * ---------------------------------------------------------------------------
 * File        : ICurrentUser.cs
 * Project     : Smart Solar Microgrid Trading System — SE4040 Assignment 1
 * Author      : <Your Full Name> (<IT Number>)
 * Created     : 2026-09-19
 * Description : Describes the caller behind the current request, as read from
 *               their validated access token.
 *
 * Why this    : Several rules depend on who is asking, not just on their role.
 *               A prosumer may edit their own profile but nobody else's, and
 *               only a Backoffice officer may reactivate an account. Those
 *               checks belong in the service layer, and the service layer must
 *               not reach into HttpContext to make them — that would drag HTTP
 *               concerns into the business rules and make the services
 *               impossible to unit test.
 * SOLID       : Dependency Inversion — services depend on this abstraction and
 *               a test can supply a fake caller with any role or NIC.
 *               Interface Segregation — only the three facts the rules actually
 *               need are exposed, not the whole ClaimsPrincipal.
 * ---------------------------------------------------------------------------
 */

using SolarMicrogrid.Api.Models.Enums;

namespace SolarMicrogrid.Api.Security;

/// <summary>
/// The authenticated caller behind the current request.
/// </summary>
public interface ICurrentUser
{
    /// <summary>Document identifier of the caller, or null when anonymous.</summary>
    string? UserId { get; }

    /// <summary>National Identity Card number of the caller, or null when anonymous.</summary>
    string? Nic { get; }

    /// <summary>Role held by the caller, or null when anonymous.</summary>
    UserRole? Role { get; }

    /// <summary>True when a validated token was presented with the request.</summary>
    bool IsAuthenticated { get; }

    /// <summary>True when the caller holds the Backoffice role.</summary>
    bool IsBackoffice { get; }
}
