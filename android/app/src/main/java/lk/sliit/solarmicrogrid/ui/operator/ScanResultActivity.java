/*
 * ---------------------------------------------------------------------------
 * File        : ScanResultActivity.java
 * Project     : Smart Solar Microgrid Trading System - SE4040 Assignment 1
 * Author      : NIMSARA R V P P (IT 23215306)
 * Created     : 2026-09-20
 * Description : What the Grid Operator sees after scanning a QR code, and
 *               where they finish the transfer. Tasks E4 and E5.
 *
 * The scan is already checked before this opens
 *              OperatorHomeActivity sends the scanned text to the server
 *              first. This screen only opens once the server has said the
 *              code belongs to a real booking that can still be used.
 *              So reaching this screen is itself the proof.
 *
 * Only Approved can be completed
 *              The button is hidden for a booking that is already finished.
 *              The server refuses a second completion anyway, but an
 *              operator should not be offered a button that will fail.
 *
 * Scan another
 *              An operator serves one prosumer after another. Going back to
 *              the camera in one tap saves them walking back through the
 *              home screen every time.
 *
 * SOLID       Single Responsibility. It shows one booking and completes it.
 *              No camera code, and no QR parsing.
 * ---------------------------------------------------------------------------
 */
package lk.sliit.solarmicrogrid.ui.operator;

import android.content.Context;
import android.content.Intent;
import android.os.Bundle;
import android.view.View;
import android.widget.Toast;

import androidx.annotation.NonNull;
import androidx.annotation.Nullable;

import lk.sliit.solarmicrogrid.R;
import lk.sliit.solarmicrogrid.data.repository.AuthRepository;
import lk.sliit.solarmicrogrid.data.repository.BookingRepository;
import lk.sliit.solarmicrogrid.databinding.ActivityScanResultBinding;
import lk.sliit.solarmicrogrid.model.Reservation;
import lk.sliit.solarmicrogrid.ui.BaseActivity;
import lk.sliit.solarmicrogrid.ui.home.OperatorHomeActivity;
import lk.sliit.solarmicrogrid.util.DateTimes;
import lk.sliit.solarmicrogrid.util.TextFormat;

public final class ScanResultActivity extends BaseActivity {

    private static final String EXTRA_ID = "id";
    private static final String EXTRA_NUMBER = "number";
    private static final String EXTRA_PROSUMER = "prosumer";
    private static final String EXTRA_NODE = "node";
    private static final String EXTRA_WHEN = "when";
    private static final String EXTRA_ENERGY = "energy";
    private static final String EXTRA_DIRECTION = "direction";
    private static final String EXTRA_STATUS = "status";

    /**
     * Builds the intent that opens this screen.
     *
     * The booking is broken into plain values rather than passed as one
     * object, the same way the booking summary screen does it.
     */
    @NonNull
    public static Intent intentFor(@NonNull Context context, @NonNull Reservation booking) {
        Intent intent = new Intent(context, ScanResultActivity.class);
        intent.putExtra(EXTRA_ID, booking.getId());
        intent.putExtra(EXTRA_NUMBER, booking.getReservationNo());
        intent.putExtra(EXTRA_PROSUMER, booking.getProsumerName());
        intent.putExtra(EXTRA_NODE, booking.getStationName());
        intent.putExtra(EXTRA_WHEN, booking.getReservationDateTime());
        intent.putExtra(EXTRA_ENERGY, booking.getEnergyKwh());
        intent.putExtra(EXTRA_DIRECTION, booking.getDirection());
        intent.putExtra(EXTRA_STATUS, booking.getStatus());
        return intent;
    }

    private ActivityScanResultBinding binding;
    private BookingRepository bookingRepository;
    private String bookingId;

    @Override
    protected void onCreate(@Nullable Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);

        binding = ActivityScanResultBinding.inflate(getLayoutInflater());
        setContentView(binding.getRoot());
        applySystemBarInsets(binding.root);

        bookingId = getIntent().getStringExtra(EXTRA_ID);
        if (bookingId == null || bookingId.trim().isEmpty()) {
            finish();
            return;
        }

        bookingRepository = BookingRepository.create();

        binding.toolbar.setNavigationOnClickListener(view -> finish());
        binding.btnComplete.setOnClickListener(view -> completeTransfer());
        binding.btnScanAnother.setOnClickListener(view -> scanAnother());

        showBooking();
    }

    private void showBooking() {

        Intent from = getIntent();

        binding.tvProsumer.setText(from.getStringExtra(EXTRA_PROSUMER));
        binding.tvNumber.setText(from.getStringExtra(EXTRA_NUMBER));
        binding.tvNode.setText(from.getStringExtra(EXTRA_NODE));
        binding.tvWhen.setText(DateTimes.showDateAndTime(from.getStringExtra(EXTRA_WHEN)));

        String energy = getString(R.string.bookings_energy_value,
                TextFormat.capacity(from.getDoubleExtra(EXTRA_ENERGY, 0d)));
        String direction = getString(
                Reservation.DIRECTION_DELIVER.equals(from.getStringExtra(EXTRA_DIRECTION))
                        ? R.string.booking_deliver
                        : R.string.booking_draw);
        binding.tvEnergy.setText(getString(
                R.string.bookings_energy_line, energy, direction));

        showStatus(from.getStringExtra(EXTRA_STATUS));
    }

    /**
     * A booking that is already finished cannot be finished again, so the
     * button goes away and the screen says why.
     */
    private void showStatus(@Nullable String status) {

        binding.tvStatus.setText(status);

        boolean alreadyDone = Reservation.STATUS_COMPLETED.equals(status);
        binding.btnComplete.setVisibility(alreadyDone ? View.GONE : View.VISIBLE);

        if (alreadyDone) {
            binding.tvVerified.setText(R.string.scan_already_completed);
        }
    }

    // ----- Completing the transfer. Task E5 ---------------------------------

    private void completeTransfer() {

        showBusy(true);

        bookingRepository.completeBooking(bookingId, new AuthRepository.Callback<Reservation>() {

            @Override
            public void onSuccess(@Nullable Reservation completed) {
                if (isGone() || completed == null) {
                    return;
                }
                showBusy(false);

                // The summary page states what happened, and offers Scan
                // another. This screen closes so Back cannot return to a
                // job that is already finished.
                startActivity(TransferSummaryActivity.intentFor(
                        ScanResultActivity.this, completed));
                finish();
            }

            @Override
            public void onFailure(@NonNull String message) {
                if (isGone()) {
                    return;
                }
                showBusy(false);
                Toast.makeText(ScanResultActivity.this, message, Toast.LENGTH_LONG).show();
            }
        });
    }

    /**
     * Goes back to the operator home screen and opens the camera again.
     *
     * Used when the scanned booking is not the right one, or is already
     * finished, so there is nothing to complete here.
     */
    private void scanAnother() {
        Intent home = new Intent(this, OperatorHomeActivity.class);
        home.addFlags(Intent.FLAG_ACTIVITY_CLEAR_TOP | Intent.FLAG_ACTIVITY_SINGLE_TOP);
        home.putExtra(OperatorHomeActivity.EXTRA_START_SCAN, true);
        startActivity(home);
        finish();
    }

    private void showBusy(boolean busy) {
        binding.progress.setVisibility(busy ? View.VISIBLE : View.GONE);
        binding.btnComplete.setEnabled(!busy);
        binding.btnScanAnother.setEnabled(!busy);
    }

    /** True when the screen is closing. A late reply must then be dropped. */
    private boolean isGone() {
        return isFinishing() || isDestroyed();
    }
}
