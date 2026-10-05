/*
 * ---------------------------------------------------------------------------
 * File        : Reservation.java
 * Project     : Smart Solar Microgrid Trading System - SE4040 Assignment 1
 * Author      : L K P Yasith (IT 23380196)
 * Created     : 2026-09-20
 * Description : One energy booking, as the screens use it.
 *
 * The rules come from the server
 *              canBeModified is BR-2. canBeCancelled is BR-3. Both mean the
 *              booking is more than 12 hours away and not already finished.
 *
 *              The app reads these. It does not work them out. So the phone
 *              and the web application can never disagree about whether a
 *              booking may still be changed.
 *
 * Immutable    Every field is final.
 *
 * Status       Pending   - booked, waiting for a Grid Operator to approve
 *              Approved  - approved, and a QR code can be shown
 *              Completed - the energy transfer is done
 *              Cancelled - called off by the prosumer or by staff
 * ---------------------------------------------------------------------------
 */
package lk.sliit.solarmicrogrid.model;

import androidx.annotation.NonNull;
import androidx.annotation.Nullable;

import lk.sliit.solarmicrogrid.data.remote.dto.ReservationDto;

public final class Reservation {

    public static final String STATUS_PENDING = "Pending";
    public static final String STATUS_APPROVED = "Approved";
    public static final String STATUS_COMPLETED = "Completed";
    public static final String STATUS_CANCELLED = "Cancelled";

    /** Sending energy to the grid. */
    public static final String DIRECTION_DELIVER = "Deliver";

    /** Taking energy from the grid. */
    public static final String DIRECTION_DRAW = "Draw";

    private final String id;
    private final String reservationNo;
    private final String prosumerNic;
    private final String prosumerName;
    private final String stationId;
    private final String stationName;
    private final String slotId;
    private final String reservationDateTime;
    private final double energyKwh;
    private final String direction;
    private final String status;
    private final boolean hasQrCode;
    private final boolean canBeModified;
    private final boolean canBeCancelled;
    private final double hoursUntilReservation;
    private final String cancelReason;

    public Reservation(@NonNull String id,
                       @NonNull String reservationNo,
                       @NonNull String prosumerNic,
                       @NonNull String prosumerName,
                       @NonNull String stationId,
                       @NonNull String stationName,
                       @NonNull String slotId,
                       @NonNull String reservationDateTime,
                       double energyKwh,
                       @NonNull String direction,
                       @NonNull String status,
                       boolean hasQrCode,
                       boolean canBeModified,
                       boolean canBeCancelled,
                       double hoursUntilReservation,
                       @NonNull String cancelReason) {
        this.id = id;
        this.reservationNo = reservationNo;
        this.prosumerNic = prosumerNic;
        this.prosumerName = prosumerName;
        this.stationId = stationId;
        this.stationName = stationName;
        this.slotId = slotId;
        this.reservationDateTime = reservationDateTime;
        this.energyKwh = energyKwh;
        this.direction = direction;
        this.status = status;
        this.hasQrCode = hasQrCode;
        this.canBeModified = canBeModified;
        this.canBeCancelled = canBeCancelled;
        this.hoursUntilReservation = hoursUntilReservation;
        this.cancelReason = cancelReason;
    }

    @NonNull
    public static Reservation from(@NonNull ReservationDto dto) {
        return new Reservation(
                orEmpty(dto.getId()),
                orEmpty(dto.getReservationNo()),
                orEmpty(dto.getProsumerNic()),
                orEmpty(dto.getProsumerName()),
                orEmpty(dto.getStationId()),
                orEmpty(dto.getStationName()),
                orEmpty(dto.getSlotId()),
                orEmpty(dto.getReservationDateTime()),
                dto.getEnergyKwh(),
                orEmpty(dto.getDirection()),
                orEmpty(dto.getStatus()),
                dto.isHasQrCode(),
                dto.isCanBeModified(),
                dto.isCanBeCancelled(),
                dto.getHoursUntilReservation(),
                orEmpty(dto.getCancelReason()));
    }

    public String getId() {
        return id;
    }

    /** The number shown to the user, for example RES-000123. */
    public String getReservationNo() {
        return reservationNo;
    }

    /** Who the booking belongs to. The operator sees this after a scan. */
    public String getProsumerNic() {
        return prosumerNic;
    }

    public String getProsumerName() {
        return prosumerName;
    }

    public String getStationId() {
        return stationId;
    }

    public String getStationName() {
        return stationName;
    }

    public String getSlotId() {
        return slotId;
    }

    /** When the booking is, as the API sent it. UTC. */
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

    public boolean hasQrCode() {
        return hasQrCode;
    }

    /** Business rule BR-2. Decided by the server. */
    public boolean canBeModified() {
        return canBeModified;
    }

    /** Business rule BR-3. Decided by the server. */
    public boolean canBeCancelled() {
        return canBeCancelled;
    }

    public double getHoursUntilReservation() {
        return hoursUntilReservation;
    }

    public String getCancelReason() {
        return cancelReason;
    }

    public boolean isPending() {
        return STATUS_PENDING.equals(status);
    }

    public boolean isApproved() {
        return STATUS_APPROVED.equals(status);
    }

    public boolean isCancelled() {
        return STATUS_CANCELLED.equals(status);
    }

    public boolean isCompleted() {
        return STATUS_COMPLETED.equals(status);
    }

    /** True when the booking is still ahead and not called off. */
    public boolean isUpcoming() {
        return !isCancelled() && !isCompleted();
    }

    public boolean isDeliver() {
        return DIRECTION_DELIVER.equals(direction);
    }

    @NonNull
    private static String orEmpty(@Nullable String value) {
        return value == null ? "" : value;
    }
}
