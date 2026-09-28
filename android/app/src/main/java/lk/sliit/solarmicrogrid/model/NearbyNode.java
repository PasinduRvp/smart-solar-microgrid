/*
 * ---------------------------------------------------------------------------
 * File        : NearbyNode.java
 * Project     : Smart Solar Microgrid Trading System - SE4040 Assignment 1
 * Author      : NIMSARA R V P P (IT 23215306)
 * Created     : 2026-09-20
 * Description : A microgrid node, plus how far it is from the user.
 *
 * Why distance is not a field on MicrogridNode
 *               A node has a fixed position. A distance only exists from
 *               where somebody is standing. Putting it on the node would
 *               mean every cached node carried a number that was true once,
 *               at a place the user has since left.
 *               Keeping them apart means the cache can never hold an old
 *               distance.
 * ---------------------------------------------------------------------------
 */
package lk.sliit.solarmicrogrid.model;

import androidx.annotation.NonNull;

import java.util.Locale;

import lk.sliit.solarmicrogrid.data.remote.dto.NearbyStationDto;

public final class NearbyNode {

    /**
     * Used when the distance is not known.
     *
     * That happens when the user did not allow location. A named constant is
     * used instead of a bare -1, so a reader does not have to guess what the
     * number means.
     */
    public static final double UNKNOWN_DISTANCE = -1d;

    private final MicrogridNode node;
    private final double distanceKm;

    public NearbyNode(@NonNull MicrogridNode node, double distanceKm) {
        this.node = node;
        this.distanceKm = distanceKm;
    }

    @NonNull
    public static NearbyNode from(@NonNull NearbyStationDto dto) {
        return new NearbyNode(
                MicrogridNode.from(dto.getStation()),
                dto.getDistanceKm());
    }

    @NonNull
    public MicrogridNode getNode() {
        return node;
    }

    public double getDistanceKm() {
        return distanceKm;
    }

    /**
     * The distance the way a person would say it.
     * Metres under one kilometre. One decimal place above it.
     *
     * 300 m is easier to picture than 0.3 km. And 12.47 km is more detail
     * than a driver needs.
     *
     * Locale.ROOT keeps the decimal point the same in every language, while
     * the unit stays English.
     */
    @NonNull
    public String getDistanceText() {
        if (distanceKm < 1d) {
            return String.format(Locale.ROOT, "%d m", Math.round(distanceKm * 1000));
        }
        return String.format(Locale.ROOT, "%.1f km", distanceKm);
    }
}
