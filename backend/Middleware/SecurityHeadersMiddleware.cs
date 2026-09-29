/*
 * ---------------------------------------------------------------------------
 * File        : SecurityHeadersMiddleware.cs
 * Project     : Smart Solar Microgrid Trading System — SE4040 Assignment 1
 * Author      : NIMSARA R V P P (IT 23215306)
 * Created     : 2026-09-16
 * Description : Adds a small set of defensive HTTP response headers to every
 *               reply the API sends.
 *
 * SOLID       : Single Responsibility — response headers only.
 *               Open/Closed — applies to every present and future endpoint
 *               without any controller being changed.
 * Security    : X-Content-Type-Options stops a browser guessing a response is
 *               HTML or script when we said it was JSON (MIME sniffing).
 *               X-Frame-Options stops the API being framed by another site.
 *               Referrer-Policy keeps our URLs out of third party referrer
 *               logs. Cache-Control prevents shared caches and the browser
 *               from storing API responses, which for this system may contain
 *               a prosumer's NIC and booking history.
 * ---------------------------------------------------------------------------
 */

namespace SolarMicrogrid.Api.Middleware;

/// <summary>
/// Applies baseline security headers to all API responses.
/// </summary>
public sealed class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;

    /// <summary>Receives the next middleware in the pipeline.</summary>
    public SecurityHeadersMiddleware(RequestDelegate next)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
    }

    /// <summary>
    /// Attaches the headers before the response body is written, then continues.
    /// </summary>
    public async Task InvokeAsync(HttpContext context)
    {
        // OnStarting runs immediately before headers are flushed, which is the
        // last safe point to add them regardless of what produced the response.
        context.Response.OnStarting(() =>
        {
            IHeaderDictionary headers = context.Response.Headers;
            headers["X-Content-Type-Options"] = "nosniff";
            headers["X-Frame-Options"] = "DENY";
            headers["Referrer-Policy"] = "no-referrer";
            headers["Cache-Control"] = "no-store, no-cache, must-revalidate";
            return Task.CompletedTask;
        });

        await _next(context).ConfigureAwait(false);
    }
}
