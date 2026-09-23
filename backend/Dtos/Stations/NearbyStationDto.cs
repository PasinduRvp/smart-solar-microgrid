/*
 * ---------------------------------------------------------------------------
 * File        : NearbyStationDto.cs
 * Project     : Smart Solar Microgrid Trading System — SE4040 Assignment 1
 * Author      : <Your Full Name> (<IT Number>)
 * Created     : 2026-09-19
 * Description : A microgrid node together with how far it is from the phone.
 *               Returned by the nearby search that backs the Android map screen.
 *
 * Design note : Distance is computed by the API rather than by the client. The
 *               assignment requires business logic to live in the service, and
 *               it also means the web application and the Android application
 *               can never disagree about which node is closest.
 * ---------------------------------------------------------------------------
 */

namespace SolarMicrogrid.Api.Dtos.Stations;

/// <summary>
/// A microgrid node and its distance from the search point.
/// </summary>
/// <param name="Station">The node itself.</param>
/// <param name="DistanceKm">Straight line distance in kilometres, to two decimal places.</param>
public sealed record NearbyStationDto(
    StationResponseDto Station,
    double DistanceKm);
