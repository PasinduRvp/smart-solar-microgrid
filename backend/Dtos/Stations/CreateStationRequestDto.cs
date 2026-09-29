/*
 * ---------------------------------------------------------------------------
 * File        : CreateStationRequestDto.cs
 * Project     : Smart Solar Microgrid Trading System — SE4040 Assignment 1
 * Author      : NIMSARA R V P P (IT 23215306)
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
    /// <summary>
    /// Reference code, unique across nodes, for example "MG-COL-004".
    /// </summary>
    /// <remarks>
    /// Optional. Leave it out and the server generates the next free code
    /// from the node name, for example "Dehiwala Coastal Hub" becomes
    /// MG-DEH-001.
    ///
    /// It is generated on the server, not in the browser, because the code
    /// has to be unique and only the server can see every node. Two officers
    /// registering a node at the same moment would otherwise pick the same
    /// number, and the unique index would reject the second one.
    /// </remarks>
    [RegularExpression(@"^[A-Z0-9\-]{3,20}$", ErrorMessage =
        "Station code must be 3 to 20 characters of capital letters, digits or hyphens.")]
    public string? StationCode { get; init; }

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
