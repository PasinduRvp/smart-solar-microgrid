/*
 * ---------------------------------------------------------------------------
 * File        : ReservationDto.java
 * Project     : Smart Solar Microgrid Trading System - SE4040 Assignment 1
 * Author      : L K P Yasith (IT 23380196)
 * Created     : 2026-09-20
 * Description : One energy booking, as the Web API returns it.
 *
 * Mirrors      ReservationResponseDto in the Web API.
 *
 * The server sends the rules, not just the data
 *              canBeModified is business rule BR-2. canBeCancelled is BR-3.
 *              Both mean the same thing: the booking is more than 12 hours
 *              away and is not already finished.
 *
 *              The app does not work these out. It reads them. So the phone
 *              and the web application can never disagree about whether a
 *              booking may still be changed.
 *
 *              The buttons only follow these flags. The server checks again
 *              anyway, so a stale screen cannot break the rule.
 * ---------------------------------------------------------------------------
 */
package lk.sliit.solarmicrogrid.data.remote.dto;

public final class ReservationDto {

    private String id;
    private String reservationNo;
    private String prosumerNic;
    private String prosumerName;
    private String stationId;
    private String stationCode;
    private String stationName;
    private String slotId;
    private String reservationDateTime;
    private double energyKwh;
    private String direction;
    private String status;
    private boolean hasQrCode;
    private boolean canBeModified;
    private boolean canBeCancelled;
    private double hoursUntilReservation;
    private String cancelReason;
    private String completedAt;
    private String createdAt;

    public String getId() {
        return id;
    }

    public String getReservationNo() {
        return reservationNo;
    }

    public String getProsumerNic() {
        return prosumerNic;
    }

    public String getProsumerName() {
        return prosumerName;
    }

    public String getStationId() {
        return stationId;
    }

    public String getStationCode() {
        return stationCode;
    }

    public String getStationName() {
        return stationName;
    }

    public String getSlotId() {
        return slotId;
    }

    public String getReservationDateTime() {
        return reservationDateTime;
    }

    public double getEnergyKwh() {
        return energyKwh;
    }

    public String getDirection() {
        return direction;
    }

    public String getStatus() {
        return status;
    }

    public boolean isHasQrCode() {
        return hasQrCode;
    }

    /** Business rule BR-2. Decided by the server. */
    public boolean isCanBeModified() {
        return canBeModified;
    }

    /** Business rule BR-3. Decided by the server. */
    public boolean isCanBeCancelled() {
        return canBeCancelled;
    }

    public double getHoursUntilReservation() {
        return hoursUntilReservation;
    }

    public String getCancelReason() {
        return cancelReason;
    }

    public String getCompletedAt() {
        return completedAt;
    }

    public String getCreatedAt() {
        return createdAt;
    }
}
