/*
 * ---------------------------------------------------------------------------
 * File        : CancelReservationRequest.java
 * Project     : Smart Solar Microgrid Trading System - SE4040 Assignment 1
 * Author      : L K P Yasith (IT 23380196)
 * Created     : 2026-09-20
 * Description : The body sent to PATCH /api/reservations/{id}/cancel.
 *
 * Mirrors      CancelReservationRequestDto in the Web API.
 *
 * Why a reason is required
 *              The server will not accept a cancellation without one, and it
 *              keeps the text on the booking. A Grid Operator can then see
 *              why a window was given up, instead of only that it was.
 *
 * Business rule BR-3
 *              A booking may only be cancelled while it is more than 12 hours
 *              away.
 * ---------------------------------------------------------------------------
 */
package lk.sliit.solarmicrogrid.data.remote.dto;

public final class CancelReservationRequest {

    private final String reason;

    public CancelReservationRequest(String reason) {
        this.reason = reason;
    }

    public String getReason() {
        return reason;
    }
}
