/*
 * ---------------------------------------------------------------------------
 * File        : MicrogridNode.java
 * Project     : Smart Solar Microgrid Trading System - SE4040 Assignment 1
 * Author      : NIMSARA R V P P (IT 23215306)
 * Created     : 2026-09-20
 * Description : A microgrid node, as the map and booking screens use it.
 *
 * It is called node, not station, because that is the word the assignment
 * uses. The Web API calls the same thing a Station. StationDto keeps that
 * name, because it copies the wire format exactly. The rename happens
 * here, in one place.
 *
 * Immutable    Every field is final. The map holds a list of these while
 *               it draws markers. Nothing can change one halfway through.
 * ---------------------------------------------------------------------------
 */
package lk.sliit.solarmicrogrid.model;

import androidx.annotation.NonNull;
import androidx.annotation.Nullable;

import lk.sliit.solarmicrogrid.data.remote.dto.StationDto;

public final class MicrogridNode {

    private final String id;
    private final String stationCode;
    private final String name;
    private final double latitude;
    private final double longitude;
    private final String addressLine;
    private final double capacityKwh;
    private final int totalBatterySlots;
    private final int availableBatterySlots;
    private final boolean active;

    public MicrogridNode(@NonNull String id,
                         @NonNull String stationCode,
                         @NonNull String name,
                         double latitude,
                         double longitude,
                         @NonNull String addressLine,
                         double capacityKwh,
                         int totalBatterySlots,
                         int availableBatterySlots,
                         boolean active) {
        this.id = id;
        this.stationCode = stationCode;
        this.name = name;
        this.latitude = latitude;
        this.longitude = longitude;
        this.addressLine = addressLine;
        this.capacityKwh = capacityKwh;
        this.totalBatterySlots = totalBatterySlots;
        this.availableBatterySlots = availableBatterySlots;
        this.active = active;
    }

    /**
     * Turns one node from the API reply into a MicrogridNode.
     *
     * The location is checked for null. A node saved with no coordinates
     * would otherwise crash the map when it tried to place a marker.
     */
    @NonNull
    public static MicrogridNode from(@NonNull StationDto dto) {
        return new MicrogridNode(
                orEmpty(dto.getId()),
                orEmpty(dto.getStationCode()),
                orEmpty(dto.getName()),
                dto.getLocation() == null ? 0d : dto.getLocation().getLatitude(),
                dto.getLocation() == null ? 0d : dto.getLocation().getLongitude(),
                dto.getLocation() == null ? "" : orEmpty(dto.getLocation().getAddressLine()),
                dto.getCapacityKwh(),
                dto.getTotalBatterySlots(),
                dto.getAvailableBatterySlots(),
                dto.isActive());
    }

    public String getId() {
        return id;
    }

    public String getStationCode() {
        return stationCode;
    }

    public String getName() {
        return name;
    }

    public double getLatitude() {
        return latitude;
    }

    public double getLongitude() {
        return longitude;
    }

    /** Where it is, in words. May be empty. */
    public String getAddressLine() {
        return addressLine;
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

    /** A closed node still shows on the map. It cannot be booked. */
    public boolean isActive() {
        return active;
    }

    @NonNull
    private static String orEmpty(@Nullable String value) {
        return value == null ? "" : value;
    }
}
