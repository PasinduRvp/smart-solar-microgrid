/*
 * ---------------------------------------------------------------------------
 * File        : QrTokenResponseDto.cs
 * Project     : Smart Solar Microgrid Trading System — SE4040 Assignment 1
 * Author      : L K P Yasith (IT 23380196)
 * Created     : 2026-09-19
 * Description : The token a prosumer's application encodes into a QR image.
 *               Returned only to the prosumer who owns the booking.
 *
 * Design note : The API returns the token as text and the Android application
 *               renders the QR bitmap itself with ZXing. Returning a rendered
 *               image instead would move a presentation concern into the
 *               service and send far more data over a mobile connection.
 * ---------------------------------------------------------------------------
 */

namespace SolarMicrogrid.Api.Dtos.Reservations;

/// <summary>
/// The QR payload for an approved booking.
/// </summary>
/// <param name="ReservationNo">Readable booking reference, shown under the code.</param>
/// <param name="QrToken">The exact string to encode into the QR image.</param>
/// <param name="IssuedAtUtc">When the token was issued.</param>
/// <param name="ReservationDateTime">When the transfer is scheduled.</param>
/// <param name="StationName">Node the prosumer should go to.</param>
public sealed record QrTokenResponseDto(
    string ReservationNo,
    string QrToken,
    DateTime IssuedAtUtc,
    DateTime ReservationDateTime,
    string StationName);
