/*
 * ---------------------------------------------------------------------------
 * File        : ISlotService.cs
 * Project     : Smart Solar Microgrid Trading System — SE4040 Assignment 1
 * Author      : <Your Full Name> (<IT Number>)
 * Created     : 2026-09-19
 * Description : Contract for managing bookable energy trading windows at a
 *               microgrid node.
 *
 * Business    : BR-9  A window cannot be overbooked. Capacity may never be
 *                     reduced below the bookings already taken.
 * ---------------------------------------------------------------------------
 */

using SolarMicrogrid.Api.Common;
using SolarMicrogrid.Api.Dtos.Slots;

namespace SolarMicrogrid.Api.Services;

/// <summary>
/// Manages energy booking slots.
/// </summary>
public interface ISlotService
{
    /// <summary>
    /// Returns the windows at a node on a date, or across the next seven days
    /// when no date is supplied.
    /// </summary>
    /// <param name="stationId">Identifier of the node.</param>
    /// <param name="slotDate">Optional UTC date to list.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<ServiceResult<IReadOnlyList<SlotResponseDto>>> GetForStationAsync(
        string stationId,
        DateTime? slotDate,
        CancellationToken cancellationToken = default);

    /// <summary>Returns one window by its document identifier.</summary>
    Task<ServiceResult<SlotResponseDto>> GetByIdAsync(
        string id,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates windows for a node by dividing each open day in its weekly
    /// schedule into windows of the requested length. Days that already have
    /// windows are skipped, so the operation is safe to repeat.
    /// </summary>
    Task<ServiceResult<IReadOnlyList<SlotResponseDto>>> GenerateAsync(
        string stationId,
        GenerateSlotsRequestDto request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Opens or closes a window, and optionally changes its capacity.
    /// BR-9: capacity cannot be set below the bookings already taken.
    /// </summary>
    Task<ServiceResult<SlotResponseDto>> UpdateAvailabilityAsync(
        string id,
        UpdateSlotAvailabilityRequestDto request,
        CancellationToken cancellationToken = default);
}
