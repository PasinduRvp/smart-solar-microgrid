/*
 * ---------------------------------------------------------------------------
 * File        : MyBookingsActivity.java
 * Project     : Smart Solar Microgrid Trading System - SE4040 Assignment 1
 * Author      : L K P Yasith (IT 23380196)
 * Created     : 2026-09-20
 * Description : Every booking of the signed in prosumer. It is the way in to
 *               changing and cancelling. Tasks D3 and D4.
 *
 * Reloaded every time it comes to the front
 *              onStart, not onCreate. Coming back from the change screen or
 *              the summary screen then shows the new state at once, instead
 *              of a list that still says what it said before.
 *
 * Nothing is cached
 *              A booking that could be changed an hour ago may be inside the
 *              12 hour window now. Showing a saved copy would offer buttons
 *              the server would refuse.
 *
 * Business rules BR-2 and BR-3
 *              The Change and Cancel buttons follow the flags the server
 *              sends. The server checks again when the call is made, so a
 *              screen left open does not let anybody past the rule.
 *
 * SOLID       Single Responsibility. It lists bookings and starts actions.
 *              No HTTP and no SQL. BookingRepository does that.
 * ---------------------------------------------------------------------------
 */
package lk.sliit.solarmicrogrid.ui.booking;

import android.os.Bundle;
import android.text.Editable;
import android.text.TextWatcher;
import android.view.LayoutInflater;
import android.view.View;
import android.widget.Toast;

import androidx.annotation.NonNull;
import androidx.annotation.Nullable;
import androidx.appcompat.app.AlertDialog;
import androidx.recyclerview.widget.LinearLayoutManager;

import com.google.android.material.chip.Chip;

import java.util.ArrayList;
import java.util.List;
import java.util.Locale;

import lk.sliit.solarmicrogrid.R;
import lk.sliit.solarmicrogrid.SolarApp;
import lk.sliit.solarmicrogrid.data.repository.AuthRepository;
import lk.sliit.solarmicrogrid.data.repository.BookingRepository;
import lk.sliit.solarmicrogrid.databinding.ActivityMyBookingsBinding;
import lk.sliit.solarmicrogrid.databinding.DialogCancelBookingBinding;
import lk.sliit.solarmicrogrid.model.Reservation;
import lk.sliit.solarmicrogrid.model.Session;
import lk.sliit.solarmicrogrid.ui.BaseActivity;
import lk.sliit.solarmicrogrid.ui.Navigation;

