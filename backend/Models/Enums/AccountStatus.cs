/*
 * ---------------------------------------------------------------------------
 * File        : AccountStatus.cs
 * Project     : Smart Solar Microgrid Trading System — SE4040 Assignment 1
 * Author      : <Your Full Name> (<IT Number>)
 * Created     : 2026-09-17
 * Description : Lifecycle state of a user account. A prosumer who registers on the mobile application starts as Pending and cannot sign in until a back office officer activates the account.
 *
 * Code smell  : Replaces free text status strings. Using an enum means an
 *               invalid value cannot be represented at all, instead of being
 *               caught by a runtime check somewhere downstream.
 * ---------------------------------------------------------------------------
 */

namespace SolarMicrogrid.Api.Models.Enums;

/// <summary>
/// Lifecycle state of a user account.
/// </summary>
public enum AccountStatus
{
    /// <summary>Registered but not yet approved. Cannot sign in.</summary>
    Pending = 0,

    /// <summary>Approved and able to sign in.</summary>
    Active = 1,

    /// <summary>Disabled. Only a Backoffice officer may reactivate it.</summary>
    Deactivated = 2,
}
