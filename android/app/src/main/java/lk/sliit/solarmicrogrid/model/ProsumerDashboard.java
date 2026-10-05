/*
 * ---------------------------------------------------------------------------
 * File        : ProsumerDashboard.java
 * Project     : Smart Solar Microgrid Trading System - SE4040 Assignment 1
 * Author      : L K P Yasith (IT 23380196)
 * Created     : 2026-09-20
 * Description : The numbers on the prosumer home screen. Task E1.
 *
 * The assignment asks for two counts by name: pending bookings, and
 * approved bookings still to come. Both are here, with the finished and
 * cancelled totals beside them so the screen tells a whole story.
 *
 * Immutable    Every field is final.
 * ---------------------------------------------------------------------------
 */
package lk.sliit.solarmicrogrid.model;

import androidx.annotation.NonNull;
import androidx.annotation.Nullable;

import lk.sliit.solarmicrogrid.data.remote.dto.ProsumerDashboardDto;

public final class ProsumerDashboard {

    private final long pendingCount;
    private final long approvedFutureCount;
    private final long completedCount;
    private final long cancelledCount;
    private final double totalEnergyDeliveredKwh;
    private final double totalEnergyDrawnKwh;

    @Nullable
    private final Reservation nextBooking;

    public ProsumerDashboard(long pendingCount,
                             long approvedFutureCount,
                             long completedCount,
                             long cancelledCount,
                             double totalEnergyDeliveredKwh,
                             double totalEnergyDrawnKwh,
                             @Nullable Reservation nextBooking) {
        this.pendingCount = pendingCount;
        this.approvedFutureCount = approvedFutureCount;
        this.completedCount = completedCount;
        this.cancelledCount = cancelledCount;
        this.totalEnergyDeliveredKwh = totalEnergyDeliveredKwh;
        this.totalEnergyDrawnKwh = totalEnergyDrawnKwh;
        this.nextBooking = nextBooking;
    }

    @NonNull
    public static ProsumerDashboard from(@NonNull ProsumerDashboardDto dto) {
        return new ProsumerDashboard(
                dto.getPendingCount(),
                dto.getApprovedFutureCount(),
                dto.getCompletedCount(),
                dto.getCancelledCount(),
                dto.getTotalEnergyDeliveredKwh(),
                dto.getTotalEnergyDrawnKwh(),
                dto.getNextBooking() == null ? null : Reservation.from(dto.getNextBooking()));
    }

    /** Booked, waiting for a Grid Operator to approve. */
    public long getPendingCount() {
        return pendingCount;
    }

    /** Approved and still to come. */
    public long getApprovedFutureCount() {
        return approvedFutureCount;
    }

    public long getCompletedCount() {
        return completedCount;
    }

    public long getCancelledCount() {
        return cancelledCount;
    }

    public double getTotalEnergyDeliveredKwh() {
        return totalEnergyDeliveredKwh;
    }

    public double getTotalEnergyDrawnKwh() {
        return totalEnergyDrawnKwh;
    }

    /** The soonest booking still to come, or null when there is none. */
    @Nullable
    public Reservation getNextBooking() {
        return nextBooking;
    }
}
