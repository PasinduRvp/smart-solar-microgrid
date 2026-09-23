/*
 * ---------------------------------------------------------------------------
 * File        : GeoDistance.cs
 * Project     : Smart Solar Microgrid Trading System — SE4040 Assignment 1
 * Author      : <Your Full Name> (<IT Number>)
 * Created     : 2026-09-19
 * Description : Great circle distance between two GPS positions, used by the
 *               nearby node search that backs the Android map screen.
 *
 * Method      : The haversine formula, which treats the Earth as a sphere and
 *               returns the shortest distance over its surface. It is accurate
 *               to roughly 0.5 percent, which is far better than this feature
 *               needs when ranking nodes a few kilometres apart.
 *
 * Design note : An alternative was to store positions as GeoJSON points, add a
 *               2dsphere index and let MongoDB answer with $near. That scales
 *               better because the database can use the index instead of
 *               examining every node. It was not chosen here because this
 *               system has a small, fixed number of nodes, and computing the
 *               distance in C# keeps the entity model simple and the rule
 *               visible in code the viva can be walked through. With thousands
 *               of nodes the geospatial index would be the right choice.
 * ---------------------------------------------------------------------------
 */

namespace SolarMicrogrid.Api.Common;

/// <summary>
/// Distance calculations between geographic positions.
/// </summary>
public static class GeoDistance
{
    /// <summary>Mean radius of the Earth in kilometres.</summary>
    private const double EarthRadiusKm = 6371.0088;

    /// <summary>
    /// Returns the great circle distance in kilometres between two positions.
    /// </summary>
    /// <param name="latitude1">Latitude of the first point, in degrees.</param>
    /// <param name="longitude1">Longitude of the first point, in degrees.</param>
    /// <param name="latitude2">Latitude of the second point, in degrees.</param>
    /// <param name="longitude2">Longitude of the second point, in degrees.</param>
    public static double KilometresBetween(
        double latitude1,
        double longitude1,
        double latitude2,
        double longitude2)
    {
        double lat1Rad = DegreesToRadians(latitude1);
        double lat2Rad = DegreesToRadians(latitude2);
        double deltaLat = DegreesToRadians(latitude2 - latitude1);
        double deltaLon = DegreesToRadians(longitude2 - longitude1);

        // Haversine: a is the square of half the chord length between the
        // points, and c is the angular distance in radians.
        double a = (Math.Sin(deltaLat / 2) * Math.Sin(deltaLat / 2))
            + (Math.Cos(lat1Rad) * Math.Cos(lat2Rad)
               * Math.Sin(deltaLon / 2) * Math.Sin(deltaLon / 2));

        double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

        return EarthRadiusKm * c;
    }

    /// <summary>Converts an angle in degrees to radians.</summary>
    private static double DegreesToRadians(double degrees) => degrees * Math.PI / 180.0;
}
