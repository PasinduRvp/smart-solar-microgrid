/*
 * ---------------------------------------------------------------------------
 * File        : DashboardRepository.java
 * Project     : Smart Solar Microgrid Trading System - SE4040 Assignment 1
 * Author      : L K P Yasith (IT 23380196)
 * Created     : 2026-09-20
 * Description : The counts for the home screens. Task E1.
 *
 * Nothing is cached
 *              The numbers change as a Grid Operator approves bookings, and
 *              as the prosumer books more. A saved copy would show a count
 *              that is already wrong, and a dashboard that lies is worse
 *              than one that takes a moment to load.
 *
 * SOLID       Single Responsibility. One call, one shape.
 * SOLID       Dependency Inversion. The API is passed in, so a test can use
 *              a fake without a server.
 * ---------------------------------------------------------------------------
 */
package lk.sliit.solarmicrogrid.data.repository;

import androidx.annotation.NonNull;

import lk.sliit.solarmicrogrid.R;
import lk.sliit.solarmicrogrid.SolarApp;
import lk.sliit.solarmicrogrid.data.remote.ApiClient;
import lk.sliit.solarmicrogrid.data.remote.ApiErrors;
import lk.sliit.solarmicrogrid.data.remote.DashboardApi;
import lk.sliit.solarmicrogrid.data.remote.dto.ProsumerDashboardDto;
import lk.sliit.solarmicrogrid.data.repository.AuthRepository.Callback;
import lk.sliit.solarmicrogrid.model.ProsumerDashboard;
import retrofit2.Call;
import retrofit2.Response;

public final class DashboardRepository {

    private final DashboardApi dashboardApi;

    public DashboardRepository(@NonNull DashboardApi dashboardApi) {
        this.dashboardApi = dashboardApi;
    }

    @NonNull
    public static DashboardRepository create() {
        return new DashboardRepository(ApiClient.create(DashboardApi.class));
    }

    /** The counts for one prosumer. */
    public void loadProsumerDashboard(@NonNull String nic,
                                      @NonNull Callback<ProsumerDashboard> callback) {

        dashboardApi.getProsumerDashboard(nic)
                .enqueue(new retrofit2.Callback<ProsumerDashboardDto>() {

                    @Override
                    public void onResponse(@NonNull Call<ProsumerDashboardDto> call,
                                           @NonNull Response<ProsumerDashboardDto> response) {

                        ProsumerDashboardDto body = response.body();

                        if (!response.isSuccessful() || body == null) {
                            callback.onFailure(ApiErrors.messageFrom(response));
                            return;
                        }
                        callback.onSuccess(ProsumerDashboard.from(body));
                    }

                    @Override
                    public void onFailure(@NonNull Call<ProsumerDashboardDto> call,
                                          @NonNull Throwable t) {
                        callback.onFailure(SolarApp.get()
                                .getString(R.string.error_no_connection));
                    }
                });
    }
}
