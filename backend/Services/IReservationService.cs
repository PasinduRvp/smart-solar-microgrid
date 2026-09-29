/*
 * ---------------------------------------------------------------------------
 * File        : IReservationService.cs
 * Project     : Smart Solar Microgrid Trading System — SE4040 Assignment 1
 * Author      : <Your Full Name> (<IT Number>)
 * Created     : 2026-09-19
 * Description : Contract for the reservation lifecycle, from a prosumer's
 *               request through approval, QR verification and completion.
 *
 * Business    : BR-1  A booking must fall within 7 days.
 *               BR-2  A change needs at least 12 hours' notice.
 *               BR-3  A cancellation needs at least 12 hours' notice.
 *               BR-8  A QR token is issued only on approval and is verified by
 *                     the server, never by the operator's device.
 *               BR-9  A window cannot be overbooked.
 * ---------------------------------------------------------------------------
 */

using SolarMicrogrid.Api.Common;
using SolarMicrogrid.Api.Dtos.Reservations;
using SolarMicrogrid.Api.Models.Enums;

namespace SolarMicrogrid.Api.Services;

/// <summary>
/// Manages the energy slot reservation lifecycle.
/// </summary>
public interface IReservationService
{
    /// <summary>
    /// Searches reservations. A prosumer is limited to their own bookings
    /// regardless of the filters supplied.
    /// </summary>
    Task<ServiceResult<IReadOnlyList<ReservationResponseDto>>> SearchAsync(
        string? prosumerNic,
        ReservationStatus? status,
        DateTime? fromUtc,
        DateTime? toUtc,
        string? stationId,
        CancellationToken cancellationToken = default);

    /// <summary>Returns the bookings awaiting approval, oldest first.</summary>
    Task<ServiceResult<IReadOnlyList<ReservationResponseDto>>> GetPendingAsync(
        CancellationToken cancellationToken = default);

    /// <summary>Returns one booking by its identifier.</summary>
    Task<ServiceResult<ReservationResponseDto>> GetByIdAsync(
        string id,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a booking. Applies BR-1 and BR-9.
    /// </summary>
    Task<ServiceResult<ReservationResponseDto>> CreateAsync(
        CreateReservationRequestDto request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Changes a booking. Applies BR-2, and BR-1 and BR-9 to the new window.
    /// </summary>
    Task<ServiceResult<ReservationResponseDto>> UpdateAsync(
        string id,
        UpdateReservationRequestDto request,
        CancellationToken cancellationToken = default);

    /// <summary>Cancels a booking. Applies BR-3.</summary>
    Task<ServiceResult<ReservationResponseDto>> CancelAsync(
        string id,
        CancelReservationRequestDto request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Approves a booking and issues its QR token. Applies BR-8.
    /// </summary>
    Task<ServiceResult<ReservationResponseDto>> ApproveAsync(
        string id,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the QR payload for an approved booking. Only the owning
    /// prosumer may call this.
    /// </summary>
    Task<ServiceResult<QrTokenResponseDto>> GetQrTokenAsync(
        string id,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Verifies a scanned QR token against the stored booking and returns the
    /// booking when it is genuine. This is the server side half of BR-8.
    /// </summary>
    Task<ServiceResult<ReservationResponseDto>> VerifyQrAsync(
        VerifyQrRequestDto request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Finalises the energy transfer after a successful scan, marking the
    /// booking Completed and retiring its QR token.
    /// </summary>
    Task<ServiceResult<ReservationResponseDto>> CompleteAsync(
        string id,
        CancellationToken cancellationToken = default);
}
