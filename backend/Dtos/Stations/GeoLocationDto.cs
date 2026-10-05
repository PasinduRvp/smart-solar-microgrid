/*
 * ---------------------------------------------------------------------------
 * File        : GeoLocationDto.cs
 * Project     : Smart Solar Microgrid Trading System — SE4040 Assignment 1
 * Author      : NIMSARA R V P P (IT 23215306)
 * Created     : 2026-09-19
 * Description : GPS position of a microgrid node as sent and received by the
 *               clients. The Android application plots these on Google Maps.
 *
 * Validation  : The latitude and longitude ranges are enforced here so an
 *               impossible coordinate is rejected with a 400 before it can be
 *               stored and later placed on a map in the middle of the ocean.
 * ---------------------------------------------------------------------------
 */

using System.ComponentModel.DataAnnotations;

namespace SolarMicrogrid.Api.Dtos.Stations;

/// <summary>
/// A geographic position with its street address.
/// </summary>
public sealed class GeoLocationDto
{
    /// <summary>Latitude in decimal degrees.</summary>
    [Range(-90, 90, ErrorMessage = "Latitude must be between -90 and 90.")]
    public double Latitude { get; init; }

    /// <summary>Longitude in decimal degrees.</summary>
    [Range(-180, 180, ErrorMessage = "Longitude must be between -180 and 180.")]
    public double Longitude { get; init; }

    /// <summary>Street address shown beside the map pin.</summary>
    [Required(AllowEmptyStrings = false, ErrorMessage = "Address line is required.")]
    [MaxLength(200)]
    public string AddressLine { get; init; } = string.Empty;
}
