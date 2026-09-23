/*
 * ---------------------------------------------------------------------------
 * File        : StationResponseDto.cs
 * Project     : Smart Solar Microgrid Trading System — SE4040 Assignment 1
 * Author      : <Your Full Name> (<IT Number>)
 * Created     : 2026-09-19
 * Description : A microgrid node as returned to the web and mobile clients.
 * ---------------------------------------------------------------------------
 */

namespace SolarMicrogrid.Api.Dtos.Stations;

/// <summary>
/// A solar microgrid node.
/// </summary>
/// <param name="Id">Document identifier.</param>
/// <param name="StationCode">Human readable reference code.</param>
/// <param name="Name">Display name.</param>
/// <param name="Location">GPS position and address.</param>
/// <param name="CapacityKwh">Rated capacity in kilowatt hours.</param>
/// <param name="TotalBatterySlots">Battery storage slots installed.</param>
/// <param name="AvailableBatterySlots">Battery storage slots currently free.</param>
/// <param name="Schedule">Weekly operating hours.</param>
/// <param name="IsActive">False once the node has been deactivated.</param>
/// <param name="CreatedAt">UTC creation time.</param>
/// <param name="UpdatedAt">UTC time of the most recent change.</param>
public sealed record StationResponseDto(
    string Id,
    string StationCode,
    string Name,
    GeoLocationDto Location,
    double CapacityKwh,
    int TotalBatterySlots,
    int AvailableBatterySlots,
    IReadOnlyList<ScheduleEntryDto> Schedule,
    bool IsActive,
    DateTime CreatedAt,
    DateTime UpdatedAt);
