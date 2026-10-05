/*
 * ---------------------------------------------------------------------------
 * File        : NearbyStationDto.java
 * Project     : Smart Solar Microgrid Trading System - SE4040 Assignment 1
 * Author      : NIMSARA R V P P (IT 23215306)
 * Created     : 2026-09-20
 * Description : A node together with how far away it is.
 *
 * Mirrors      NearbyStationDto in the Web API. It wraps a station instead
 *               of repeating its fields.
 *
 * Why the server measures the distance
 *               The API already knows where every node is. It can sort and
 *               filter in one pass. Downloading every node and measuring on
 *               the phone would move more data. It would also give a
 *               different answer from the web application, which asks this
 *               same endpoint.
 * ---------------------------------------------------------------------------
 */
package lk.sliit.solarmicrogrid.data.remote.dto;

public final class NearbyStationDto {

    private StationDto station;
    private double distanceKm;

    public StationDto getStation() {
        return station;
    }

    public double getDistanceKm() {
        return distanceKm;
    }
}
