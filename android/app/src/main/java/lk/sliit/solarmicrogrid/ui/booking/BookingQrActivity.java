/*
 * ---------------------------------------------------------------------------
 * File        : BookingQrActivity.java
 * Project     : Smart Solar Microgrid Trading System - SE4040 Assignment 1
 * Author      : L K P Yasith (IT 23380196)
 * Created     : 2026-09-20
 * Description : Shows the transaction QR code of an approved booking.
 *               Task E3.
 *
 * The token comes from the server
 *              The app asks for it, then draws it. It never makes one up.
 *              The server only issues a token once a Grid Operator has
 *              approved the booking, which is business rule BR-7. A pending
 *              booking gets a 409 and its message is shown.
 *
 * Why the picture is drawn after layout
 *              The code is drawn to fill the width of the card, so the width
 *              has to be known first. In onCreate it is still 0.
 *              The drawing is posted until after the first layout pass.
 *
 * The screen brightness is raised
 *              A dim screen behind a scratched screen protector is a common
 *              reason a code will not scan. The brightness is put back when
 *              the screen closes, so it is not left turned up.
 *
 * SOLID       Single Responsibility. It fetches one token and draws it.
 * ---------------------------------------------------------------------------
 */
package lk.sliit.solarmicrogrid.ui.booking;

import android.content.Context;
import android.content.Intent;
import android.graphics.Bitmap;
import android.os.Bundle;
import android.view.View;
import android.view.WindowManager;
import android.widget.Toast;

import androidx.annotation.NonNull;
import androidx.annotation.Nullable;

import lk.sliit.solarmicrogrid.R;
import lk.sliit.solarmicrogrid.data.repository.AuthRepository;
import lk.sliit.solarmicrogrid.data.repository.BookingRepository;
import lk.sliit.solarmicrogrid.databinding.ActivityBookingQrBinding;
import lk.sliit.solarmicrogrid.model.QrToken;
import lk.sliit.solarmicrogrid.ui.BaseActivity;
import lk.sliit.solarmicrogrid.util.DateTimes;
import lk.sliit.solarmicrogrid.util.QrImages;

public final class BookingQrActivity extends BaseActivity {

    private static final String EXTRA_BOOKING_ID = "booking_id";

    /** Full brightness while the code is on screen. */
    private static final float FULL_BRIGHTNESS = 1f;

    @NonNull
    public static Intent intentFor(@NonNull Context context, @NonNull String bookingId) {
        Intent intent = new Intent(context, BookingQrActivity.class);
        intent.putExtra(EXTRA_BOOKING_ID, bookingId);
        return intent;
    }

    private ActivityBookingQrBinding binding;
    private BookingRepository bookingRepository;

    @Override
    protected void onCreate(@Nullable Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);

        binding = ActivityBookingQrBinding.inflate(getLayoutInflater());
        setContentView(binding.getRoot());
        applySystemBarInsets(binding.root);

        String bookingId = getIntent().getStringExtra(EXTRA_BOOKING_ID);
        if (bookingId == null || bookingId.trim().isEmpty()) {
            finish();
            return;
        }

        bookingRepository = BookingRepository.create();
        binding.toolbar.setNavigationOnClickListener(view -> finish());

        turnBrightnessUp();
        loadToken(bookingId);
    }

    /**
     * Raises the screen brightness for this screen only.
     *
     * Android puts it back by itself when the screen closes, because the
     * value is set on this window and not on the phone.
     */
    private void turnBrightnessUp() {
        WindowManager.LayoutParams settings = getWindow().getAttributes();
        settings.screenBrightness = FULL_BRIGHTNESS;
        getWindow().setAttributes(settings);
    }

    private void loadToken(@NonNull String bookingId) {

        binding.progress.setVisibility(View.VISIBLE);

        bookingRepository.loadQrToken(bookingId, new AuthRepository.Callback<QrToken>() {

            @Override
            public void onSuccess(@Nullable QrToken token) {
                if (isGone() || token == null) {
                    return;
                }
                binding.progress.setVisibility(View.GONE);
                show(token);
            }

            @Override
            public void onFailure(@NonNull String message) {
                if (isGone()) {
                    return;
                }
                binding.progress.setVisibility(View.GONE);

                // Reached when the booking is not approved yet. The server
                // explains that in its own words, so the message is shown
                // and the screen closes.
                Toast.makeText(BookingQrActivity.this, message, Toast.LENGTH_LONG).show();
                finish();
            }
        });
    }

    private void show(@NonNull QrToken token) {

        binding.tvNode.setText(token.getStationName());
        binding.tvWhen.setText(DateTimes.showDateAndTime(token.getReservationDateTime()));
        binding.tvNumber.setText(token.getReservationNo());

        drawWhenSizeIsKnown(token.getQrToken());
    }

    /**
     * Draws the code once the view has a width.
     *
     * In onCreate the width is still 0, and a code cannot be drawn into no
     * space at all. post() runs this again after the first layout pass.
     */
    private void drawWhenSizeIsKnown(@NonNull String tokenText) {

        int width = binding.imgQr.getWidth();

        if (width == 0) {
            binding.imgQr.post(() -> {
                if (!isGone()) {
                    drawWhenSizeIsKnown(tokenText);
                }
            });
            return;
        }

        Bitmap picture = QrImages.draw(tokenText, width);

        if (picture == null) {
            Toast.makeText(this, R.string.qr_failed, Toast.LENGTH_LONG).show();
            return;
        }
        binding.imgQr.setImageBitmap(picture);
    }

    /** True when the screen is closing. A late reply must then be dropped. */
    private boolean isGone() {
        return isFinishing() || isDestroyed();
    }
}
