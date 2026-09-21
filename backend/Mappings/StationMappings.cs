/*
 * ---------------------------------------------------------------------------
 * File        : StationMappings.cs
 * Project     : Smart Solar Microgrid Trading System — SE4040 Assignment 1
 * Author      : <Your Full Name> (<IT Number>)
 * Created     : 2026-09-19
 * Description : Converts between microgrid node entities and their DTOs, in
 *               both directions.
 *
 * Code smell  : Written once and used by every station endpoint, so the schedule
 *               and location projections are not repeated per action.
 * SOLID       : Single Responsibility — translation between layers only.
 * ---------------------------------------------------------------------------
 */

using SolarMicrogrid.Api.Dtos.Stations;
using SolarMicrogrid.Api.Models;

namespace SolarMicrogrid.Api.Mappings;

/// <summary>
/// Projections between <see cref="SolarStationInfo"/> and its DTOs.
/// </summary>
public static class StationMappings
{
    /// <summary>Builds the response DTO for a node.</summary>
    public static StationResponseDto ToResponseDto(this SolarStationInfo station)
    {
        ArgumentNullException.ThrowIfNull(station);

        return new StationResponseDto(
            Id: station.Id ?? string.Empty,
            StationCode: station.StationCode,
            Name: station.Name,
            Location: new GeoLocationDto
            {
                Latitude = station.Location.Latitude,
                Longitude = station.Location.Longitude,
                AddressLine = station.Location.AddressLine,
            },
            CapacityKwh: station.CapacityKwh,
            TotalBatterySlots: station.TotalBatterySlots,
            AvailableBatterySlots: station.AvailableBatterySlots,
            Schedule: station.Schedule.Select(ToScheduleDto).ToList(),
            IsActive: station.IsActive,
            CreatedAt: station.CreatedAt,
            UpdatedAt: station.UpdatedAt);
    }

    /// <summary>Builds response DTOs for a collection of nodes.</summary>
    public static IReadOnlyList<StationResponseDto> ToResponseDtos(this IEnumerable<SolarStationInfo> stations)
    {
        ArgumentNullException.ThrowIfNull(stations);
        return stations.Select(station => station.ToResponseDto()).ToList();
    }

    /// <summary>Converts a schedule entry entity into its DTO.</summary>
    public static ScheduleEntryDto ToScheduleDto(this ScheduleEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);

        return new ScheduleEntryDto
        {
            DayOfWeek = entry.DayOfWeek.ToString(),
            OpenTime = entry.OpenTime,
            CloseTime = entry.CloseTime,
        };
    }

    /// <summary>
    /// Converts a schedule entry DTO into its entity.
    /// The day name has already been constrained by the DTO's regular
    /// expression, so the parse below cannot realistically fail; Monday is used
    /// as a safe fallback rather than allowing an exception to escape.
    /// </summary>
    public static ScheduleEntry ToEntity(this ScheduleEntryDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        DayOfWeek day = Enum.TryParse(dto.DayOfWeek, ignoreCase: true, out DayOfWeek parsed)
            ? parsed
            : DayOfWeek.Monday;

        return new ScheduleEntry
        {
            DayOfWeek = day,
            OpenTime = dto.OpenTime,
            CloseTime = dto.CloseTime,
        };
    }

    /// <summary>Converts a location DTO into its entity.</summary>
    public static GeoLocation ToEntity(this GeoLocationDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        return new GeoLocation
        {
            Latitude = dto.Latitude,
            Longitude = dto.Longitude,
            AddressLine = dto.AddressLine.Trim(),
        };
    }
}
