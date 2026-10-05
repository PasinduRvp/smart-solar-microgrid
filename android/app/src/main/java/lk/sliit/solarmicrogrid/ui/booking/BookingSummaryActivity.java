/*
 * ---------------------------------------------------------------------------
 * File        : BookingSummaryActivity.java
 * Project     : Smart Solar Microgrid Trading System - SE4040 Assignment 1
 * Author      : L K P Yasith (IT 23380196)
 * Created     : 2026-09-20
 * Description : Shown after a booking is made, changed or cancelled.
 *               Task D5.
 *
 * Why a whole screen
 *              The assignment asks for a summary after each action, and it
 *              carries its own marks. A toast disappears in two seconds and
 *              cannot be read again.
 *
 *              This screen says what happened, shows exactly what is saved,
 *              and waits for Done. The user can check the date and the time
 *              before leaving.
 *
 * It shows the server reply, not what was typed
 *              The booking comes from the API response. So the number, the
 *              status and the time are what the server really stored. If the
 *              server changed anything, the user sees the real value.
 *
 * SOLID       Single Responsibility. It shows one booking. No HTTP, no SQL.
 * ---------------------------------------------------------------------------
 */
package lk.sliit.solarmicrogrid.ui.booking;

import android.content.Context;
import android.content.Intent;
import android.os.Bundle;
import android.view.View;

import androidx.activity.OnBackPressedCallback;
import androidx.annotation.ColorRes;
import androidx.annotation.NonNull;
import androidx.annotation.Nullable;
import androidx.annotation.StringRes;
import androidx.core.content.ContextCompat;

import lk.sliit.solarmicrogrid.R;
import lk.sliit.solarmicrogrid.databinding.ActivityBookingSummaryBinding;
import lk.sliit.solarmicrogrid.model.Reservation;
import lk.sliit.solarmicrogrid.ui.BaseActivity;
import lk.sliit.solarmicrogrid.util.DateTimes;
import lk.sliit.solarmicrogrid.util.TextFormat;

public final class BookingSummaryActivity extends BaseActivity {

    /** Which of the three actions just happened. */
    public static final String EXTRA_ACTION = "action";
    public static final String ACTION_CREATED = "created";
    public static final String ACTION_UPDATED = "updated";
    public static final String ACTION_CANCELLED = "cancelled";

    private static final String EXTRA_NUMBER = "number";
    private static final String EXTRA_NODE = "node";
    private static final String EXTRA_WHEN = "when";
    private static final String EXTRA_ENERGY = "energy";
    private static final String EXTRA_DIRECTION = "direction";
    private static final String EXTRA_STATUS = "status";
    private static final String EXTRA_REASON = "reason";

    /**
     * Builds the intent that opens this screen.
     *
     * The booking is broken into plain values here rather than passed as one
     * object. Reservation would otherwise have to be made Serializable only
     * so it could travel between two screens, which is a lot of ceremony for
     * seven fields.
     */
    @NonNull
    public static Intent intentFor(@NonNull Context context,
                                   @NonNull String action,
                                   @NonNull Reservation booking) {

        Intent intent = new Intent(context, BookingSummaryActivity.class);
        intent.putExtra(EXTRA_ACTION, action);
        intent.putExtra(EXTRA_NUMBER, booking.getReservationNo());
        intent.putExtra(EXTRA_NODE, booking.getStationName());
        intent.putExtra(EXTRA_WHEN, booking.getReservationDateTime());
        intent.putExtra(EXTRA_ENERGY, booking.getEnergyKwh());
        intent.putExtra(EXTRA_DIRECTION, booking.getDirection());
        intent.putExtra(EXTRA_STATUS, booking.getStatus());
        intent.putExtra(EXTRA_REASON, booking.getCancelReason());
        return intent;
    }

    private ActivityBookingSummaryBinding binding;

    @Override
    protected void onCreate(@Nullable Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);

        binding = ActivityBookingSummaryBinding.inflate(getLayoutInflater());
        setContentView(binding.getRoot());
        applySystemBarInsets(binding.root);

        binding.btnDone.setOnClickListener(view -> goToMyBookings());

        // Back does the same as Done. This screen is the end of an action,
        // so both ways out should land in the same place.
        getOnBackPressedDispatcher().addCallback(this, new OnBackPressedCallback(true) {
            @Override
            public void handleOnBackPressed() {
                goToMyBookings();
            }
        });

        showHeadline(getIntent().getStringExtra(EXTRA_ACTION));
        showDetails();
    }

    /**
     * Done always lands on My bookings.
     *
     * Without this, Done would simply close the screen, and the user would
     * end up back where they started. After booking from a node that is the
     * node detail screen, which is not where a new booking can be seen.
     *
     * CLEAR_TOP with SINGLE_TOP reuses the My bookings screen when it is
     * already open, instead of stacking a second copy of it. That screen
     * reloads in onStart, so the list shows the change straight away.
     */
    private void goToMyBookings() {
        Intent intent = new Intent(this, MyBookingsActivity.class);
        intent.addFlags(Intent.FLAG_ACTIVITY_CLEAR_TOP | Intent.FLAG_ACTIVITY_SINGLE_TOP);
        startActivity(intent);
        finish();
    }

    /** The heading, the note and the colour depend on what just happened. */
    private void showHeadline(@Nullable String action) {

        @StringRes int headline;
        @StringRes int note;
        @ColorRes int colour;

        if (ACTION_CANCELLED.equals(action)) {
            headline = R.string.summary_cancelled;
            note = R.string.summary_cancelled_note;
            colour = R.color.status_danger;
        } else if (ACTION_UPDATED.equals(action)) {
            headline = R.string.summary_updated;
            note = R.string.summary_updated_note;
            colour = R.color.status_success;
        } else {
            headline = R.string.summary_created;
            note = R.string.summary_created_note;
            colour = R.color.status_success;
        }

        binding.tvHeadline.setText(headline);
        binding.tvHeadline.setTextColor(ContextCompat.getColor(this, colour));
        binding.tvNote.setText(note);
    }

    private void showDetails() {

        Intent from = getIntent();

        binding.tvNumber.setText(from.getStringExtra(EXTRA_NUMBER));
        binding.tvNode.setText(from.getStringExtra(EXTRA_NODE));

        // The API sends UTC. This shows it in the time zone of the phone, so
        // a booking made for 08:00 reads as 08:00.
        binding.tvWhen.setText(DateTimes.showDateAndTime(from.getStringExtra(EXTRA_WHEN)));

        binding.tvEnergy.setText(getString(
                R.string.bookings_energy_value,
                TextFormat.capacity(from.getDoubleExtra(EXTRA_ENERGY, 0d))));

        binding.tvDirection.setText(directionText(from.getStringExtra(EXTRA_DIRECTION)));
        binding.tvStatus.setText(from.getStringExtra(EXTRA_STATUS));

        showReasonIfCancelled(from.getStringExtra(EXTRA_REASON));
    }

    /** The reason only exists after a cancellation, so the row is hidden otherwise. */
    private void showReasonIfCancelled(@Nullable String reason) {
        boolean hasReason = reason != null && !reason.trim().isEmpty();
        binding.groupReason.setVisibility(hasReason ? View.VISIBLE : View.GONE);
        if (hasReason) {
            binding.tvReason.setText(reason);
        }
    }

    /** Turns Deliver or Draw into words a user reads rather than a code. */
    @NonNull
    private String directionText(@Nullable String direction) {
        return Reservation.DIRECTION_DELIVER.equals(direction)
                ? getString(R.string.booking_deliver)
                : getString(R.string.booking_draw);
    }
}
