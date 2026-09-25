/*
 * ---------------------------------------------------------------------------
 * File        : HealthService.cs
 * Project     : Smart Solar Microgrid Trading System — SE4040 Assignment 1
 * Author      : NIMSARA R V P P (IT 23215306)
 * Created     : 2026-09-16
 * Description : Decides whether the service is healthy by pinging MongoDB
 *               through IMongoContext and assembling the response DTO.
 *
 *               This class exists so that the decision "what counts as
 *               healthy" lives in the service layer and not in the controller.
 *               The same separation is applied to every feature in this
 *               project: controllers handle HTTP, services hold the rules.
 *               That is the FAT service pattern the assignment requires — the
 *               web and Android clients never make this decision themselves.
 *
 * SOLID       : Single Responsibility — one reason to change, namely a change
 *               to what "healthy" means.
 *               Dependency Inversion — depends on IMongoContext and
 *               IHostEnvironment abstractions, both injected by the container.
 * ---------------------------------------------------------------------------
 */

using SolarMicrogrid.Api.Data;
using SolarMicrogrid.Api.Dtos;

namespace SolarMicrogrid.Api.Services;

/// <inheritdoc cref="IHealthService" />
public sealed class HealthService : IHealthService
{
    private const string HealthyStatus = "Healthy";
    private const string DegradedStatus = "Degraded";

    private readonly IMongoContext _mongoContext;
    private readonly IHostEnvironment _environment;

    /// <summary>
    /// Receives its dependencies from the DI container. Constructor injection
    /// makes the dependencies explicit and the class testable in isolation.
    /// </summary>
    public HealthService(IMongoContext mongoContext, IHostEnvironment environment)
    {
        _mongoContext = mongoContext ?? throw new ArgumentNullException(nameof(mongoContext));
        _environment = environment ?? throw new ArgumentNullException(nameof(environment));
    }

    /// <inheritdoc />
    public async Task<HealthResponseDto> CheckAsync(CancellationToken cancellationToken = default)
    {
        // The service is considered degraded rather than failed when the
        // database is unreachable: the process is alive and can still answer,
        // which is exactly the distinction we want visible during deployment.
        bool databaseConnected = await _mongoContext.PingAsync(cancellationToken).ConfigureAwait(false);

        return new HealthResponseDto(
            Status: databaseConnected ? HealthyStatus : DegradedStatus,
            DatabaseConnected: databaseConnected,
            DatabaseName: _mongoContext.DatabaseName,
            Environment: _environment.EnvironmentName,
            ServerTimeUtc: DateTime.UtcNow);
    }
}
