/*
 * ---------------------------------------------------------------------------
 * File        : ReservationResponseDto.cs
 * Project     : Smart Solar Microgrid Trading System — SE4040 Assignment 1
 * Author      : L K P Yasith (IT 23380196)
 * Created     : 2026-09-19
 * Description : A reservation as returned to the clients. This is also what the
 *               mobile application shows on the summary screen after a booking
 *               is created, changed or cancelled.
 *
 * Security    : QrToken is NOT part of this DTO. The token is returned only by
 *               the dedicated endpoint the owning prosumer calls to render
 *               their QR code, so it never appears in a list that staff or
 *               another screen might display or log.
 * Design note : CanBeModified and CanBeCancelled are decided by the API, not by
 *               the client. The mobile application greys out its buttons using
 *               these flags rather than working out the 12 hour rule for
 *               itself, which keeps that rule in the service where it belongs.
 * ---------------------------------------------------------------------------
 */

namespace SolarMicrogrid.Api.Dtos.Reservations;

/// <summary>
/// A booked energy trading slot.
/// </summary>
/// <param name="Id">Document identifier.</param>
/// <param name="ReservationNo">Readable booking reference.</param>
/// <param name="ProsumerNic">NIC of the prosumer who owns the booking.</param>
/// <param name="ProsumerName">Name of the prosumer, for staff screens.</param>
/// <param name="StationId">Identifier of the node.</param>
/// <param name="StationCode">Reference code of the node.</param>
/// <param name="StationName">Display name of the node.</param>
/// <param name="SlotId">Identifier of the booked window.</param>
/// <param name="ReservationDateTime">UTC instant the transfer is scheduled for.</param>
/// <param name="EnergyKwh">Energy to be traded, in kilowatt hours.</param>
/// <param name="Direction">Deliver or Draw.</param>
/// <param name="Status">Pending, Approved, Completed or Cancelled.</param>
/// <param name="HasQrCode">True once a QR token has been issued.</param>
/// <param name="CanBeModified">True when BR-2 currently permits a change.</param>
/// <param name="CanBeCancelled">True when BR-3 currently permits cancellation.</param>
/// <param name="HoursUntilReservation">Hours remaining until the booking, negative once past.</param>
/// <param name="CancelReason">Reason recorded when cancelled.</param>
/// <param name="CompletedAt">UTC time the transfer was finalised.</param>
/// <param name="CreatedAt">UTC time the booking was made.</param>
public sealed record ReservationResponseDto(
    string Id,
    string ReservationNo,
    string ProsumerNic,
    string ProsumerName,
    string StationId,
    string StationCode,
    string StationName,
    string SlotId,
    DateTime ReservationDateTime,
    double EnergyKwh,
    string Direction,
    string Status,
    bool HasQrCode,
    bool CanBeModified,
    bool CanBeCancelled,
    double HoursUntilReservation,
    string? CancelReason,
    DateTime? CompletedAt,
    DateTime CreatedAt);
