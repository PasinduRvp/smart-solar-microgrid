/*
 * ---------------------------------------------------------------------------
 * File        : NewBookingActivity.java
 * Project     : Smart Solar Microgrid Trading System - SE4040 Assignment 1
 * Author      : L K P Yasith (IT 23380196)
 * Created     : 2026-09-20
 * Description : Book a window, or change a booking.
 *               Tasks D1, D2 and D3.
 *
 * One screen, two jobs
 *              Booking a new window and changing an old one ask for exactly
 *              the same things: a day, a window, an amount of energy and a
 *              direction. Two screens would be the same file twice.
 *              Passing a booking id turns it into the change screen.
 *
 * Business rule BR-1 on screen
 *              Only the next 7 days are offered. A day the server would
 *              refuse can never be picked.
 *
 * Business rule BR-9 belongs to the server
 *              A window can fill up between opening this screen and tapping
 *              Confirm. The server answers 409 in that case, and its message
 *              is shown. The app does not try to hold a place.
 *
 * SOLID       Single Responsibility. It collects a booking and reports what
 *              happened. No HTTP and no SQL. BookingRepository does that.
 * ---------------------------------------------------------------------------
 */
package lk.sliit.solarmicrogrid.ui.booking;

import android.content.Context;
import android.content.Intent;
import android.os.Bundle;
import android.view.View;
import android.widget.Toast;

import androidx.annotation.NonNull;
import androidx.annotation.Nullable;
import androidx.recyclerview.widget.LinearLayoutManager;

import com.google.android.material.chip.Chip;

import java.util.Date;
import java.util.List;

import lk.sliit.solarmicrogrid.R;
import lk.sliit.solarmicrogrid.data.repository.AuthRepository;
import lk.sliit.solarmicrogrid.data.repository.BookingRepository;
import lk.sliit.solarmicrogrid.databinding.ActivityNewBookingBinding;
import lk.sliit.solarmicrogrid.model.EnergySlot;
import lk.sliit.solarmicrogrid.model.Reservation;
import lk.sliit.solarmicrogrid.ui.BaseActivity;
import lk.sliit.solarmicrogrid.util.DateTimes;
import lk.sliit.solarmicrogrid.util.TextFormat;
import lk.sliit.solarmicrogrid.util.Validators;

public final class NewBookingActivity extends BaseActivity {

    public static final String EXTRA_STATION_ID = "station_id";
    public static final String EXTRA_STATION_NAME = "station_name";

    /** Present only when an existing booking is being changed. */
    public static final String EXTRA_BOOKING_ID = "booking_id";
    public static final String EXTRA_BOOKING_SLOT_ID = "booking_slot_id";
    public static final String EXTRA_BOOKING_ENERGY = "booking_energy";
    public static final String EXTRA_BOOKING_DIRECTION = "booking_direction";

    /** Business rule BR-1. A booking must be inside the next 7 days. */
    private static final int BOOKING_WINDOW_DAYS = 7;

    /** Opens the screen to make a new booking at this node. */
    @NonNull
    public static Intent bookIntent(@NonNull Context context,
                                    @NonNull String stationId,
                                    @NonNull String stationName) {
        Intent intent = new Intent(context, NewBookingActivity.class);
        intent.putExtra(EXTRA_STATION_ID, stationId);
        intent.putExtra(EXTRA_STATION_NAME, stationName);
        return intent;
    }

    /** Opens the same screen to change a booking that already exists. */
    @NonNull
    public static Intent changeIntent(@NonNull Context context, @NonNull Reservation booking) {
        Intent intent = new Intent(context, NewBookingActivity.class);
        intent.putExtra(EXTRA_STATION_ID, booking.getStationId());
        intent.putExtra(EXTRA_STATION_NAME, booking.getStationName());
        intent.putExtra(EXTRA_BOOKING_ID, booking.getId());
        intent.putExtra(EXTRA_BOOKING_SLOT_ID, booking.getSlotId());
        intent.putExtra(EXTRA_BOOKING_ENERGY, booking.getEnergyKwh());
        intent.putExtra(EXTRA_BOOKING_DIRECTION, booking.getDirection());
        return intent;
    }

