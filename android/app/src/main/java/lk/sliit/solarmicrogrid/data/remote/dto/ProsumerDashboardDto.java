/*
 * ---------------------------------------------------------------------------
 * File        : ProsumerDashboardDto.java
 * Project     : Smart Solar Microgrid Trading System - SE4040 Assignment 1
 * Author      : L K P Yasith (IT 23380196)
 * Created     : 2026-09-20
 * Description : The counts shown on the prosumer dashboard. Task E1.
 *
 * Mirrors      ProsumerDashboardDto in the Web API.
 *
 * Why the server counts, not the app
 *              The app could download every booking and count them on the
 *              phone. That would move far more data, and the numbers would
 *              differ from the web application the moment either side
 *              changed how it counts.
 *              One endpoint, one answer, both clients agree.
 *
 * nextBooking may be null
 *              It is the soonest booking still to come. A prosumer with no
 *              upcoming booking has none, and the screen hides that card.
 * ---------------------------------------------------------------------------
 */
package lk.sliit.solarmicrogrid.data.remote.dto;

import androidx.annotation.Nullable;

public final class ProsumerDashboardDto {

    private String prosumerNic;
    private String prosumerName;
    private long pendingCount;
    private long approvedFutureCount;
    private long completedCount;
    private long cancelledCount;
    private double totalEnergyDeliveredKwh;
    private double totalEnergyDrawnKwh;

    @Nullable
    private ReservationDto nextBooking;

    public String getProsumerNic() {
        return prosumerNic;
    }

    public String getProsumerName() {
        return prosumerName;
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
    public ReservationDto getNextBooking() {
        return nextBooking;
    }
}
