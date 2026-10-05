/*
 * ---------------------------------------------------------------------------
 * File        : GeoLocation.cs
 * Project     : Smart Solar Microgrid Trading System — SE4040 Assignment 1
 * Author      : NIMSARA R V P P (IT 23215306)
 * Created     : 2026-09-17
 * Description : GPS position of a microgrid node. Used by the Android client to
 *               plot nearby stations on Google Maps.
 *
 * OOP         : A value object. Latitude and longitude are meaningless apart
 *               from one another, so they are modelled as one concept rather
 *               than two loose fields on the station. This is composition:
 *               a station HAS A location.
 * Code smell  : Avoids "primitive obsession", where related primitives are
 *               scattered across a class instead of being grouped into a type.
 * ---------------------------------------------------------------------------
 */

using MongoDB.Bson.Serialization.Attributes;

namespace SolarMicrogrid.Api.Models;

/// <summary>
/// A geographic position together with its human readable address.
/// </summary>
public sealed class GeoLocation
{
    /// <summary>Latitude in decimal degrees. Valid range is -90 to 90.</summary>
    [BsonElement("latitude")]
    public double Latitude { get; set; }

    /// <summary>Longitude in decimal degrees. Valid range is -180 to 180.</summary>
    [BsonElement("longitude")]
    public double Longitude { get; set; }

    /// <summary>Street address shown to the user alongside the map pin.</summary>
    [BsonElement("addressLine")]
    public string AddressLine { get; set; } = string.Empty;
}
