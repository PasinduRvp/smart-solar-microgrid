/*
 * ---------------------------------------------------------------------------
 * File        : CreateStationRequestDto.cs
 * Project     : Smart Solar Microgrid Trading System — SE4040 Assignment 1
 * Author      : <Your Full Name> (<IT Number>)
 * Created     : 2026-09-19
 * Description : Details a Backoffice officer supplies when registering a new
 *               microgrid node, as the assignment requires: GPS location,
 *               capacity in kW/h, and the available battery storage slots.
 * ---------------------------------------------------------------------------
 */

using System.ComponentModel.DataAnnotations;

namespace SolarMicrogrid.Api.Dtos.Stations;

/// <summary>
/// Details for a new microgrid node.
/// </summary>
public sealed class CreateStationRequestDto
{
    /// <summary>Reference code, unique across nodes, for example "MG-COL-004".</summary>
    [Required(AllowEmptyStrings = false, ErrorMessage = "Station code is required.")]
    [RegularExpression(@"^[A-Z0-9\-]{3,20}$", ErrorMessage =
        "Station code must be 3 to 20 characters of capital letters, digits or hyphens.")]
    public string StationCode { get; init; } = string.Empty;

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

    /// <summary>
    /// Weekly operating hours. May be left empty and set later through the
    /// schedule endpoint, but slots cannot be generated until it is populated.
    /// </summary>
    public IList<ScheduleEntryDto> Schedule { get; init; } = new List<ScheduleEntryDto>();
}
