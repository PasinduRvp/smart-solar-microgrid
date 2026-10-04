/*
 * ---------------------------------------------------------------------------
 * File        : ProsumerHomeActivity.java
 * Project     : Smart Solar Microgrid Trading System - SE4040 Assignment 1
 * Author      : M T A J Yapa (IT 23278530)
 * Created     : 2026-09-20
 * Description : Where a Solar Prosumer lands after signing in.
 *               It also shows their booking counts. Task E1.
 *
 * Everything shared with the operator screen is in RoleHomeActivity.
 * This class adds the one thing only a prosumer sees: the counts.
 *
 * OOP          showSession is overridden, not copied. The base class still
 *              fills the name, the NIC and the role. This adds to that
 *              instead of repeating it.
 *
 * The server counts, not the app
 *              One call returns every number. Counting on the phone would
 *              mean downloading every booking, and the totals could drift
 *              away from the web application.
 * ---------------------------------------------------------------------------
 */
package lk.sliit.solarmicrogrid.ui.home;

import android.view.View;

import androidx.annotation.NonNull;
import androidx.annotation.Nullable;
import androidx.annotation.StringRes;

import java.util.Locale;

import lk.sliit.solarmicrogrid.R;
import lk.sliit.solarmicrogrid.data.repository.AuthRepository;
import lk.sliit.solarmicrogrid.data.repository.DashboardRepository;
import lk.sliit.solarmicrogrid.model.ProsumerDashboard;
import lk.sliit.solarmicrogrid.model.Reservation;
import lk.sliit.solarmicrogrid.model.Session;
import lk.sliit.solarmicrogrid.util.DateTimes;
import lk.sliit.solarmicrogrid.util.TextFormat;

public final class ProsumerHomeActivity extends RoleHomeActivity {

    @StringRes
    @Override
    protected int titleResource() {
        return R.string.home_prosumer_title;
    }

    /**
     * Fills the header as usual, then loads the counts.
     *
     * The base class runs this from onStart, so the numbers refresh every
     * time the screen comes to the front. Coming back from booking something
     * then shows the new count straight away.
     */
    @Override
    protected void showSession(@NonNull Session session) {
        super.showSession(session);
        loadCounts(session.getNic());
    }

    private void loadCounts(@NonNull String nic) {

        DashboardRepository.create().loadProsumerDashboard(nic,
                new AuthRepository.Callback<ProsumerDashboard>() {

                    @Override
                    public void onSuccess(@Nullable ProsumerDashboard dashboard) {
                        if (isFinishing() || isDestroyed() || dashboard == null) {
                            return;
                        }
                        show(dashboard);
                    }

                    @Override
                    public void onFailure(@NonNull String message) {
                        // The counts are useful, not essential. A failure
                        // leaves the card hidden rather than covering the
                        // screen with an error the user cannot act on.
                        if (!isFinishing() && !isDestroyed()) {
                            binding.cardStats.setVisibility(View.GONE);
                        }
                    }
                });
    }

    private void show(@NonNull ProsumerDashboard dashboard) {

        binding.cardStats.setVisibility(View.VISIBLE);

        binding.tvPendingCount.setText(count(dashboard.getPendingCount()));
        binding.tvApprovedCount.setText(count(dashboard.getApprovedFutureCount()));
        binding.tvCompletedCount.setText(count(dashboard.getCompletedCount()));

        showNextBooking(dashboard.getNextBooking());

        binding.tvEnergyTotals.setText(getString(
                R.string.dash_energy_summary,
                TextFormat.capacity(dashboard.getTotalEnergyDeliveredKwh()),
                TextFormat.capacity(dashboard.getTotalEnergyDrawnKwh())));
    }

    /** The node and the time of the soonest booking still to come. */
    private void showNextBooking(@Nullable Reservation next) {

        if (next == null) {
            binding.tvNextBooking.setText(R.string.dash_no_next_booking);
            return;
        }

        binding.tvNextBooking.setText(getString(
                R.string.dash_next_booking_value,
                next.getStationName(),
                DateTimes.showDateAndTime(next.getReservationDateTime())));
    }

    /**
     * Locale.US so the digits are the ones the rest of the screen uses.
     * The default locale would print Arabic digits on a phone set to Arabic,
     * next to English labels.
     */
    @NonNull
    private static String count(long value) {
        return String.format(Locale.US, "%d", value);
    }
}
