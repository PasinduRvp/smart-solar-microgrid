/*
 * ---------------------------------------------------------------------------
 * File        : ApiControllerBase.cs
 * Project     : Smart Solar Microgrid Trading System — SE4040 Assignment 1
 * Author      : NIMSARA R V P P (IT 23215306)
 * Created     : 2026-09-19
 * Description : Base class for every controller in the API. Translates a
 *               ServiceResult returned by the service layer into the matching
 *               HTTP response.
 *
 * Why this    : Without it, each controller action would repeat the same
 *               switch over the error type. That duplication is a code smell,
 *               and a mistake in any one copy would produce the wrong status
 *               code on that endpoint alone. Written once, every endpoint in
 *               the project answers consistently.
 *
 * SOLID       : Single Responsibility — mapping outcomes to HTTP, nothing else.
 *               Open/Closed — a new controller inherits this behaviour without
 *               this file changing.
 *               Dependency Inversion in spirit: the service layer stays free of
 *               HTTP concepts, and this class is the only place the two meet.
 * ---------------------------------------------------------------------------
 */

using Microsoft.AspNetCore.Mvc;
using SolarMicrogrid.Api.Common;

namespace SolarMicrogrid.Api.Controllers;

/// <summary>
/// Shared behaviour for the API controllers.
/// </summary>
[ApiController]
[Produces("application/json")]
public abstract class ApiControllerBase : ControllerBase
{
    /// <summary>
    /// Returns 200 with the value when the operation succeeded, otherwise the
    /// status code matching the failure category.
    /// </summary>
    /// <typeparam name="TValue">Type carried by a successful result.</typeparam>
    /// <param name="result">Outcome produced by the service layer.</param>
    protected ActionResult<TValue> ToActionResult<TValue>(ServiceResult<TValue> result)
    {
        ArgumentNullException.ThrowIfNull(result);

        if (result.IsSuccess)
        {
            return Ok(result.Value);
        }

        return BuildProblem<TValue>(result);
    }

    /// <summary>
    /// Returns 201 Created without a Location header when the operation
    /// succeeded, otherwise the status code matching the failure category.
    /// </summary>
    /// <remarks>
    /// Used when the created resource has no endpoint the caller is allowed to
    /// retrieve it from. Prosumer self registration is the case in point: the
    /// account is created in the Pending state and only staff may read it, so
    /// pointing an anonymous caller at a Location it cannot follow would be
    /// misleading. A 201 without Location is valid HTTP.
    /// </remarks>
    /// <typeparam name="TValue">Type carried by a successful result.</typeparam>
    /// <param name="result">Outcome produced by the service layer.</param>
    protected ActionResult<TValue> ToCreatedResult<TValue>(ServiceResult<TValue> result)
    {
        ArgumentNullException.ThrowIfNull(result);

        if (result.IsSuccess)
        {
            return StatusCode(StatusCodes.Status201Created, result.Value);
        }

        return BuildProblem<TValue>(result);
    }

    /// <summary>
    /// Returns 201 Created with a Location header when the operation succeeded,
    /// otherwise the status code matching the failure category.
    /// </summary>
    /// <remarks>
    /// The named action must exist and must accept the supplied route values,
    /// otherwise ASP.NET throws "No route matches the supplied values" while
    /// building the header — after the record has already been written.
    /// </remarks>
    /// <typeparam name="TValue">Type carried by a successful result.</typeparam>
    /// <param name="result">Outcome produced by the service layer.</param>
    /// <param name="actionName">Action that can retrieve the new resource.</param>
    /// <param name="routeValues">Route values identifying the new resource.</param>
    protected ActionResult<TValue> ToCreatedResult<TValue>(
        ServiceResult<TValue> result,
        string actionName,
        object routeValues)
    {
        ArgumentNullException.ThrowIfNull(result);

        if (result.IsSuccess)
        {
            return CreatedAtAction(actionName, routeValues, result.Value);
        }

        return BuildProblem<TValue>(result);
    }

    /// <summary>
    /// Produces an RFC 7807 problem response for a failed service result.
    /// </summary>
    private ActionResult<TValue> BuildProblem<TValue>(ServiceResult<TValue> result)
    {
        // The service layer names the category; this method owns the HTTP
        // meaning of each one. Conflict is used for a violated business rule,
        // such as a duplicate NIC or a booking outside the permitted window.
        int statusCode = result.ErrorType switch
        {
            ServiceErrorType.Validation => StatusCodes.Status400BadRequest,
            ServiceErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
            ServiceErrorType.Forbidden => StatusCodes.Status403Forbidden,
            ServiceErrorType.NotFound => StatusCodes.Status404NotFound,
            ServiceErrorType.Conflict => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status400BadRequest,
        };

        // The message comes from the service and is written for the end user,
        // so the clients can display it directly. This is how the web and
        // Android apps show "cannot cancel within 12 hours" without either of
        // them containing that rule.
        ProblemDetails problem = new()
        {
            Status = statusCode,
            Title = ReasonFor(result.ErrorType),
            Detail = result.Error,
            Instance = HttpContext.Request.Path,
        };

        return StatusCode(statusCode, problem);
    }

    /// <summary>Short human readable title for a failure category.</summary>
    private static string ReasonFor(ServiceErrorType errorType) => errorType switch
    {
        ServiceErrorType.Validation => "Validation failed.",
        ServiceErrorType.Unauthorized => "Authentication failed.",
        ServiceErrorType.Forbidden => "Access denied.",
        ServiceErrorType.NotFound => "Not found.",
        ServiceErrorType.Conflict => "Request conflicts with the current state.",
        _ => "Request could not be completed.",
    };
}
