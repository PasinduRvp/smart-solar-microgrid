/*
 * ---------------------------------------------------------------------------
 * File        : GeoLocationDto.java
 * Project     : Smart Solar Microgrid Trading System - SE4040 Assignment 1
 * Author      : NIMSARA R V P P (IT 23215306)
 * Created     : 2026-09-20
 * Description : Where a microgrid node is.
 *
 * Mirrors      GeoLocationDto in the Web API.
 *               The two numbers are what the map puts a marker on.
 *               addressLine is what a person reads. 6.9271, 79.8612 tells a
 *               prosumer nothing about where to drive.
 * ---------------------------------------------------------------------------
 */
package lk.sliit.solarmicrogrid.data.remote.dto;

public final class GeoLocationDto {

    private double latitude;
    private double longitude;
    private String addressLine;

    public double getLatitude() {
        return latitude;
    }

    public double getLongitude() {
        return longitude;
    }

    public String getAddressLine() {
        return addressLine;
    }
}
