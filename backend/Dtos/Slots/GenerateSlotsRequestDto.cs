/*
 * ---------------------------------------------------------------------------
 * File        : GenerateSlotsRequestDto.cs
 * Project     : Smart Solar Microgrid Trading System — SE4040 Assignment 1
 * Author      : <Your Full Name> (<IT Number>)
 * Created     : 2026-09-19
 * Description : Instructions for generating bookable windows at a node. The
 *               generator reads the node's weekly schedule and divides each
 *               open day into windows of the requested length.
 *
 * Business    : NumberOfDays is capped at 7 because the assignment only allows
 *               a reservation to be made within seven days. Generating windows
 *               further ahead would produce slots that BR-1 could never accept.
 * ---------------------------------------------------------------------------
 */

using System.ComponentModel.DataAnnotations;

namespace SolarMicrogrid.Api.Dtos.Slots;

/// <summary>
/// Parameters for generating bookable windows.
/// </summary>
public sealed class GenerateSlotsRequestDto
{
    /// <summary>First date to generate windows for, in UTC. Defaults to today when omitted.</summary>
    public DateTime? StartDate { get; init; }

    /// <summary>
    /// How many consecutive days to generate, from 1 to 7. The upper bound
    /// matches the seven day booking window the assignment specifies.
    /// </summary>
    [Range(1, 7, ErrorMessage = "Number of days must be between 1 and 7, matching the 7 day booking window.")]
    public int NumberOfDays { get; init; } = 7;

    /// <summary>Length of each window in minutes.</summary>
    [Range(15, 480, ErrorMessage = "Slot duration must be between 15 and 480 minutes.")]
    public int SlotDurationMinutes { get; init; } = 60;

    /// <summary>How many bookings each window may hold.</summary>
    [Range(1, 100, ErrorMessage = "Capacity per slot must be between 1 and 100.")]
    public int CapacityPerSlot { get; init; } = 1;

    /// <summary>Trading price per kilowatt hour for the generated windows.</summary>
    [Range(0, 10000, ErrorMessage = "Energy rate must be between 0 and 10000.")]
    public double EnergyRatePerKwh { get; init; }
}
