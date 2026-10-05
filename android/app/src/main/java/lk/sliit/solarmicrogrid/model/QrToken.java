/*
 * ---------------------------------------------------------------------------
 * File        : QrToken.java
 * Project     : Smart Solar Microgrid Trading System - SE4040 Assignment 1
 * Author      : L K P Yasith (IT 23380196)
 * Created     : 2026-09-20
 * Description : The transaction QR code of an approved booking. Task E3.
 *
 * The phone never invents a token
 *              The token is made and stored by the server when a Grid
 *              Operator approves a booking. The app only draws it as a
 *              square of black and white.
 *              So a prosumer cannot produce a code for a booking that was
 *              never approved.
 *
 * A copied code is still useless
 *              The operator scanning it sends the token back to the API. The
 *              API says whose booking it is and whether it can still be
 *              used. A screenshot of somebody else code fails there.
 *
 * Immutable    Every field is final.
 * ---------------------------------------------------------------------------
 */
package lk.sliit.solarmicrogrid.model;

import androidx.annotation.NonNull;
import androidx.annotation.Nullable;

import lk.sliit.solarmicrogrid.data.remote.dto.QrTokenDto;

public final class QrToken {

    private final String reservationNo;
    private final String qrToken;
    private final String reservationDateTime;
    private final String stationName;

    public QrToken(@NonNull String reservationNo,
                   @NonNull String qrToken,
                   @NonNull String reservationDateTime,
                   @NonNull String stationName) {
        this.reservationNo = reservationNo;
        this.qrToken = qrToken;
        this.reservationDateTime = reservationDateTime;
        this.stationName = stationName;
    }

    @NonNull
    public static QrToken from(@NonNull QrTokenDto dto) {
        return new QrToken(
                orEmpty(dto.getReservationNo()),
                orEmpty(dto.getQrToken()),
                orEmpty(dto.getReservationDateTime()),
                orEmpty(dto.getStationName()));
    }

    public String getReservationNo() {
        return reservationNo;
    }

    /** The text drawn inside the QR square. */
    public String getQrToken() {
        return qrToken;
    }

    /** When the booking is, as the API sent it. UTC. */
    public String getReservationDateTime() {
        return reservationDateTime;
    }

    public String getStationName() {
        return stationName;
    }

    @NonNull
    private static String orEmpty(@Nullable String value) {
        return value == null ? "" : value;
    }
}
