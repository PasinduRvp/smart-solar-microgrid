/*
 * ---------------------------------------------------------------------------
 * File        : EnergySlot.java
 * Project     : Smart Solar Microgrid Trading System - SE4040 Assignment 1
 * Author      : L K P Yasith (IT 23380196)
 * Created     : 2026-09-20
 * Description : One booking window, as the picker screen uses it.
 *
 * Immutable    Every field is final. The picker holds a list of these while
 *              it draws the windows. Nothing can change one halfway through.
 *
 * bookable is the server answer
 *              It is true only when the window is open, has room left, and is
 *              still in the future. The app never works that out from the
 *              other fields. The server owns the rule.
 * ---------------------------------------------------------------------------
 */
package lk.sliit.solarmicrogrid.model;

import androidx.annotation.NonNull;
import androidx.annotation.Nullable;

import lk.sliit.solarmicrogrid.data.remote.dto.SlotDto;

public final class EnergySlot {

    private final String id;
    private final String stationId;
    private final String slotDate;
    private final String startTime;
    private final String endTime;
    private final int totalCapacitySlots;
    private final int remainingCapacity;
    private final double energyRatePerKwh;
    private final boolean bookable;

    public EnergySlot(@NonNull String id,
                      @NonNull String stationId,
                      @NonNull String slotDate,
                      @NonNull String startTime,
                      @NonNull String endTime,
                      int totalCapacitySlots,
                      int remainingCapacity,
                      double energyRatePerKwh,
                      boolean bookable) {
        this.id = id;
        this.stationId = stationId;
        this.slotDate = slotDate;
        this.startTime = startTime;
        this.endTime = endTime;
        this.totalCapacitySlots = totalCapacitySlots;
        this.remainingCapacity = remainingCapacity;
        this.energyRatePerKwh = energyRatePerKwh;
        this.bookable = bookable;
    }

    @NonNull
    public static EnergySlot from(@NonNull SlotDto dto) {
        return new EnergySlot(
                orEmpty(dto.getId()),
                orEmpty(dto.getStationId()),
                orEmpty(dto.getSlotDate()),
                orEmpty(dto.getStartTime()),
                orEmpty(dto.getEndTime()),
                dto.getTotalCapacitySlots(),
                dto.getRemainingCapacity(),
                dto.getEnergyRatePerKwh(),
                dto.isBookable());
    }

    public String getId() {
        return id;
    }

    public String getStationId() {
        return stationId;
    }

    /** The day of the window, as the API sent it. */
    public String getSlotDate() {
        return slotDate;
    }

    /** For example 08:00. */
    public String getStartTime() {
        return startTime;
    }

    /** For example 09:00. */
    public String getEndTime() {
        return endTime;
    }

    public int getTotalCapacitySlots() {
        return totalCapacitySlots;
    }

    public int getRemainingCapacity() {
        return remainingCapacity;
    }

    public double getEnergyRatePerKwh() {
        return energyRatePerKwh;
    }

    /** True when this window can be booked right now. */
    public boolean isBookable() {
        return bookable;
    }

    /** For example 08:00 to 09:00. */
    @NonNull
    public String getTimeRange() {
        return startTime + " to " + endTime;
    }

    @NonNull
    private static String orEmpty(@Nullable String value) {
        return value == null ? "" : value;
    }
}
