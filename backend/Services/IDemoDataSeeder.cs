/*
 * ---------------------------------------------------------------------------
 * File        : IDemoDataSeeder.cs
 * Project     : Smart Solar Microgrid Trading System — SE4040 Assignment 1
 * Author      : NIMSARA R V P P (IT 23215306)
 * Created     : 2026-09-19
 * Description : Contract for populating the database with a realistic set of
 *               sample records across all four collections.
 *
 * Why this    : Three reasons. The assignment's database criterion asks for
 *               sample data in every collection. A demonstration needs data
 *               that shows each rule working, including a booking too close to
 *               its time to be cancelled. And if the cloud database is
 *               unreachable on the day, a local one can be filled in seconds.
 * ---------------------------------------------------------------------------
 */

using SolarMicrogrid.Api.Common;

namespace SolarMicrogrid.Api.Services;

/// <summary>
/// Populates the database with demonstration data.
/// </summary>
public interface IDemoDataSeeder
{
    /// <summary>
    /// Creates sample staff, prosumers, nodes, booking windows and
    /// reservations. Records that already exist are left untouched, so the
    /// operation is safe to run more than once.
    /// </summary>
    /// <returns>A human readable summary of what was created.</returns>
    Task<ServiceResult<IReadOnlyList<string>>> SeedAsync(CancellationToken cancellationToken = default);
}
