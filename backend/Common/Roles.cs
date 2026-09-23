/*
 * ---------------------------------------------------------------------------
 * File        : Roles.cs
 * Project     : Smart Solar Microgrid Trading System — SE4040 Assignment 1
 * Author      : <Your Full Name> (<IT Number>)
 * Created     : 2026-09-19
 * Description : Role names as constants, for use in [Authorize(Roles = ...)].
 *
 * Code smell  : Attribute arguments must be compile time constants, so the
 *               UserRole enum cannot be used directly in [Authorize]. Without
 *               these constants every controller would repeat string literals
 *               such as "Backoffice", and a single typo would silently open an
 *               endpoint to nobody — or, worse, fail to close it. The unit test
 *               style guard below keeps the constants and the enum in step.
 * ---------------------------------------------------------------------------
 */

using SolarMicrogrid.Api.Models.Enums;

namespace SolarMicrogrid.Api.Common;

/// <summary>
/// Role names used by the authorisation attributes.
/// </summary>
public static class Roles
{
    /// <summary>Back office officer. Full administrative rights.</summary>
    public const string Backoffice = nameof(UserRole.Backoffice);

    /// <summary>Grid operator. Operational tools on web and mobile.</summary>
    public const string GridOperator = nameof(UserRole.GridOperator);

    /// <summary>Solar prosumer. Mobile application only.</summary>
    public const string Prosumer = nameof(UserRole.Prosumer);

    /// <summary>Convenience for endpoints open to either kind of staff account.</summary>
    public const string BackofficeOrGridOperator = Backoffice + "," + GridOperator;
}
