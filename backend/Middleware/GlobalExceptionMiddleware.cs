/*
 * ---------------------------------------------------------------------------
 * File        : GlobalExceptionMiddleware.cs
 * Project     : Smart Solar Microgrid Trading System — SE4040 Assignment 1
 * Author      : NIMSARA R V P P (IT 23215306)
 * Created     : 2026-09-16
 * Description : Catches any unhandled exception, records the full detail in the
 *               server log, and returns a generic RFC 7807 ProblemDetails
 *               response to the caller.
 *
 *               Having this in one place means no controller needs its own
 *               try/catch block for unexpected failures, which removes a large
 *               source of duplicated code from the rest of the project.
 *
 * SOLID       : Single Responsibility — turns exceptions into HTTP responses
 *               and does nothing else.
 *               Open/Closed — new controllers are covered automatically without
 *               this class being modified.
 * Security    : Stack traces, exception types and inner messages are written to
 *               the log but never to the response body. Returning them would be
 *               information disclosure: they reveal file paths, library
 *               versions and query structure that help an attacker. The caller
 *               receives a correlation id instead, which we can look up in the
 *               log when a user reports a failure.
 * ---------------------------------------------------------------------------
 */

using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace SolarMicrogrid.Api.Middleware;

/// <summary>
/// Converts unhandled exceptions into safe, consistent error responses.
/// </summary>
public sealed partial class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    /// <summary>Receives the next middleware in the pipeline and a logger.</summary>
    public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Passes the request down the pipeline and handles anything that escapes it.
    /// </summary>
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            await HandleAsync(context, ex).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Logs the failure in full and writes a generic problem response.
    /// </summary>
    private async Task HandleAsync(HttpContext context, Exception exception)
    {
        // Correlate the log entry with the response so a reported error can be
        // traced without the client ever seeing the underlying detail.
        string traceId = Activity.Current?.Id ?? context.TraceIdentifier;

        LogUnhandledException(exception, context.Request.Method, context.Request.Path, traceId);

        // If the response has already started we cannot safely rewrite it.
        if (context.Response.HasStarted)
        {
            return;
        }

        ProblemDetails problem = new()
        {
            Status = StatusCodes.Status500InternalServerError,
            Title = "An unexpected error occurred.",
            Detail = "The request could not be completed. Quote the trace id when reporting this.",
            Instance = context.Request.Path,
        };
        problem.Extensions["traceId"] = traceId;

        context.Response.Clear();
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        context.Response.ContentType = "application/problem+json";

        await context.Response.WriteAsJsonAsync(problem).ConfigureAwait(false);
    }

    // -----------------------------------------------------------------------
    // Source generated log method. See the note in MongoContext.cs for why the
    // project logs through [LoggerMessage] rather than calling ILogger directly.
    // -----------------------------------------------------------------------

    [LoggerMessage(
        EventId = 2001,
        Level = LogLevel.Error,
        Message = "Unhandled exception for {Method} {Path}. TraceId {TraceId}.")]
    private partial void LogUnhandledException(
        Exception exception,
        string method,
        string path,
        string traceId);
}
