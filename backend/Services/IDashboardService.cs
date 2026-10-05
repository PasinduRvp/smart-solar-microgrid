/*
 * ---------------------------------------------------------------------------
 * File        : IDashboardService.cs
 * Project     : Smart Solar Microgrid Trading System — SE4040 Assignment 1
 * Author      : L K P Yasith (IT 23380196)
 * Created     : 2026-09-19
 * Description : Contract for the summary figures shown on the prosumer's home
 *               screen in the Android application and on the staff home screen
 *               in the React web application.
 *
 * SOLID       : Interface Segregation — reporting is separated from the command
 *               side. IReservationService changes when a booking RULE changes;
 *               this changes when a FIGURE changes. Keeping them apart means
 *               neither reason to change disturbs the other.
 * ---------------------------------------------------------------------------
 */

using SolarMicrogrid.Api.Common;
using SolarMicrogrid.Api.Dtos.Dashboard;

namespace SolarMicrogrid.Api.Services;

/// <summary>
/// Produces the dashboard summaries for both clients.
/// </summary>
public interface IDashboardService
{
    /// <summary>
    /// Builds the summary for one prosumer. A prosumer may only request their
    /// own; staff may request any.
    /// </summary>
    Task<ServiceResult<ProsumerDashboardDto>> GetProsumerDashboardAsync(
        string nic,
        CancellationToken cancellationToken = default);

    /// <summary>Builds the summary shown to Grid Operators and Backoffice officers.</summary>
    Task<ServiceResult<OperatorDashboardDto>> GetOperatorDashboardAsync(
        CancellationToken cancellationToken = default);
}
