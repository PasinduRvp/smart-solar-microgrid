/*
 * ---------------------------------------------------------------------------
 * File        : ScheduleEntryDto.cs
 * Project     : Smart Solar Microgrid Trading System — SE4040 Assignment 1
 * Author      : <Your Full Name> (<IT Number>)
 * Created     : 2026-09-19
 * Description : Operating hours for one day of the week at a microgrid node.
 *               Maintained by Backoffice officers and used when generating
 *               bookable slots.
 *
 * Validation  : Times are held as "HH:mm" strings and checked with a regular
 *               expression, so "25:00" or "9am" never reaches the generator.
 * ---------------------------------------------------------------------------
 */

using System.ComponentModel.DataAnnotations;

namespace SolarMicrogrid.Api.Dtos.Stations;

/// <summary>
/// Opening hours for a single day of the week.
/// </summary>
public sealed class ScheduleEntryDto
{
    /// <summary>Day these hours apply to, for example "Monday".</summary>
    [Required(AllowEmptyStrings = false, ErrorMessage = "Day of week is required.")]
    [RegularExpression("^(Monday|Tuesday|Wednesday|Thursday|Friday|Saturday|Sunday)$",
        ErrorMessage = "Day of week must be a full English day name, e.g. Monday.")]
    public string DayOfWeek { get; init; } = string.Empty;

    /// <summary>Opening time in 24 hour "HH:mm" format.</summary>
    [Required(AllowEmptyStrings = false, ErrorMessage = "Open time is required.")]
    [RegularExpression(@"^([01]\d|2[0-3]):[0-5]\d$", ErrorMessage = "Open time must be in HH:mm format.")]
    public string OpenTime { get; init; } = string.Empty;

    /// <summary>Closing time in 24 hour "HH:mm" format.</summary>
    [Required(AllowEmptyStrings = false, ErrorMessage = "Close time is required.")]
    [RegularExpression(@"^([01]\d|2[0-3]):[0-5]\d$", ErrorMessage = "Close time must be in HH:mm format.")]
    public string CloseTime { get; init; } = string.Empty;
}
