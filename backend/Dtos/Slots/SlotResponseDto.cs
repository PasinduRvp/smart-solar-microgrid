/*
 * ---------------------------------------------------------------------------
 * File        : SlotResponseDto.cs
 * Project     : Smart Solar Microgrid Trading System — SE4040 Assignment 1
 * Author      : <Your Full Name> (<IT Number>)
 * Created     : 2026-09-19
 * Description : A bookable energy trading window as shown to the clients when
 *               a prosumer picks a time.
 *
 * Design note : RemainingCapacity and IsBookable are computed by the API rather
 *               than left for each client to work out. The Android and web
 *               applications therefore cannot disagree about whether a slot can
 *               still be booked, and neither of them contains the rule.
 * ---------------------------------------------------------------------------
 */

namespace SolarMicrogrid.Api.Dtos.Slots;

/// <summary>
/// A bookable energy trading time window.
/// </summary>
/// <param name="Id">Document identifier.</param>
/// <param name="StationId">Identifier of the owning node.</param>
/// <param name="SlotDate">Calendar date of the window, in UTC.</param>
/// <param name="StartTime">Start of the window, "HH:mm".</param>
/// <param name="EndTime">End of the window, "HH:mm".</param>
/// <param name="TotalCapacitySlots">How many bookings the window can hold.</param>
/// <param name="BookedCount">How many bookings it already holds.</param>
/// <param name="RemainingCapacity">Bookings still available, never below zero.</param>
/// <param name="EnergyRatePerKwh">Trading price per kilowatt hour.</param>
/// <param name="IsAvailable">False when an operator has closed the window.</param>
/// <param name="IsBookable">True only when the window is open, has capacity left and is still in the future.</param>
public sealed record SlotResponseDto(
    string Id,
    string StationId,
    DateTime SlotDate,
    string StartTime,
    string EndTime,
    int TotalCapacitySlots,
    int BookedCount,
    int RemainingCapacity,
    double EnergyRatePerKwh,
    bool IsAvailable,
    bool IsBookable);
