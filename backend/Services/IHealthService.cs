/*
 * ---------------------------------------------------------------------------
 * File        : IHealthService.cs
 * Project     : Smart Solar Microgrid Trading System — SE4040 Assignment 1
 * Author      : NIMSARA R V P P (IT 23215306)
 * Created     : 2026-09-16
 * Description : Contract for the service that determines the health of the
 *               application and its database connection.
 *
 * SOLID       : Dependency Inversion — HealthController depends on this
 *               abstraction rather than on a concrete implementation, so the
 *               controller can be tested without a live MongoDB server.
 *               Interface Segregation — one focused operation per interface
 *               rather than a single "IService" containing everything.
 * ---------------------------------------------------------------------------
 */

using SolarMicrogrid.Api.Dtos;

namespace SolarMicrogrid.Api.Services;

/// <summary>
/// Reports whether the service and its dependencies are operational.
/// </summary>
public interface IHealthService
{
    /// <summary>
    /// Checks the database connection and builds the health response.
    /// </summary>
    Task<HealthResponseDto> CheckAsync(CancellationToken cancellationToken = default);
}
