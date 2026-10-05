/*
 * ---------------------------------------------------------------------------
 * File        : UpdateReservationRequest.java
 * Project     : Smart Solar Microgrid Trading System - SE4040 Assignment 1
 * Author      : L K P Yasith (IT 23380196)
 * Created     : 2026-09-20
 * Description : The body sent to PUT /api/reservations/{id}.
 *
 * Mirrors      UpdateReservationRequestDto in the Web API.
 *
 * slotId may be null
 *              Null means keep the window that is already booked, and change
 *              only the energy or the direction. Sending a slot id moves the
 *              booking to that window.
 *
 * Business rule BR-2
 *              A booking may only be changed while it is more than 12 hours
 *              away. The server checks that and refuses with its own message.
 *              The app hides the button early, so the user is not led into a
 *              change that will be refused.
 * ---------------------------------------------------------------------------
 */
package lk.sliit.solarmicrogrid.data.remote.dto;

import androidx.annotation.Nullable;

public final class UpdateReservationRequest {

    @Nullable
    private final String slotId;
    private final double energyKwh;
    private final String direction;

    public UpdateReservationRequest(@Nullable String slotId, double energyKwh, String direction) {
        this.slotId = slotId;
        this.energyKwh = energyKwh;
        this.direction = direction;
    }

    @Nullable
    public String getSlotId() {
        return slotId;
    }

    public double getEnergyKwh() {
        return energyKwh;
    }

    public String getDirection() {
        return direction;
    }
}
