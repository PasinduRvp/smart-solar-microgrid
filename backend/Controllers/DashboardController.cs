/*
 * ---------------------------------------------------------------------------
 * File        : DashboardController.cs
 * Project     : Smart Solar Microgrid Trading System — SE4040 Assignment 1
 * Author      : L K P Yasith (IT 23380196)
 * Created     : 2026-09-19
 * Description : The two dashboard endpoints. The prosumer summary backs the
 *               home screen of the Android application; the operator summary
 *               backs the home screen of the React web application for both
 *               Grid Operators and Backoffice officers.
 *
 * Security    : A prosumer may request only their own summary. The role check
 *               here admits prosumers and staff alike, and the service then
 *               compares the requested NIC against the one in the signed token.
 * ---------------------------------------------------------------------------
 */

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SolarMicrogrid.Api.Common;
using SolarMicrogrid.Api.Dtos.Dashboard;
using SolarMicrogrid.Api.Services;

namespace SolarMicrogrid.Api.Controllers;

/// <summary>
/// Summary figures for the prosumer and staff home screens.
/// </summary>
[Route("api/[controller]")]
[Authorize]
public sealed class DashboardController : ApiControllerBase
{
    private readonly IDashboardService _dashboardService;

    /// <summary>Receives the dashboard service from the DI container.</summary>
    public DashboardController(IDashboardService dashboardService)
    {
        _dashboardService = dashboardService ?? throw new ArgumentNullException(nameof(dashboardService));
    }

    /// <summary>
    /// Returns the summary for one prosumer.
    /// </summary>
    /// <remarks>
    /// Carries the two figures the assignment names: the number of pending
    /// reservations, and the count of approved reservations still in the
    /// future. Both are counted by the database on every request, so the
    /// screen never shows a stale or hard-coded number.
    /// </remarks>
    /// <param name="nic">NIC of the prosumer.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="200">The prosumer's summary figures.</response>
    /// <response code="403">A prosumer asked for someone else's dashboard.</response>
    /// <response code="404">No prosumer with that NIC.</response>
    [HttpGet("prosumer/{nic}")]
    [ProducesResponseType(typeof(ProsumerDashboardDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProsumerDashboardDto>> GetProsumerDashboardAsync(
        string nic,
        CancellationToken cancellationToken)
    {
        ServiceResult<ProsumerDashboardDto> result =
            await _dashboardService.GetProsumerDashboardAsync(nic, cancellationToken).ConfigureAwait(false);

        return ToActionResult(result);
    }

    /// <summary>
    /// Returns the summary shown to Grid Operators and Backoffice officers.
    /// </summary>
    /// <remarks>
    /// Combines the work waiting on staff — bookings to approve and prosumer
    /// accounts to activate — with today's schedule and the load at each node.
    /// </remarks>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="200">The staff summary figures.</response>
    /// <response code="403">The caller is not a staff member.</response>
    [HttpGet("operator")]
    [Authorize(Roles = Roles.BackofficeOrGridOperator)]
    [ProducesResponseType(typeof(OperatorDashboardDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<OperatorDashboardDto>> GetOperatorDashboardAsync(
        CancellationToken cancellationToken)
    {
        ServiceResult<OperatorDashboardDto> result =
            await _dashboardService.GetOperatorDashboardAsync(cancellationToken).ConfigureAwait(false);

        return ToActionResult(result);
    }
}
