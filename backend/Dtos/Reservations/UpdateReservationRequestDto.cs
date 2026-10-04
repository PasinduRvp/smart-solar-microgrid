/*
 * ---------------------------------------------------------------------------
 * File        : UpdateReservationRequestDto.cs
 * Project     : Smart Solar Microgrid Trading System — SE4040 Assignment 1
 * Author      : <Your Full Name> (<IT Number>)
 * Created     : 2026-09-19
 * Description : A request to change an existing booking: move it to a different
 *               window, or change the amount or direction of energy.
 *
 * Business    : BR-2 applies. The change is refused unless it is made at least
 *               12 hours before the booking's scheduled time, and the rule is
 *               checked against the EXISTING booking time, so a caller cannot
 *               escape it by moving the booking further away.
 * Security    : Status is not accepted here. A prosumer cannot approve their
 *               own booking or mark it completed by sending an update.
 * ---------------------------------------------------------------------------
 */

using System.ComponentModel.DataAnnotations;

namespace SolarMicrogrid.Api.Dtos.Reservations;

/// <summary>
/// Changes to an existing booking.
/// </summary>
public sealed class UpdateReservationRequestDto
{
    /// <summary>
    /// Identifier of the window to move to. Leave null to keep the current one.
    /// </summary>
    public string? SlotId { get; init; }

    /// <summary>Energy to be traded, in kilowatt hours.</summary>
    [Range(0.1, 1000, ErrorMessage = "Energy must be between 0.1 and 1000 kWh.")]
    public double EnergyKwh { get; init; }

    /// <summary>Whether the prosumer is delivering energy or drawing it.</summary>
    [Required(AllowEmptyStrings = false, ErrorMessage = "Direction is required.")]
    [RegularExpression("^(Deliver|Draw)$", ErrorMessage = "Direction must be either Deliver or Draw.")]
    public string Direction { get; init; } = string.Empty;
}
