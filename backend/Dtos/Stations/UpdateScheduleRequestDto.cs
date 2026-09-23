/*
 * ---------------------------------------------------------------------------
 * File        : UpdateScheduleRequestDto.cs
 * Project     : Smart Solar Microgrid Trading System — SE4040 Assignment 1
 * Author      : <Your Full Name> (<IT Number>)
 * Created     : 2026-09-19
 * Description : Replacement weekly operating hours for a microgrid node. The
 *               assignment lists updating node schedules as a Backoffice duty.
 *
 * Design note : The whole schedule is replaced rather than patched day by day.
 *               That keeps the operation idempotent and avoids the awkward
 *               question of how a caller would delete a single day's hours.
 * ---------------------------------------------------------------------------
 */

using System.ComponentModel.DataAnnotations;

namespace SolarMicrogrid.Api.Dtos.Stations;

/// <summary>
/// A complete replacement weekly schedule.
/// </summary>
public sealed class UpdateScheduleRequestDto
{
    /// <summary>
    /// Operating hours, at most one entry per day of the week. An empty list
    /// closes the node on every day.
    /// </summary>
    [Required(ErrorMessage = "Schedule is required.")]
    [MaxLength(7, ErrorMessage = "A schedule cannot have more than seven entries, one per day.")]
    public IList<ScheduleEntryDto> Schedule { get; init; } = new List<ScheduleEntryDto>();
}
