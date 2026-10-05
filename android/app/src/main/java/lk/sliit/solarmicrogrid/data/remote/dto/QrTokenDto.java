/*
 * ---------------------------------------------------------------------------
 * File        : QrTokenDto.java
 * Project     : Smart Solar Microgrid Trading System - SE4040 Assignment 1
 * Author      : L K P Yasith (IT 23380196)
 * Created     : 2026-09-20
 * Description : The transaction QR code for an approved booking. Task E3.
 *
 * Mirrors      QrTokenResponseDto in the Web API.
 *
 * The phone does not invent the token
 *              qrToken is made and stored by the server when a booking is
 *              approved. The app only draws it as a square of black and
 *              white. It cannot make a token of its own, so a prosumer
 *              cannot produce a code for a booking that was never approved.
 *
 * The scanner checks with the server
 *              A Grid Operator scanning this code sends the token back to
 *              the API. The API says whose booking it is and whether it is
 *              still valid. So a photo of somebody else code is useless.
 * ---------------------------------------------------------------------------
 */
package lk.sliit.solarmicrogrid.data.remote.dto;

public final class QrTokenDto {

    private String reservationNo;
    private String qrToken;
    private String issuedAtUtc;
    private String reservationDateTime;
    private String stationName;

    public String getReservationNo() {
        return reservationNo;
    }

    /** The text drawn inside the QR square. */
    public String getQrToken() {
        return qrToken;
    }

    public String getIssuedAtUtc() {
        return issuedAtUtc;
    }

    public String getReservationDateTime() {
        return reservationDateTime;
    }

    public String getStationName() {
        return stationName;
    }
}
