/*
 * ---------------------------------------------------------------------------
 * File        : ScheduleEntryDto.java
 * Project     : Smart Solar Microgrid Trading System - SE4040 Assignment 1
 * Author      : NIMSARA R V P P (IT 23215306)
 * Created     : 2026-09-20
 * Description : One day of a node's opening hours.
 *
 * Mirrors      ScheduleEntryDto in the Web API. The server checks that
 *               dayOfWeek is a full English day name. It checks both times
 *               are in HH:mm form.
 *
 * Why it matters
 *               Booking windows are made from the schedule. A node with no
 *               schedule has nothing to book. Showing the hours is how a
 *               prosumer knows if a node is worth the drive.
 * ---------------------------------------------------------------------------
 */
package lk.sliit.solarmicrogrid.data.remote.dto;

public final class ScheduleEntryDto {

    private String dayOfWeek;
    private String openTime;
    private String closeTime;

    public String getDayOfWeek() {
        return dayOfWeek;
    }

    public String getOpenTime() {
        return openTime;
    }

    public String getCloseTime() {
        return closeTime;
    }
}
