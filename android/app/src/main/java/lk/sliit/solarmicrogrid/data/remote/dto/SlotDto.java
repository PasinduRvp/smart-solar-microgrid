/*
 * ---------------------------------------------------------------------------
 * File        : SlotDto.java
 * Project     : Smart Solar Microgrid Trading System - SE4040 Assignment 1
 * Author      : L K P Yasith (IT 23380196)
 * Created     : 2026-09-20
 * Description : One booking window at a node, as the Web API returns it.
 *
 * Mirrors      SlotResponseDto in the Web API.
 *
 * isBookable is the one to trust
 *              It is worked out by the server. It is true only when the
 *              window is open, has room left, and is still in the future.
 *              The app must not try to work this out from the other fields.
 *              The server owns that rule, and it is the one that decides.
 * ---------------------------------------------------------------------------
 */
package lk.sliit.solarmicrogrid.data.remote.dto;

public final class SlotDto {

    private String id;
    private String stationId;
    private String slotDate;
    private String startTime;
    private String endTime;
    private int totalCapacitySlots;
    private int bookedCount;
    private int remainingCapacity;
    private double energyRatePerKwh;
    private boolean isAvailable;
    private boolean isBookable;

    public String getId() {
        return id;
    }

    public String getStationId() {
        return stationId;
    }

    public String getSlotDate() {
        return slotDate;
    }

    public String getStartTime() {
        return startTime;
    }

    public String getEndTime() {
        return endTime;
    }

    public int getTotalCapacitySlots() {
        return totalCapacitySlots;
    }

    public int getBookedCount() {
        return bookedCount;
    }

    public int getRemainingCapacity() {
        return remainingCapacity;
    }

    public double getEnergyRatePerKwh() {
        return energyRatePerKwh;
    }

    public boolean isAvailable() {
        return isAvailable;
    }

    /** True when this window can still be booked right now. */
    public boolean isBookable() {
        return isBookable;
    }
}
