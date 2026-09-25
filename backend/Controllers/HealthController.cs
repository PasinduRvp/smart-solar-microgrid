/*
 * ---------------------------------------------------------------------------
 * File        : HealthController.cs
 * Project     : Smart Solar Microgrid Trading System — SE4040 Assignment 1
 * Author      : NIMSARA R V P P (IT 23215306)
 * Created     : 2026-09-16
 * Description : Exposes GET /api/health so that the React web application, the
 *               Android application and the IIS deployment check can all
 *               confirm the service is running and connected to MongoDB.
 *
 *               Note how thin this class is. It receives the request, calls the
 *               service, and maps the outcome to an HTTP status code. It holds
 *               no rules of its own. Every controller in this project follows
 *               the same shape, which is what keeps business logic inside the
 *               API rather than leaking towards the clients.
 *
 * SOLID       : Single Responsibility — HTTP concerns only.
 *               Dependency Inversion — depends on IHealthService, not on the
 *               concrete HealthService or on MongoDB.
 * Security    : Left unauthenticated on purpose so deployment can be verified
 *               before any user exists, and therefore returns no sensitive
 *               detail. See HealthResponseDto for what is deliberately omitted.
 * ---------------------------------------------------------------------------
 */

using Microsoft.AspNetCore.Mvc;
using SolarMicrogrid.Api.Dtos;
using SolarMicrogrid.Api.Services;

namespace SolarMicrogrid.Api.Controllers;

/// <summary>
/// Service and database availability checks.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public sealed class HealthController : ControllerBase
{
    private readonly IHealthService _healthService;

    /// <summary>Receives the health service from the DI container.</summary>
    public HealthController(IHealthService healthService)
    {
        _healthService = healthService ?? throw new ArgumentNullException(nameof(healthService));
    }

    /// <summary>
    /// Reports whether the API is running and whether MongoDB is reachable.
    /// </summary>
    /// <response code="200">The service and the database are both available.</response>
    /// <response code="503">The service is running but the database is unreachable.</response>
    [HttpGet]
    [ProducesResponseType(typeof(HealthResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HealthResponseDto), StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<HealthResponseDto>> GetAsync(CancellationToken cancellationToken)
    {
        // Ask the service for the verdict, then translate that verdict into the
        // HTTP status code that monitoring tools and IIS expect to see.
        HealthResponseDto health = await _healthService.CheckAsync(cancellationToken).ConfigureAwait(false);

        return health.DatabaseConnected
            ? Ok(health)
            : StatusCode(StatusCodes.Status503ServiceUnavailable, health);
    }
}
