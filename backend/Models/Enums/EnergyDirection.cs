/*
 * ---------------------------------------------------------------------------
 * File        : EnergyDirection.cs
 * Project     : Smart Solar Microgrid Trading System — SE4040 Assignment 1
 * Author      : <Your Full Name> (<IT Number>)
 * Created     : 2026-09-17
 * Description : Which way the energy flows in a trade: the prosumer either feeds power into the microgrid or draws it out.
 *
 * Code smell  : Replaces free text status strings. Using an enum means an
 *               invalid value cannot be represented at all, instead of being
 *               caught by a runtime check somewhere downstream.
 * ---------------------------------------------------------------------------
 */

namespace SolarMicrogrid.Api.Models.Enums;

/// <summary>
/// Direction of energy flow for a reservation.
/// </summary>
public enum EnergyDirection
{
    /// <summary>Prosumer delivers surplus solar energy into the microgrid.</summary>
    Deliver = 0,

    /// <summary>Prosumer draws energy from the microgrid, e.g. battery charging.</summary>
    Draw = 1,
}
