/*
 * ---------------------------------------------------------------------------
 * File        : ReservationStatus.cs
 * Project     : Smart Solar Microgrid Trading System — SE4040 Assignment 1
 * Author      : <Your Full Name> (<IT Number>)
 * Created     : 2026-09-17
 * Description : Lifecycle state of an energy slot reservation, from request through to completed energy transfer.
 *
 * Code smell  : Replaces free text status strings. Using an enum means an
 *               invalid value cannot be represented at all, instead of being
 *               caught by a runtime check somewhere downstream.
 * ---------------------------------------------------------------------------
 */

namespace SolarMicrogrid.Api.Models.Enums;

/// <summary>
/// Lifecycle state of an energy slot reservation.
/// </summary>
public enum ReservationStatus
{
    /// <summary>Requested by the prosumer, awaiting approval.</summary>
    Pending = 0,

    /// <summary>Approved. A QR token has been issued for the transfer.</summary>
    Approved = 1,

    /// <summary>Energy transfer finalised by a grid operator.</summary>
    Completed = 2,

    /// <summary>Cancelled by the prosumer or by an operator.</summary>
    Cancelled = 3,
}
