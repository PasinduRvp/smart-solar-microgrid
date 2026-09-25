/*
 * ---------------------------------------------------------------------------
 * File        : SlotMappings.cs
 * Project     : Smart Solar Microgrid Trading System — SE4040 Assignment 1
 * Author      : <Your Full Name> (<IT Number>)
 * Created     : 2026-09-19
 * Description : Converts booking window entities into their DTOs, working out
 *               the derived fields the clients display.
 *
 * Why derived : RemainingCapacity and IsBookable are computed in the API rather
 *               than in each client. If the Android app worked out "is this
 *               bookable" for itself it would be duplicating a rule that the
 *               service owns, and the two could drift apart. The clients simply
 *               render what they are told, which is what the FAT service
 *               pattern requires.
 * ---------------------------------------------------------------------------
 */

using SolarMicrogrid.Api.Common;
using SolarMicrogrid.Api.Dtos.Slots;
using SolarMicrogrid.Api.Models;

namespace SolarMicrogrid.Api.Mappings;

/// <summary>
/// Projections from <see cref="EnergyBookingSlot"/> onto its DTO.
/// </summary>
public static class SlotMappings
{
    /// <summary>
    /// Builds the response DTO for a booking window, including whether it can
    /// currently be booked.
    /// </summary>
    public static SlotResponseDto ToResponseDto(this EnergyBookingSlot slot)
    {
        ArgumentNullException.ThrowIfNull(slot);

        // Clamped at zero so a data problem can never show a negative number of
        // remaining places on a client screen.
        int remaining = Math.Max(0, slot.TotalCapacitySlots - slot.BookedCount);

        // A window can be booked only when all three hold: the operator has it
        // open, it still has room, and it has not already passed.
        bool isBookable = slot.IsAvailable
            && remaining > 0
            && SlotStartUtc(slot) > DateTime.UtcNow;

        return new SlotResponseDto(
            Id: slot.Id ?? string.Empty,
            StationId: slot.StationId,
            SlotDate: slot.SlotDate,
            StartTime: slot.StartTime,
            EndTime: slot.EndTime,
            TotalCapacitySlots: slot.TotalCapacitySlots,
            BookedCount: slot.BookedCount,
            RemainingCapacity: remaining,
            EnergyRatePerKwh: slot.EnergyRatePerKwh,
            IsAvailable: slot.IsAvailable,
            IsBookable: isBookable);
    }

    /// <summary>Builds response DTOs for a collection of windows.</summary>
    public static IReadOnlyList<SlotResponseDto> ToResponseDtos(this IEnumerable<EnergyBookingSlot> slots)
    {
        ArgumentNullException.ThrowIfNull(slots);
        return slots.Select(slot => slot.ToResponseDto()).ToList();
    }

    /// <summary>
    /// Combines the window's date and start time into a single UTC instant.
    /// This is also the value a reservation is measured against by the 7 day
    /// and 12 hour rules, so it is defined here once.
    /// </summary>
    /// <remarks>
    /// StartTime is a local wall clock time at the node, such as "06:00". It
    /// is converted to UTC, not relabelled as UTC.
    ///
    /// This used to call DateTime.SpecifyKind, which only changes the label.
    /// A window opening at 06:00 was then stored as 06:00 UTC, and every
    /// client showing local time displayed it as 11:30. The booking picker
    /// and the booking list disagreed by the whole 5 hour 30 minute offset.
    /// </remarks>
    public static DateTime SlotStartUtc(this EnergyBookingSlot slot)
    {
        ArgumentNullException.ThrowIfNull(slot);

        // A malformed time would otherwise throw deep inside a query, so an
        // unreadable one falls back to midnight. That makes the window look
        // already past rather than bookable, which is the safe direction.
        TimeOnly startTime = GridTime.ParseTimeOrMidnight(slot.StartTime);

        return GridTime.ToUtc(slot.SlotDate, startTime);
    }
}
