/*
 * ---------------------------------------------------------------------------
 * File        : StationApi.java
 * Project     : Smart Solar Microgrid Trading System - SE4040 Assignment 1
 * Author      : NIMSARA R V P P (IT 23215306)
 * Created     : 2026-09-20
 * Description : Reading microgrid nodes. Tasks C2, C3 and C4.
 *
 * Read only    Creating, editing and deactivating a node is Backoffice work.
 *               That is done in the web application. The phone only reads.
 *               So the write endpoints are left out on purpose. An endpoint
 *               that is not here cannot be called by mistake.
 *
 * Token        The whole Stations controller is behind [Authorize]. Every
 *               call here needs a token. AuthInterceptor adds it, so no
 *               method below mentions it.
 *
 * SOLID        Interface Segregation. Node reads only.
 * ---------------------------------------------------------------------------
 */
package lk.sliit.solarmicrogrid.data.remote;

import java.util.List;

import lk.sliit.solarmicrogrid.data.remote.dto.NearbyStationDto;
import lk.sliit.solarmicrogrid.data.remote.dto.StationDto;
import retrofit2.Call;
import retrofit2.http.GET;
import retrofit2.http.Path;
import retrofit2.http.Query;

public interface StationApi {

    /**
     * Every node.
     *
     * @param activeOnly true leaves out nodes a Backoffice officer closed.
     *                   The map passes false. A closed node is still drawn,
     *                   in another colour, and cannot be booked. A prosumer
     *                   who used it before can see that it is closed, instead
     *                   of finding it simply gone.
     */
    @GET("api/stations")
    Call<List<StationDto>> getAll(@Query("activeOnly") boolean activeOnly);

    /**
     * Nodes within radiusKm of a point. Each has its distance. Nearest first.
     *
     * The server does the distance sum. So the phone and the web application
     * always agree on what counts as nearby.
     */
    @GET("api/stations/nearby")
    Call<List<NearbyStationDto>> getNearby(
            @Query("lat") double latitude,
            @Query("lng") double longitude,
            @Query("radiusKm") double radiusKm);

    /**
     * One node in full, with its opening hours.
     *
     * The detail screen asks for this instead of reading the cache. The
     * schedule and the free battery bays change as other prosumers book.
     */
    @GET("api/stations/{id}")
    Call<StationDto> getById(@Path("id") String id);
}
