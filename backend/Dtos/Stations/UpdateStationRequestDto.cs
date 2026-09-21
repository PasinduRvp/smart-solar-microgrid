/*
 * ---------------------------------------------------------------------------
 * File        : UpdateStationRequestDto.cs
 * Project     : Smart Solar Microgrid Trading System — SE4040 Assignment 1
 * Author      : <Your Full Name> (<IT Number>)
 * Created     : 2026-09-19
 * Description : Editable details of an existing microgrid node.
 *
 * Security    : StationCode and IsActive are absent on purpose. The code is the
 *               node's stable reference, and activation state is changed only
 *               through the dedicated deactivate endpoint, which enforces BR-4.
 *               Allowing IsActive to be set here would let a caller sidestep
 *               that rule with an ordinary update.
 * ---------------------------------------------------------------------------
 */

using System.ComponentModel.DataAnnotations;

namespace SolarMicrogrid.Api.Dtos.Stations;

/// <summary>
/// Details that may be changed on an existing node.
/// </summary>
public sealed class UpdateStationRequestDto
{
    /// <summary>Display name of the node.</summary>
    [Required(AllowEmptyStrings = false, ErrorMessage = "Station name is required.")]
    [MaxLength(120)]
    public string Name { get; init; } = string.Empty;

    /// <summary>GPS position and street address.</summary>
    [Required(ErrorMessage = "Location is required.")]
    public GeoLocationDto Location { get; init; } = new();

    /// <summary>Rated capacity of the node in kilowatt hours.</summary>
    [Range(0.1, 100000, ErrorMessage = "Capacity must be between 0.1 and 100000 kWh.")]
    public double CapacityKwh { get; init; }

    /// <summary>Number of battery storage slots physically installed.</summary>
    [Range(1, 500, ErrorMessage = "Total battery slots must be between 1 and 500.")]
    public int TotalBatterySlots { get; init; }

    /// <summary>Battery storage slots currently free. Maintained by grid operators.</summary>
    [Range(0, 500, ErrorMessage = "Available battery slots must be between 0 and 500.")]
    public int AvailableBatterySlots { get; init; }
}
