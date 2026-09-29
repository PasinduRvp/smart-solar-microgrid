/*
 * ---------------------------------------------------------------------------
 * File        : TransferSummaryActivity.java
 * Project     : Smart Solar Microgrid Trading System - SE4040 Assignment 1
 * Author      : NIMSARA R V P P (IT 23215306)
 * Created     : 2026-09-21
 * Description : Shown after the operator marks a transfer complete. Task E5.
 *
 * Why a page and not a toast
 *              The operator is standing at the node with a person in front
 *              of them. They need to see plainly that it worked, and to read
 *              the booking number back if asked. A toast is gone in two
 *              seconds and cannot be read again.
 *
 *              It also matches the summary a prosumer gets after booking,
 *              changing or cancelling, so both sides of the app behave the
 *              same way.
 *
 * It shows the server reply
 *              The status comes from the API response, so it says what the
 *              server really stored. Not what the app hoped it stored.
 *
 * Two ways out
 *              Scan another opens the camera again, because an operator
 *              serves one prosumer after another.
 *              Done returns to the operator home screen.
 *
 * SOLID       Single Responsibility. It shows one finished booking.
 *              No HTTP, no SQL, no camera.
 * ---------------------------------------------------------------------------
 */
package lk.sliit.solarmicrogrid.ui.operator;

import android.content.Context;
import android.content.Intent;
import android.os.Bundle;

import androidx.activity.OnBackPressedCallback;
import androidx.annotation.NonNull;
import androidx.annotation.Nullable;

import lk.sliit.solarmicrogrid.R;
import lk.sliit.solarmicrogrid.databinding.ActivityTransferSummaryBinding;
import lk.sliit.solarmicrogrid.model.Reservation;
import lk.sliit.solarmicrogrid.ui.BaseActivity;
import lk.sliit.solarmicrogrid.ui.home.OperatorHomeActivity;
import lk.sliit.solarmicrogrid.util.TextFormat;

public final class TransferSummaryActivity extends BaseActivity {

    private static final String EXTRA_PROSUMER = "prosumer";
    private static final String EXTRA_NUMBER = "number";
    private static final String EXTRA_NODE = "node";
    private static final String EXTRA_ENERGY = "energy";
    private static final String EXTRA_DIRECTION = "direction";
    private static final String EXTRA_STATUS = "status";

    /**
     * Builds the intent that opens this screen.
     *
     * The booking is broken into plain values, the same way the booking
     * summary screen does it.
     */
    @NonNull
    public static Intent intentFor(@NonNull Context context, @NonNull Reservation booking) {
        Intent intent = new Intent(context, TransferSummaryActivity.class);
        intent.putExtra(EXTRA_PROSUMER, booking.getProsumerName());
        intent.putExtra(EXTRA_NUMBER, booking.getReservationNo());
        intent.putExtra(EXTRA_NODE, booking.getStationName());
        intent.putExtra(EXTRA_ENERGY, booking.getEnergyKwh());
        intent.putExtra(EXTRA_DIRECTION, booking.getDirection());
        intent.putExtra(EXTRA_STATUS, booking.getStatus());
        return intent;
    }

    private ActivityTransferSummaryBinding binding;

    @Override
    protected void onCreate(@Nullable Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);

        binding = ActivityTransferSummaryBinding.inflate(getLayoutInflater());
        setContentView(binding.getRoot());
        applySystemBarInsets(binding.root);

        binding.btnScanAnother.setOnClickListener(view -> goHome(true));
        binding.btnDone.setOnClickListener(view -> goHome(false));

        // Back does the same as Done. This screen is the end of an action,
        // so both ways out land in the same place.
        getOnBackPressedDispatcher().addCallback(this, new OnBackPressedCallback(true) {
            @Override
            public void handleOnBackPressed() {
                goHome(false);
            }
        });

        showBooking();
    }

    private void showBooking() {

        Intent from = getIntent();

        binding.tvProsumer.setText(from.getStringExtra(EXTRA_PROSUMER));
        binding.tvNumber.setText(from.getStringExtra(EXTRA_NUMBER));
        binding.tvNode.setText(from.getStringExtra(EXTRA_NODE));
        binding.tvStatus.setText(from.getStringExtra(EXTRA_STATUS));

        String energy = getString(R.string.bookings_energy_value,
                TextFormat.capacity(from.getDoubleExtra(EXTRA_ENERGY, 0d)));
        String direction = getString(
                Reservation.DIRECTION_DELIVER.equals(from.getStringExtra(EXTRA_DIRECTION))
                        ? R.string.booking_deliver
                        : R.string.booking_draw);
        binding.tvEnergy.setText(getString(
                R.string.bookings_energy_line, energy, direction));
    }

    /**
     * Returns to the operator home screen.
     *
     * CLEAR_TOP with SINGLE_TOP reuses the home screen that is already open
     * instead of stacking another copy, and removes the scan screens above
     * it. So Back from home does not walk back through a finished job.
     *
     * @param thenScan true to open the camera again as soon as home is back.
     */
    private void goHome(boolean thenScan) {

        Intent home = new Intent(this, OperatorHomeActivity.class);
        home.addFlags(Intent.FLAG_ACTIVITY_CLEAR_TOP | Intent.FLAG_ACTIVITY_SINGLE_TOP);
        home.putExtra(OperatorHomeActivity.EXTRA_START_SCAN, thenScan);

        startActivity(home);
        finish();
    }
}
