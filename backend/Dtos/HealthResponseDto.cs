/*
 * ---------------------------------------------------------------------------
 * File        : HealthResponseDto.cs
 * Project     : Smart Solar Microgrid Trading System — SE4040 Assignment 1
 * Author      : NIMSARA R V P P (IT 23215306)
 * Created     : 2026-09-16
 * Description : Response shape returned by GET /api/health. Reports whether
 *               the service is running and whether it can reach MongoDB, so
 *               both client applications (and the IIS deployment check) can
 *               confirm the service is wired up correctly.
 *
 * SOLID       : Single Responsibility — a transport contract only, with no
 *               behaviour attached.
 * Security    : Deliberately excludes the cluster host, credentials, driver
 *               version and machine name. Health endpoints are usually
 *               unauthenticated, so anything returned here is public; we
 *               expose the database NAME only, never how to reach it.
 * ---------------------------------------------------------------------------
 */

namespace SolarMicrogrid.Api.Dtos;

/// <summary>
/// Outcome of a service health check.
/// </summary>
/// <param name="Status">"Healthy" when the database responded, otherwise "Degraded".</param>
/// <param name="DatabaseConnected">True when MongoDB answered the ping.</param>
/// <param name="DatabaseName">Name of the database in use. Carries no credentials.</param>
/// <param name="Environment">Hosting environment name, e.g. Development or Production.</param>
/// <param name="ServerTimeUtc">Server clock in UTC — useful when debugging the 7-day and 12-hour booking rules.</param>
public sealed record HealthResponseDto(
    string Status,
    bool DatabaseConnected,
    string DatabaseName,
    string Environment,
    DateTime ServerTimeUtc);
