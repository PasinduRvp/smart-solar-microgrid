/*
 * ---------------------------------------------------------------------------
 * File        : DashboardApi.java
 * Project     : Smart Solar Microgrid Trading System - SE4040 Assignment 1
 * Author      : L K P Yasith (IT 23380196)
 * Created     : 2026-09-20
 * Description : The counts shown on the home screens. Task E1.
 *
 * Why the server counts
 *              The app could download every booking and count them here.
 *              That would move far more data than a handful of numbers, and
 *              the totals would drift away from the web application the
 *              moment either side changed how it counts.
 *              One endpoint means both clients always agree.
 *
 * Token       The controller is behind [Authorize], so the call needs a
 *              token. AuthInterceptor adds it.
 *
 * SOLID       Interface Segregation. Dashboard counts only.
 * ---------------------------------------------------------------------------
 */
package lk.sliit.solarmicrogrid.data.remote;

import lk.sliit.solarmicrogrid.data.remote.dto.ProsumerDashboardDto;
import retrofit2.Call;
import retrofit2.http.GET;
import retrofit2.http.Path;

public interface DashboardApi {

    /**
     * The counts for one prosumer.
     *
     * The NIC is in the path. The server still limits a prosumer to their
     * own numbers, so another NIC here is refused rather than answered.
     */
    @GET("api/dashboard/prosumer/{nic}")
    Call<ProsumerDashboardDto> getProsumerDashboard(@Path("nic") String nic);
}
