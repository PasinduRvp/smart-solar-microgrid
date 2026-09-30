/*
 * ---------------------------------------------------------------------------
 * File        : CancelReservationRequestDto.cs
 * Project     : Smart Solar Microgrid Trading System — SE4040 Assignment 1
 * Author      : <Your Full Name> (<IT Number>)
 * Created     : 2026-09-19
 * Description : A request to cancel a booking, with the reason recorded against
 *               it for the audit trail.
 *
 * Business    : BR-3 applies. Cancellation is refused unless it is made at
 *               least 12 hours before the booking's scheduled time.
 * ---------------------------------------------------------------------------
 */

using System.ComponentModel.DataAnnotations;

namespace SolarMicrogrid.Api.Dtos.Reservations;

/// <summary>
/// Reason for cancelling a booking.
/// </summary>
public sealed class CancelReservationRequestDto
{
    /// <summary>Why the booking is being cancelled. Kept for the audit trail.</summary>
    [Required(AllowEmptyStrings = false, ErrorMessage = "A cancellation reason is required.")]
    [MaxLength(200)]
    public string Reason { get; init; } = string.Empty;
}