    private ActivityNewBookingBinding binding;
    private BookingRepository bookingRepository;
    private SlotAdapter slotAdapter;

    private String stationId;

    /** Null when making a new booking. Set when changing one. */
    @Nullable
    private String bookingId;

    /** The window the booking already has, so it starts off chosen. */
    @Nullable
    private String existingSlotId;

    private Date[] days;

    @Override
    protected void onCreate(@Nullable Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);

        binding = ActivityNewBookingBinding.inflate(getLayoutInflater());
        setContentView(binding.getRoot());
        applySystemBarInsets(binding.root);

        stationId = getIntent().getStringExtra(EXTRA_STATION_ID);
        if (stationId == null || stationId.trim().isEmpty()) {
            // Nothing can be booked without a node. This should not happen.
            finish();
            return;
        }

        bookingId = getIntent().getStringExtra(EXTRA_BOOKING_ID);
        existingSlotId = getIntent().getStringExtra(EXTRA_BOOKING_SLOT_ID);
        bookingRepository = BookingRepository.create();

        setUpScreen();
        buildDayChips();
        showExistingValues();
    }

    private void setUpScreen() {

        boolean changing = bookingId != null;

        binding.toolbar.setTitle(changing
                ? R.string.booking_edit_title
                : R.string.booking_new_title);
        binding.toolbar.setNavigationOnClickListener(view -> finish());

        binding.btnConfirm.setText(changing
                ? R.string.action_save_booking
                : R.string.action_confirm_booking);
        binding.btnConfirm.setOnClickListener(view -> confirm());

        binding.tvNodeName.setText(getIntent().getStringExtra(EXTRA_STATION_NAME));

        slotAdapter = new SlotAdapter();
        binding.listSlots.setLayoutManager(new LinearLayoutManager(this));
        binding.listSlots.setAdapter(slotAdapter);

        // Deliver is the common case: a prosumer with panels usually has
        // energy to give. The user can still switch to Draw.
        binding.toggleDirection.check(R.id.btnDeliver);
    }

    /**
     * Builds one chip per day, for the next 7 days only.
     *
     * The chips are made in code rather than written in the layout, because
     * the dates change every day. Seven fixed chips would be wrong tomorrow.
     */
    private void buildDayChips() {

        days = DateTimes.nextDays(BOOKING_WINDOW_DAYS);

        for (int index = 0; index < days.length; index++) {

            Chip chip = new Chip(this);
            chip.setText(getString(R.string.booking_day_chip,
                    DateTimes.showDayName(days[index]),
                    DateTimes.showDayOfMonth(days[index])));
            chip.setCheckable(true);
            chip.setId(View.generateViewId());
            chip.setTag(index);

            binding.chipsDays.addView(chip);
        }

        binding.chipsDays.setOnCheckedStateChangeListener((group, checkedIds) -> {
            if (checkedIds.isEmpty()) {
                return;
            }
            Chip chosen = group.findViewById(checkedIds.get(0));
            if (chosen != null) {
                loadSlotsFor(days[(int) chosen.getTag()]);
            }
        });

        // Start on today, so the screen has something to show at once.
        if (binding.chipsDays.getChildCount() > 0) {
            ((Chip) binding.chipsDays.getChildAt(0)).setChecked(true);
        }
    }

    /** When changing a booking, the old energy and direction are filled in. */
    private void showExistingValues() {

        if (bookingId == null) {
            return;
        }

        double energy = getIntent().getDoubleExtra(EXTRA_BOOKING_ENERGY, 0d);
        if (binding.tilEnergy.getEditText() != null) {
            binding.tilEnergy.getEditText().setText(TextFormat.capacity(energy));
        }

        String direction = getIntent().getStringExtra(EXTRA_BOOKING_DIRECTION);
        binding.toggleDirection.check(Reservation.DIRECTION_DRAW.equals(direction)
                ? R.id.btnDraw
                : R.id.btnDeliver);
    }

    // ----- Loading the windows ----------------------------------------------

    private void loadSlotsFor(@NonNull Date day) {

        showBusy(true);
        binding.tvNoWindows.setVisibility(View.GONE);
        binding.listSlots.setVisibility(View.VISIBLE);

        bookingRepository.loadSlots(stationId, day, new AuthRepository.Callback<List<EnergySlot>>() {

            @Override
            public void onSuccess(@Nullable List<EnergySlot> slots) {
                if (isGone() || slots == null) {
                    return;
                }
                showBusy(false);
                slotAdapter.replaceAll(slots);

                // The window the booking already has starts off chosen, so
                // the user can change only the energy if they want to.
                slotAdapter.chooseById(existingSlotId);

                // One or the other, never both. The list is hidden as well
                // as emptied, or its 220dp box would leave a hole above the
                // message.
                boolean noWindows = slots.isEmpty();

                binding.listSlots.setVisibility(noWindows ? View.GONE : View.VISIBLE);
                binding.tvNoWindows.setVisibility(noWindows ? View.VISIBLE : View.GONE);

                if (noWindows) {
                    // Naming the day matters. The user taps through seven
                    // chips, so "that day" does not tell them which one.
                    binding.tvNoWindows.setText(getString(
                            R.string.booking_no_windows,
                            getString(R.string.booking_day_inline,
                                    DateTimes.showDayName(day),
                                    DateTimes.showDayNumber(day))));
                }
            }

            @Override
            public void onFailure(@NonNull String message) {
                if (isGone()) {
                    return;
                }
                showBusy(false);
                Toast.makeText(NewBookingActivity.this, message, Toast.LENGTH_LONG).show();
            }
        });
    }

    // ----- Booking ----------------------------------------------------------

    private void confirm() {

        EnergySlot chosen = slotAdapter.getChosen();
        if (chosen == null) {
            Toast.makeText(this, R.string.booking_error_pick_window, Toast.LENGTH_LONG).show();
            return;
        }

        String energyText = energyText();
        if (!Validators.isValidSolarCapacity(energyText)) {
            binding.tilEnergy.setError(getString(R.string.booking_error_energy));
            return;
        }
        binding.tilEnergy.setError(null);

        Double energy = Validators.parseDoubleOrNull(energyText);
        if (energy == null) {
            binding.tilEnergy.setError(getString(R.string.booking_error_energy));
            return;
        }

        String direction = binding.toggleDirection.getCheckedButtonId() == R.id.btnDraw
                ? Reservation.DIRECTION_DRAW
                : Reservation.DIRECTION_DELIVER;

        showBusy(true);

        if (bookingId == null) {
            bookingRepository.createBooking(chosen.getId(), energy, direction,
                    finishWith(BookingSummaryActivity.ACTION_CREATED));
        } else {
            bookingRepository.updateBooking(bookingId, chosen.getId(), energy, direction,
                    finishWith(BookingSummaryActivity.ACTION_UPDATED));
        }
    }

    /**
     * On success, opens the summary screen and closes this one.
     *
     * This screen is closed so that Back from the summary returns to where
     * the user started, not to a form they have already submitted.
     */
    @NonNull
    private AuthRepository.Callback<Reservation> finishWith(@NonNull String action) {

        return new AuthRepository.Callback<Reservation>() {

            @Override
            public void onSuccess(@Nullable Reservation booking) {
                if (isGone() || booking == null) {
                    return;
                }
                showBusy(false);
                startActivity(BookingSummaryActivity.intentFor(
                        NewBookingActivity.this, action, booking));
                finish();
            }

            @Override
            public void onFailure(@NonNull String message) {
                if (isGone()) {
                    return;
                }
                showBusy(false);

                // The screen stays open, so the user can pick another window
                // when the server says this one just filled up.
                Toast.makeText(NewBookingActivity.this, message, Toast.LENGTH_LONG).show();
            }
        };
    }

    // ----- Helpers ----------------------------------------------------------

    @NonNull
    private String energyText() {
        return binding.tilEnergy.getEditText() == null
                ? ""
                : binding.tilEnergy.getEditText().getText().toString().trim();
    }

    private void showBusy(boolean busy) {
        binding.progress.setVisibility(busy ? View.VISIBLE : View.GONE);
        binding.btnConfirm.setEnabled(!busy);
    }

    /** True when the screen is closing. A late reply must then be dropped. */
    private boolean isGone() {
        return isFinishing() || isDestroyed();
    }
}
