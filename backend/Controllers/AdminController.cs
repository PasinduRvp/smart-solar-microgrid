/*
 * ---------------------------------------------------------------------------
 * File        : AdminController.cs
 * Project     : Smart Solar Microgrid Trading System — SE4040 Assignment 1
 * Author      : NIMSARA R V P P (IT 23215306)
 * Created     : 2026-09-19
 * Description : Administrative utilities. At present this exposes the
 *               demonstration data seeder, which fills every collection with a
 *               realistic sample set.
 *
 * Security    : This endpoint writes a lot of data and creates accounts with a
 *               known password, so it is protected twice over:
 *                 1. It requires the Backoffice role, like every other
 *                    administrative endpoint.
 *                 2. It is switched off unless Seed:AllowDemoDataEndpoint is
 *                    true in configuration, which is false in the committed
 *                    appsettings.json. Role alone would not be enough — a
 *                    forgotten flag is far more visible than a forgotten
 *                    attribute, and on a real deployment the endpoint should
 *                    not merely be restricted but absent.
 * ---------------------------------------------------------------------------
 */

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SolarMicrogrid.Api.Common;
using SolarMicrogrid.Api.Services;

namespace SolarMicrogrid.Api.Controllers;

/// <summary>
/// Administrative utilities for setting up a demonstration environment.
/// </summary>
[Route("api/[controller]")]
[Authorize(Roles = Roles.Backoffice)]
public sealed class AdminController : ApiControllerBase
{
    private readonly IDemoDataSeeder _demoDataSeeder;
    private readonly IConfiguration _configuration;

    /// <summary>Receives the seeder and configuration from the DI container.</summary>
    public AdminController(IDemoDataSeeder demoDataSeeder, IConfiguration configuration)
    {
        _demoDataSeeder = demoDataSeeder ?? throw new ArgumentNullException(nameof(demoDataSeeder));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    }

    /// <summary>
    /// Fills the database with demonstration data across all four collections.
    /// </summary>
    /// <remarks>
    /// Creates Grid Operators, prosumers in every account state, four microgrid
    /// nodes with weekly schedules, a week of booking windows and reservations
    /// in each status — including one inside the twelve hour notice period so
    /// the BR-2 and BR-3 refusals can be demonstrated straight away.
    ///
    /// Safe to run more than once: anything that already exists is left alone.
    /// </remarks>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="200">A summary of what was created.</response>
    /// <response code="403">The caller is not a Backoffice officer.</response>
    /// <response code="404">The endpoint is disabled in configuration.</response>
    [HttpPost("seed-demo-data")]
    [ProducesResponseType(typeof(IReadOnlyList<string>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<string>>> SeedDemoDataAsync(
        CancellationToken cancellationToken)
    {
        // Reported as 404 rather than 403 when disabled. A disabled endpoint
        // should look as though it does not exist, instead of confirming that
        // a seeding facility is present but switched off.
        if (!_configuration.GetValue<bool>("Seed:AllowDemoDataEndpoint"))
        {
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Not found.",
                Instance = HttpContext.Request.Path,
            });
        }

        ServiceResult<IReadOnlyList<string>> result =
            await _demoDataSeeder.SeedAsync(cancellationToken).ConfigureAwait(false);

        return ToActionResult(result);
    }
}
