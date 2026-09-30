/*
 * ---------------------------------------------------------------------------
 * File        : OperatorHomeActivity.java
 * Project     : Smart Solar Microgrid Trading System - SE4040 Assignment 1
 * Author      : M T A J Yapa (IT 23278530)
 * Created     : 2026-09-20
 * Description : Where a Grid Operator lands after signing in.
 *               It is also where they open the QR scanner. Task E4.
 *
 * Everything shared with the prosumer screen is in RoleHomeActivity.
 * This class adds the one thing only an operator does: scanning.
 *
 * The app does not read the code
 *              The camera returns text. That text goes straight to the
 *              server. The server says whose booking it is and whether it
 *              can still be used.
 *              So a copied code, an edited one, or a code for a cancelled
 *              booking all fail on the server, not on a check here that
 *              somebody could get around.
 *
 * ZXing asks for the camera itself
 *              The scanner screen requests the camera permission when it
 *              opens, so there is no permission code here. CAMERA is still
 *              declared in the manifest, because a permission that is not
 *              declared cannot be granted.
 * ---------------------------------------------------------------------------
 */
package lk.sliit.solarmicrogrid.ui.home;

import android.content.Intent;
import android.os.Bundle;
import android.view.View;
import android.widget.Toast;

import androidx.activity.result.ActivityResultLauncher;
import androidx.annotation.NonNull;
import androidx.annotation.Nullable;
import androidx.annotation.StringRes;

import com.journeyapps.barcodescanner.ScanContract;
import com.journeyapps.barcodescanner.ScanIntentResult;
import com.journeyapps.barcodescanner.ScanOptions;

import lk.sliit.solarmicrogrid.R;
import lk.sliit.solarmicrogrid.data.repository.AuthRepository;
import lk.sliit.solarmicrogrid.data.repository.BookingRepository;
import lk.sliit.solarmicrogrid.model.Reservation;
import lk.sliit.solarmicrogrid.model.Session;
import lk.sliit.solarmicrogrid.ui.operator.ScanResultActivity;

public final class OperatorHomeActivity extends RoleHomeActivity {

    /**
     * Set by the transfer summary screen when the operator taps Scan
     * another, so the camera opens as soon as this screen is back.
     */
    public static final String EXTRA_START_SCAN = "start_scan";

    private BookingRepository bookingRepository;

    /**
     * Opens the camera and hands back what it read.
     *
     * Registered as a field, not inside a method. Android needs this ready
     * before the screen starts, or it throws when the camera returns.
     */
    private final ActivityResultLauncher<ScanOptions> scanLauncher =
            registerForActivityResult(new ScanContract(), this::onScanned);

    @StringRes
    @Override
    protected int titleResource() {
        return R.string.home_operator_title;
    }

    /**
     * Opens the camera again when the summary screen asked for it.
     *
     * onNewIntent, not onCreate. The summary screen returns here with
     * CLEAR_TOP, which reuses this screen rather than building a new one, so
     * onCreate does not run again.
     */
    @Override
    protected void onNewIntent(@NonNull Intent intent) {
        super.onNewIntent(intent);
        setIntent(intent);

        if (intent.getBooleanExtra(EXTRA_START_SCAN, false)) {
            startScan();
        }
    }

    @Override
    protected void showSession(@NonNull Session session) {
        super.showSession(session);

        if (bookingRepository == null) {
            bookingRepository = BookingRepository.create();
        }

        // An operator does not book energy, so My bookings would always be
        // empty for them. The scan card takes that slot instead.
        binding.btnMyBookings.setVisibility(View.GONE);
        binding.btnScanQr.setVisibility(View.VISIBLE);
        binding.btnScanQr.setOnClickListener(view -> startScan());
    }

    private void startScan() {

        ScanOptions options = new ScanOptions();
        options.setDesiredBarcodeFormats(ScanOptions.QR_CODE);
        options.setPrompt(getString(R.string.scan_prompt));
        options.setBeepEnabled(true);

        // Portrait locked. The scanner rotating while an operator holds the
        // phone over a counter is more annoying than helpful.
        options.setOrientationLocked(true);

        scanLauncher.launch(options);
    }

    /**
     * Called with whatever the camera read.
     *
     * The text is sent to the server without being looked at. Task E4.
     */
    private void onScanned(@Nullable ScanIntentResult result) {

        if (result == null || result.getContents() == null) {
            // The operator pressed Back, or the camera was refused.
            Toast.makeText(this, R.string.scan_cancelled, Toast.LENGTH_SHORT).show();
            return;
        }

        verifyWithServer(result.getContents());
    }

    private void verifyWithServer(@NonNull String scannedToken) {

        bookingRepository.verifyQr(scannedToken, new AuthRepository.Callback<Reservation>() {

            @Override
            public void onSuccess(@Nullable Reservation booking) {
                if (isFinishing() || isDestroyed() || booking == null) {
                    return;
                }
                startActivity(ScanResultActivity.intentFor(
                        OperatorHomeActivity.this, booking));
            }

            @Override
            public void onFailure(@NonNull String message) {
                if (isFinishing() || isDestroyed()) {
                    return;
                }
                // Reached when no booking has that code, or the booking is
                // cancelled or already finished. The server explains which.
                Toast.makeText(OperatorHomeActivity.this, message, Toast.LENGTH_LONG).show();
            }
        });
    }
}
