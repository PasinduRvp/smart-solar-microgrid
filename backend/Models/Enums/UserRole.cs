/*
 * ---------------------------------------------------------------------------
 * File        : UserRole.cs
 * Project     : Smart Solar Microgrid Trading System — SE4040 Assignment 1
 * Author      : <Your Full Name> (<IT Number>)
 * Created     : 2026-09-17
 * Description : The three kinds of account the system recognises. Determines which endpoints an authenticated caller may reach.
 *
 * Code smell  : Replaces free text status strings. Using an enum means an
 *               invalid value cannot be represented at all, instead of being
 *               caught by a runtime check somewhere downstream.
 * ---------------------------------------------------------------------------
 */

namespace SolarMicrogrid.Api.Models.Enums;

/// <summary>
/// Role assigned to a user account, used for role based authorisation.
/// </summary>
public enum UserRole
{
    /// <summary>Back office officer. Full system administration rights.</summary>
    Backoffice = 0,

    /// <summary>Grid operator. Operational tools on both web and mobile.</summary>
    GridOperator = 1,

    /// <summary>Solar prosumer. Mobile application only.</summary>
    Prosumer = 2,
}
