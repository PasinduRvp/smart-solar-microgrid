/*
 * ---------------------------------------------------------------------------
 * File        : CreateReservationRequest.java
 * Project     : Smart Solar Microgrid Trading System - SE4040 Assignment 1
 * Author      : L K P Yasith (IT 23380196)
 * Created     : 2026-09-20
 * Description : The body sent to POST /api/reservations.
 *
 * Mirrors      CreateReservationRequestDto in the Web API.
 *
 * prosumerNic is left out on purpose
 *              The field exists so a Backoffice officer can book for someone
 *              else from the web application. A prosumer booking for
 *              themselves does not send it. The server then takes the NIC
 *              from the token.
 *              That is what stops one prosumer booking in another name.
 *
 * Server rules Energy must be between 0.1 and 1000 kWh.
 *              Direction must be Deliver or Draw.
 *              The window must be inside the next 7 days. That is BR-1.
 * ---------------------------------------------------------------------------
 */
package lk.sliit.solarmicrogrid.data.remote.dto;

public final class CreateReservationRequest {

    private final String slotId;
    private final double energyKwh;
    private final String direction;

    public CreateReservationRequest(String slotId, double energyKwh, String direction) {
        this.slotId = slotId;
        this.energyKwh = energyKwh;
        this.direction = direction;
    }

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
