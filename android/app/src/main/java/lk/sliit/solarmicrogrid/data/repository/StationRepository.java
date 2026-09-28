/*
 * ---------------------------------------------------------------------------
 * File        : StationRepository.java
 * Project     : Smart Solar Microgrid Trading System - SE4040 Assignment 1
 * Author      : NIMSARA R V P P (IT 23215306)
 * Created     : 2026-09-20
 * Description : Reading microgrid nodes, with the SQLite cache in front.
 *               Tasks C2, C3 and C4.
 *
 * Cache first
 *               cachedNodes() answers at once from SQLite. The map can drop
 *               its markers the moment it opens. loadAll() then refreshes.
 *               A map that stays empty until a network call returns looks
 *               broken. With no signal it would stay empty for good.
 *
 * Closed nodes are kept
 *               loadAll asks for every node, not active ones only. A node a
 *               Backoffice officer switched off is still drawn, in a
 *               different colour. A prosumer who used it before can see it
 *               is closed, instead of finding it simply gone.
 *
 * SOLID        Single Responsibility. Node reads and their caching.
 * SOLID        Dependency Inversion. The API and the store are passed in,
 *               so a test can supply fakes for both.
 * ---------------------------------------------------------------------------
 */
package lk.sliit.solarmicrogrid.data.repository;

import androidx.annotation.NonNull;

import java.util.ArrayList;
import java.util.List;

import lk.sliit.solarmicrogrid.R;
import lk.sliit.solarmicrogrid.SolarApp;
import lk.sliit.solarmicrogrid.data.local.StationStore;
import lk.sliit.solarmicrogrid.data.remote.ApiClient;
import lk.sliit.solarmicrogrid.data.remote.ApiErrors;
import lk.sliit.solarmicrogrid.data.remote.StationApi;
import lk.sliit.solarmicrogrid.data.remote.dto.NearbyStationDto;
import lk.sliit.solarmicrogrid.data.remote.dto.StationDto;
import lk.sliit.solarmicrogrid.data.repository.AuthRepository.Callback;
import lk.sliit.solarmicrogrid.model.MicrogridNode;
import lk.sliit.solarmicrogrid.model.NearbyNode;
import retrofit2.Call;
import retrofit2.Response;

public final class StationRepository {

    /** How far around the user the "nearby" search looks. */
    public static final double DEFAULT_RADIUS_KM = 25d;

    private final StationApi stationApi;
    private final StationStore stationStore;

    public StationRepository(@NonNull StationApi stationApi, @NonNull StationStore stationStore) {
        this.stationApi = stationApi;
        this.stationStore = stationStore;
    }

    @NonNull
    public static StationRepository create() {
        return new StationRepository(
                ApiClient.create(StationApi.class),
                SolarApp.get().stations());
    }

    /** Every node kept on the phone. Returns at once. No network. */
    @NonNull
    public List<MicrogridNode> cachedNodes() {
        return stationStore.readAll();
    }

    /**
     * Downloads every node and replaces the cache with it.
     */
    public void loadAll(@NonNull Callback<List<MicrogridNode>> callback) {

        stationApi.getAll(false).enqueue(new retrofit2.Callback<List<StationDto>>() {

            @Override
            public void onResponse(@NonNull Call<List<StationDto>> call,
                                   @NonNull Response<List<StationDto>> response) {

                List<StationDto> body = response.body();

                if (!response.isSuccessful() || body == null) {
                    callback.onFailure(ApiErrors.messageFrom(response));
                    return;
                }

                List<MicrogridNode> nodes = new ArrayList<>(body.size());
                for (StationDto dto : body) {
                    nodes.add(MicrogridNode.from(dto));
                }

                stationStore.replaceAll(nodes);
                callback.onSuccess(nodes);
            }

            @Override
            public void onFailure(@NonNull Call<List<StationDto>> call, @NonNull Throwable t) {
                callback.onFailure(offlineMessage());
            }
        });
    }

    /**
     * Nodes within radiusKm of a point. Nearest first.
     *
     * This does NOT touch the cache, on purpose. The answer depends on where
     * the user is standing. Saving it would keep a distance that stops being
     * true as soon as they move.
     */
    public void loadNearby(double latitude,
                           double longitude,
                           double radiusKm,
                           @NonNull Callback<List<NearbyNode>> callback) {

        stationApi.getNearby(latitude, longitude, radiusKm)
                .enqueue(new retrofit2.Callback<List<NearbyStationDto>>() {

                    @Override
                    public void onResponse(@NonNull Call<List<NearbyStationDto>> call,
                                           @NonNull Response<List<NearbyStationDto>> response) {

                        List<NearbyStationDto> body = response.body();

                        if (!response.isSuccessful() || body == null) {
                            callback.onFailure(ApiErrors.messageFrom(response));
                            return;
                        }

                        List<NearbyNode> nearby = new ArrayList<>(body.size());
                        for (NearbyStationDto dto : body) {
                            nearby.add(NearbyNode.from(dto));
                        }

                        callback.onSuccess(nearby);
                    }

                    @Override
                    public void onFailure(@NonNull Call<List<NearbyStationDto>> call,
                                          @NonNull Throwable t) {
                        callback.onFailure(offlineMessage());
                    }
                });
    }

    /**
     * One node in full, with its opening hours.
     *
     * The detail screen asks the server instead of reading the cache. The
     * free battery bays change as other prosumers book. The schedule is not
     * cached at all.
     */
    public void loadById(@NonNull String id, @NonNull Callback<StationDto> callback) {

        stationApi.getById(id).enqueue(new retrofit2.Callback<StationDto>() {

            @Override
            public void onResponse(@NonNull Call<StationDto> call,
                                   @NonNull Response<StationDto> response) {

                StationDto body = response.body();

                if (!response.isSuccessful() || body == null) {
                    callback.onFailure(ApiErrors.messageFrom(response));
                    return;
                }
                callback.onSuccess(body);
            }

            @Override
            public void onFailure(@NonNull Call<StationDto> call, @NonNull Throwable t) {
                callback.onFailure(offlineMessage());
            }
        });
    }

    @NonNull
    private static String offlineMessage() {
        return SolarApp.get().getString(R.string.error_no_connection);
    }
}
