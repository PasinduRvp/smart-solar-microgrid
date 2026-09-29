/*
 * ---------------------------------------------------------------------------
 * File        : CorsSettings.cs
 * Project     : Smart Solar Microgrid Trading System — SE4040 Assignment 1
 * Author      : NIMSARA R V P P (IT 23215306)
 * Created     : 2026-09-16
 * Description : Strongly typed configuration listing the browser origins that
 *               are permitted to call this API. Used to build a named CORS
 *               policy at startup.
 *
 * SOLID       : Single Responsibility — carries CORS configuration only.
 * Security    : The allowed origins are an explicit whitelist. We deliberately
 *               do NOT use AllowAnyOrigin, because this API issues and accepts
 *               bearer tokens; an open origin policy would let any website run
 *               authenticated requests against the service on behalf of a
 *               signed-in user.
 * ---------------------------------------------------------------------------
 */

namespace SolarMicrogrid.Api.Configuration;

/// <summary>
/// Origins allowed to call this API from a browser.
/// </summary>
public sealed class CorsSettings
{
    /// <summary>Name of the configuration section these settings are bound from.</summary>
    public const string SectionName = "Cors";

    /// <summary>Name of the CORS policy registered in the DI container.</summary>
    public const string PolicyName = "SolarMicrogridWebClient";

    /// <summary>
    /// Exact origins permitted to call the API, e.g. "http://localhost:5173"
    /// for the Vite dev server. The Android client is not a browser and is
    /// therefore unaffected by CORS.
    /// </summary>
    public string[] AllowedOrigins { get; init; } = [];
}
