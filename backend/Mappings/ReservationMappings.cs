/*
 * ---------------------------------------------------------------------------
 * File        : ReservationMappings.cs
 * Project     : Smart Solar Microgrid Trading System — SE4040 Assignment 1
 * Author      : <Your Full Name> (<IT Number>)
 * Created     : 2026-09-19
 * Description : Projects reservation entities onto their response DTO, filling
 *               in the node and prosumer names and the rule driven flags.
 *
 * Why flags   : CanBeModified and CanBeCancelled are worked out here from
 *               BookingRules, the same helper the service uses to accept or
 *               refuse the operation. The mobile application therefore greys
 *               out its Change and Cancel buttons using the SAME rule that
 *               will be applied if the button is pressed, instead of
 *               re-implementing the 12 hour calculation in Java where the two
 *               could drift apart.
 * Security    : QrToken is never copied into this DTO. It is returned only by
 *               the endpoint the owning prosumer calls to draw their QR code.
 * ---------------------------------------------------------------------------
 */

using SolarMicrogrid.Api.Common;
using SolarMicrogrid.Api.Dtos.Reservations;
using SolarMicrogrid.Api.Models;
using SolarMicrogrid.Api.Models.Enums;

namespace SolarMicrogrid.Api.Mappings;

/// <summary>
/// Projections from <see cref="EnergyReservation"/> onto its DTO.
/// </summary>
public static class ReservationMappings
{
    /// <summary>
    /// Builds the response DTO for a booking.
    /// </summary>
    /// <param name="reservation">The booking.</param>
    /// <param name="station">Its node, when known, for the code and name.</param>
    /// <param name="prosumerName">Name of the owning prosumer, when known.</param>
    /// <param name="nowUtc">The instant the rule flags are evaluated against.</param>
    public static ReservationResponseDto ToResponseDto(
        this EnergyReservation reservation,
        SolarStationInfo? station,
        string prosumerName,
        DateTime nowUtc)
    {
        ArgumentNullException.ThrowIfNull(reservation);

        // Only a booking that is still awaiting approval or already approved is
        // a candidate for change. A Completed or Cancelled booking is finished,
        // whatever the clock says.
        bool isLive = reservation.Status is ReservationStatus.Pending or ReservationStatus.Approved;
        bool outsideNotice = BookingRules.IsOutsideNoticePeriod(reservation.ReservationDateTime, nowUtc);

        return new ReservationResponseDto(
            Id: reservation.Id ?? string.Empty,
            ReservationNo: reservation.ReservationNo,
            ProsumerNic: reservation.ProsumerNic,
            ProsumerName: prosumerName,
            StationId: reservation.StationId,
            StationCode: station?.StationCode ?? string.Empty,
            StationName: station?.Name ?? string.Empty,
            SlotId: reservation.SlotId,
            ReservationDateTime: reservation.ReservationDateTime,
            EnergyKwh: reservation.EnergyKwh,
            Direction: reservation.Direction.ToString(),
            Status: reservation.Status.ToString(),
            HasQrCode: !string.IsNullOrWhiteSpace(reservation.QrToken),

            // BR-2 and BR-3 expressed as flags for the client's buttons.
            CanBeModified: isLive && outsideNotice,
            CanBeCancelled: isLive && outsideNotice,

            HoursUntilReservation: BookingRules.HoursUntil(reservation.ReservationDateTime, nowUtc),
            CancelReason: reservation.CancelReason,
            CompletedAt: reservation.CompletedAt,
            CreatedAt: reservation.CreatedAt);
    }
}