public final class MyBookingsActivity extends BaseActivity
        implements BookingAdapter.OnBookingAction {

    /** The chip that means do not filter by status. */
    private static final String STATUS_ALL = "";

    private ActivityMyBookingsBinding binding;
    private BookingRepository bookingRepository;
    private BookingAdapter adapter;
    private String nic;

    /** Every booking downloaded. The list on screen is filtered from this. */
    private final List<Reservation> allBookings = new ArrayList<>();

    /** The status chip currently chosen. Empty means All. */
    private String statusFilter = STATUS_ALL;

    @Override
    protected void onCreate(@Nullable Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);

        binding = ActivityMyBookingsBinding.inflate(getLayoutInflater());
        setContentView(binding.getRoot());
        applySystemBarInsets(binding.root);

        Session session = SolarApp.get().sessions().read();
        if (session == null) {
            Navigation.toLogin(this);
            return;
        }

        nic = session.getNic();
        bookingRepository = BookingRepository.create();

        adapter = new BookingAdapter(this);
        binding.list.setLayoutManager(new LinearLayoutManager(this));
        binding.list.setAdapter(adapter);

        binding.toolbar.setNavigationOnClickListener(view -> finish());
        binding.refresh.setOnRefreshListener(this::loadBookings);

        buildStatusChips();
        watchSearchBox();
    }

    // ----- Search and filter. Task E2 ---------------------------------------

    /**
     * Builds one chip per status, plus All.
     *
     * The chips are made in code so the status names come from one place -
     * the Reservation constants that match the Web API - instead of being
     * typed again into the layout.
     */
    private void buildStatusChips() {

        addStatusChip(getString(R.string.bookings_filter_all), STATUS_ALL);
        addStatusChip(Reservation.STATUS_PENDING, Reservation.STATUS_PENDING);
        addStatusChip(Reservation.STATUS_APPROVED, Reservation.STATUS_APPROVED);
        addStatusChip(Reservation.STATUS_COMPLETED, Reservation.STATUS_COMPLETED);
        addStatusChip(Reservation.STATUS_CANCELLED, Reservation.STATUS_CANCELLED);

        binding.chipsStatus.setOnCheckedStateChangeListener((group, checkedIds) -> {
            if (checkedIds.isEmpty()) {
                return;
            }
            Chip chosen = group.findViewById(checkedIds.get(0));
            if (chosen != null) {
                statusFilter = (String) chosen.getTag();
                showFiltered();
            }
        });

        // Start on All, so every booking is visible when the screen opens.
        ((Chip) binding.chipsStatus.getChildAt(0)).setChecked(true);
    }

    private void addStatusChip(@NonNull String label, @NonNull String status) {
        Chip chip = new Chip(this);
        chip.setText(label);
        chip.setCheckable(true);
        chip.setId(View.generateViewId());
        chip.setTag(status);
        binding.chipsStatus.addView(chip);
    }

    /** Filters as the user types. No button to press. */
    private void watchSearchBox() {

        if (binding.tilSearch.getEditText() == null) {
            return;
        }

        binding.tilSearch.getEditText().addTextChangedListener(new TextWatcher() {

            @Override
            public void beforeTextChanged(CharSequence s, int start, int count, int after) {
                // Nothing to do before the change.
            }

            @Override
            public void onTextChanged(CharSequence s, int start, int before, int count) {
                showFiltered();
            }

            @Override
            public void afterTextChanged(Editable s) {
                // Nothing to do after the change.
            }
        });
    }

    /**
     * Applies the search text and the status chip to the downloaded list.
     *
     * Both are applied together, so searching inside a status works the way
     * a user expects.
     */
    private void showFiltered() {

        String search = searchText();
        List<Reservation> matching = new ArrayList<>();

        for (Reservation booking : allBookings) {
            if (matchesStatus(booking) && matchesSearch(booking, search)) {
                matching.add(booking);
            }
        }

        adapter.replaceAll(matching);
        showEmptyMessage(matching.isEmpty());
    }

    private boolean matchesStatus(@NonNull Reservation booking) {
        return STATUS_ALL.equals(statusFilter) || statusFilter.equals(booking.getStatus());
    }

    /**
     * Matches the node name or the booking number.
     *
     * Both are lowered before comparing, so a search for galle finds Galle
     * Fort Solar Node. Locale.ROOT keeps that the same in every language.
     */
    private boolean matchesSearch(@NonNull Reservation booking, @NonNull String search) {

        if (search.isEmpty()) {
            return true;
        }

        return booking.getStationName().toLowerCase(Locale.ROOT).contains(search)
                || booking.getReservationNo().toLowerCase(Locale.ROOT).contains(search);
    }

    @NonNull
    private String searchText() {
        return binding.tilSearch.getEditText() == null
                ? ""
                : binding.tilSearch.getEditText().getText().toString().trim().toLowerCase(Locale.ROOT);
    }

    /**
     * Two different empty messages.
     *
     * No bookings at all is not the same as none matching a search, and
     * telling the user to go and book something when they have twelve
     * bookings would be wrong.
     */
    private void showEmptyMessage(boolean nothingToShow) {

        binding.tvEmpty.setVisibility(nothingToShow ? View.VISIBLE : View.GONE);

        if (nothingToShow) {
            binding.tvEmpty.setText(allBookings.isEmpty()
                    ? R.string.bookings_empty
                    : R.string.bookings_none_match);
        }
    }

    /**
     * Loads in onStart, not onCreate.
     *
     * Returning from the change screen or the summary screen then shows the
     * new state straight away.
     */
    @Override
    protected void onStart() {
        super.onStart();
        loadBookings();
    }

    private void loadBookings() {

        showBusy(true);

        bookingRepository.loadMyBookings(nic, new AuthRepository.Callback<List<Reservation>>() {

            @Override
            public void onSuccess(@Nullable List<Reservation> bookings) {
                if (isGone() || bookings == null) {
                    return;
                }
                showBusy(false);
                binding.refresh.setRefreshing(false);

                allBookings.clear();
                allBookings.addAll(bookings);

                // The search text and the chip are kept, so a reload does
                // not throw away what the user was looking at.
                showFiltered();
            }

            @Override
            public void onFailure(@NonNull String message) {
                if (isGone()) {
                    return;
                }
                showBusy(false);
                binding.refresh.setRefreshing(false);
                Toast.makeText(MyBookingsActivity.this, message, Toast.LENGTH_LONG).show();
            }
        });
    }

    // ----- Changing a booking. Task D3 --------------------------------------

    @Override
    public void onChangeTapped(@NonNull Reservation booking) {
        startActivity(NewBookingActivity.changeIntent(this, booking));
    }

    // ----- Showing the QR code. Task E3 -------------------------------------

    @Override
    public void onShowQrTapped(@NonNull Reservation booking) {
        startActivity(BookingQrActivity.intentFor(this, booking.getId()));
    }

    // ----- Cancelling a booking. Task D4 ------------------------------------

    /**
     * Asks for a reason before cancelling.
     *
     * The server will not accept a cancellation without one, so asking here
     * saves a refused call. It also means the operator is told why.
     */
    @Override
    public void onCancelTapped(@NonNull Reservation booking) {

        DialogCancelBookingBinding dialogBinding =
                DialogCancelBookingBinding.inflate(LayoutInflater.from(this));

        AlertDialog dialog = new AlertDialog.Builder(this)
                .setTitle(R.string.cancel_title)
                .setMessage(R.string.cancel_message)
                .setView(dialogBinding.getRoot())
                .setNegativeButton(R.string.cancel_keep, null)
                .setPositiveButton(R.string.cancel_confirm, null)
                .create();

        dialog.show();

        // The listener is set after show() on purpose. Set the usual way, the
        // dialog closes on every tap, even when the reason box is empty.
        dialog.getButton(AlertDialog.BUTTON_POSITIVE).setOnClickListener(view -> {

            String reason = reasonFrom(dialogBinding);

            if (reason.isEmpty()) {
                dialogBinding.tilReason.setError(getString(R.string.cancel_error_reason));
                return;
            }

            dialog.dismiss();
            cancelBooking(booking, reason);
        });
    }

    private void cancelBooking(@NonNull Reservation booking, @NonNull String reason) {

        showBusy(true);

        bookingRepository.cancelBooking(booking.getId(), reason,
                new AuthRepository.Callback<Reservation>() {

                    @Override
                    public void onSuccess(@Nullable Reservation cancelled) {
                        if (isGone() || cancelled == null) {
                            return;
                        }
                        showBusy(false);

                        // The summary screen states what happened. Task D5.
                        startActivity(BookingSummaryActivity.intentFor(
                                MyBookingsActivity.this,
                                BookingSummaryActivity.ACTION_CANCELLED,
                                cancelled));
                    }

                    @Override
                    public void onFailure(@NonNull String message) {
                        if (isGone()) {
                            return;
                        }
                        showBusy(false);

                        // Reached when the booking moved inside the 12 hour
                        // window while this screen was open. The server
                        // explains it in its own words.
                        Toast.makeText(MyBookingsActivity.this, message, Toast.LENGTH_LONG).show();
                        loadBookings();
                    }
                });
    }

    // ----- Helpers ----------------------------------------------------------

    @NonNull
    private static String reasonFrom(@NonNull DialogCancelBookingBinding dialogBinding) {
        return dialogBinding.tilReason.getEditText() == null
                ? ""
                : dialogBinding.tilReason.getEditText().getText().toString().trim();
    }

    private void showBusy(boolean busy) {
        // The spinner only covers an empty screen. Hiding a list the user is
        // already reading would be worse.
        boolean listIsEmpty = adapter.getItemCount() == 0;
        binding.progress.setVisibility(busy && listIsEmpty ? View.VISIBLE : View.GONE);
    }

    /** True when the screen is closing. A late reply must then be dropped. */
    private boolean isGone() {
        return isFinishing() || isDestroyed();
    }
}
