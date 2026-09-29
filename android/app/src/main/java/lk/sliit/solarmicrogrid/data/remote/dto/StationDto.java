/*
 * ---------------------------------------------------------------------------
 * File        : StationDto.java
 * Project     : Smart Solar Microgrid Trading System - SE4040 Assignment 1
 * Author      : NIMSARA R V P P (IT 23215306)
 * Created     : 2026-09-20
 * Description : A microgrid node as the Web API returns it.
 *
 * Mirrors      StationResponseDto in the Web API.
 *
 * Why it is cached
 *               The map must still show nodes when the signal drops. Redrawing
 *               markers should not need a new HTTP call every time the map is
 *               moved. StationStore keeps the fields the map needs in SQLite.
 * ---------------------------------------------------------------------------
 */
package lk.sliit.solarmicrogrid.data.remote.dto;

import java.util.List;

public final class StationDto {

    private String id;
    private String stationCode;
    private String name;
    private GeoLocationDto location;
    private double capacityKwh;
    private int totalBatterySlots;
    private int availableBatterySlots;
    private List<ScheduleEntryDto> schedule;
    private boolean isActive;

    public String getId() {
        return id;
    }

    public String getStationCode() {
        return stationCode;
    }

    public String getName() {
        return name;
    }

    public GeoLocationDto getLocation() {
        return location;
    }

    public double getCapacityKwh() {
        return capacityKwh;
    }

    public int getTotalBatterySlots() {
        return totalBatterySlots;
    }

    public int getAvailableBatterySlots() {
        return availableBatterySlots;
    }

    /** Opening hours. One entry per day the node is open. May be empty. */
    public List<ScheduleEntryDto> getSchedule() {
        return schedule;
    }

    public boolean isActive() {
        return isActive;
    }
}
