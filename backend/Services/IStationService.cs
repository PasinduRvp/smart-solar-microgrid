/*
 * ---------------------------------------------------------------------------
 * File        : IStationService.cs
 * Project     : Smart Solar Microgrid Trading System — SE4040 Assignment 1
 * Author      : <Your Full Name> (<IT Number>)
 * Created     : 2026-09-19
 * Description : Contract for managing solar microgrid nodes: registration, GPS
 *               and capacity details, weekly schedules, the nearby search used
 *               by the Android map, and deactivation.
 *
 * Business    : BR-4  A node cannot be deactivated while it still has active
 *                     reservations.
 * ---------------------------------------------------------------------------
 */

using SolarMicrogrid.Api.Common;
using SolarMicrogrid.Api.Dtos.Stations;

namespace SolarMicrogrid.Api.Services;

/// <summary>
/// Manages solar microgrid nodes.
/// </summary>
public interface IStationService
{
    /// <summary>
    /// Returns nodes, optionally limited to those still in service.
    /// </summary>
    /// <param name="activeOnly">True to exclude deactivated nodes.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<ServiceResult<IReadOnlyList<StationResponseDto>>> GetAllAsync(
        bool activeOnly,
        CancellationToken cancellationToken = default);

    /// <summary>Returns one node by its document identifier.</summary>
    Task<ServiceResult<StationResponseDto>> GetByIdAsync(
        string id,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns active nodes within a radius of a position, nearest first.
    /// Backs the map screen in the Android application.
    /// </summary>
    /// <param name="latitude">Latitude of the search point, in degrees.</param>
    /// <param name="longitude">Longitude of the search point, in degrees.</param>
    /// <param name="radiusKm">Search radius in kilometres.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<ServiceResult<IReadOnlyList<NearbyStationDto>>> GetNearbyAsync(
        double latitude,
        double longitude,
        double radiusKm,
        CancellationToken cancellationToken = default);

    /// <summary>Registers a new node.</summary>
    Task<ServiceResult<StationResponseDto>> CreateAsync(
        CreateStationRequestDto request,
        CancellationToken cancellationToken = default);

    /// <summary>Updates the details of an existing node.</summary>
    Task<ServiceResult<StationResponseDto>> UpdateAsync(
        string id,
        UpdateStationRequestDto request,
        CancellationToken cancellationToken = default);

    /// <summary>Replaces the weekly operating schedule of a node.</summary>
    Task<ServiceResult<StationResponseDto>> UpdateScheduleAsync(
        string id,
        UpdateScheduleRequestDto request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Takes a node out of service. BR-4: refused while the node still has
    /// Pending or Approved reservations in the future.
    /// </summary>
    Task<ServiceResult<StationResponseDto>> DeactivateAsync(
        string id,
        CancellationToken cancellationToken = default);

    /// <summary>Returns a deactivated node to service.</summary>
    Task<ServiceResult<StationResponseDto>> ReactivateAsync(
        string id,
        CancellationToken cancellationToken = default);
}
