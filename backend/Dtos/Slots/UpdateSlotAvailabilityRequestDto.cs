/*
 * ---------------------------------------------------------------------------
 * File        : UpdateSlotAvailabilityRequestDto.cs
 * Project     : Smart Solar Microgrid Trading System — SE4040 Assignment 1
 * Author      : <Your Full Name> (<IT Number>)
 * Created     : 2026-09-19
 * Description : Used by a grid operator to open or close a single window, which
 *               the assignment describes as updating battery slot availability.
 *
 * Security    : BookedCount is absent by design. It is maintained by the
 *               reservation service as bookings are accepted and cancelled, and
 *               letting an operator set it directly would allow the overbooking
 *               check in BR-9 to be bypassed.
 * ---------------------------------------------------------------------------
 */

using System.ComponentModel.DataAnnotations;

namespace SolarMicrogrid.Api.Dtos.Slots;

/// <summary>
/// New availability for a single booking window.
/// </summary>
public sealed class UpdateSlotAvailabilityRequestDto
{
    /// <summary>True to open the window for booking, false to close it.</summary>
    [Required(ErrorMessage = "Availability is required.")]
    public bool IsAvailable { get; init; }

    /// <summary>
    /// Optional new capacity for the window. When supplied it may not be set
    /// below the number of bookings already taken.
    /// </summary>
    [Range(1, 100, ErrorMessage = "Capacity per slot must be between 1 and 100.")]
    public int? TotalCapacitySlots { get; init; }
}
