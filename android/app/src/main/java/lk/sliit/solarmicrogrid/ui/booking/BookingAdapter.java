/*
 * ---------------------------------------------------------------------------
 * File        : BookingAdapter.java
 * Project     : Smart Solar Microgrid Trading System - SE4040 Assignment 1
 * Author      : L K P Yasith (IT 23380196)
 * Created     : 2026-09-20
 * Description : Fills the rows of the My bookings list.
 *
 * The buttons follow the server
 *              Change and Cancel are shown only when the server says
 *              canBeModified and canBeCancelled are true. Those are business
 *              rules BR-2 and BR-3: a booking can only be touched while it
 *              is more than 12 hours away.
 *
 *              The adapter never works that out from the date. It reads the
 *              two flags. So the phone and the web application always agree.
 *
 *              When both are false, a line of text explains why. Buttons
 *              that vanish with no reason look like a bug.
 *
 * SOLID       Single Responsibility. It fills rows. It books nothing and
 *              cancels nothing. The screen passes those actions in.
 * ---------------------------------------------------------------------------
 */
package lk.sliit.solarmicrogrid.ui.booking;

import android.content.res.ColorStateList;
import android.view.LayoutInflater;
import android.view.View;
import android.view.ViewGroup;

import androidx.annotation.ColorRes;
import androidx.annotation.NonNull;
import androidx.core.content.ContextCompat;
import androidx.core.graphics.ColorUtils;
import androidx.recyclerview.widget.RecyclerView;

import java.util.ArrayList;
import java.util.List;

import lk.sliit.solarmicrogrid.R;
import lk.sliit.solarmicrogrid.databinding.ItemBookingBinding;
import lk.sliit.solarmicrogrid.model.Reservation;
import lk.sliit.solarmicrogrid.util.DateTimes;
import lk.sliit.solarmicrogrid.util.TextFormat;

public final class BookingAdapter extends RecyclerView.Adapter<BookingAdapter.BookingRow> {

    /** What the screen does when a button on a row is tapped. */
    public interface OnBookingAction {

        void onChangeTapped(@NonNull Reservation booking);

        void onCancelTapped(@NonNull Reservation booking);

        void onShowQrTapped(@NonNull Reservation booking);
    }

    /** How solid the pill behind a status word is, out of 255. */
    private static final int PILL_ALPHA = 38;

    private final List<Reservation> rows = new ArrayList<>();
    private final OnBookingAction actionListener;

    public BookingAdapter(@NonNull OnBookingAction actionListener) {
        this.actionListener = actionListener;
    }

    @SuppressWarnings("NotifyDataSetChanged")
    public void replaceAll(@NonNull List<Reservation> newRows) {
        rows.clear();
        rows.addAll(newRows);
        notifyDataSetChanged();
    }

    @NonNull
    @Override
    public BookingRow onCreateViewHolder(@NonNull ViewGroup parent, int viewType) {
        ItemBookingBinding binding = ItemBookingBinding.inflate(
                LayoutInflater.from(parent.getContext()), parent, false);
        return new BookingRow(binding);
    }

    @Override
    public void onBindViewHolder(@NonNull BookingRow holder, int position) {
        holder.show(rows.get(position), actionListener);
    }

    @Override
    public int getItemCount() {
        return rows.size();
    }

    /** Holds the views of one row, so they are not looked up again on scroll. */
    static final class BookingRow extends RecyclerView.ViewHolder {

        private final ItemBookingBinding binding;

        BookingRow(@NonNull ItemBookingBinding binding) {
            super(binding.getRoot());
            this.binding = binding;
        }

        void show(@NonNull Reservation booking, @NonNull OnBookingAction actionListener) {

            binding.tvNode.setText(booking.getStationName());
            binding.tvNumber.setText(booking.getReservationNo());

            // The API sends UTC. This shows it in the phone time zone.
            binding.tvWhen.setText(DateTimes.showDateAndTime(booking.getReservationDateTime()));

            binding.tvEnergy.setText(energyLine(booking));
            showStatus(booking);
            showActions(booking, actionListener);
        }

        /** For example: 12 kWh, Deliver to grid. */
        @NonNull
        private String energyLine(@NonNull Reservation booking) {
            String energy = binding.getRoot().getContext().getString(
                    R.string.bookings_energy_value, TextFormat.capacity(booking.getEnergyKwh()));
            String direction = binding.getRoot().getContext().getString(
                    booking.isDeliver() ? R.string.booking_deliver : R.string.booking_draw);
            return binding.getRoot().getContext().getString(
                    R.string.bookings_energy_line, energy, direction);
        }

        /** A colour per state, so the list can be read at a glance. */
        private void showStatus(@NonNull Reservation booking) {

            @ColorRes int colour;
            if (booking.isCancelled()) {
                colour = R.color.status_danger;
            } else if (booking.isApproved() || booking.isCompleted()) {
                colour = R.color.status_success;
            } else {
                colour = R.color.status_warning;
            }

            int statusColour = ContextCompat.getColor(binding.getRoot().getContext(), colour);

            binding.tvStatus.setText(booking.getStatus());
            binding.tvStatus.setTextColor(statusColour);

            // The pill behind the word is the same colour, faded right back.
            // One colour for both keeps the badge readable without needing a
            // second colour picked by hand for every status.
            binding.tvStatus.setBackgroundTintList(ColorStateList.valueOf(
                    ColorUtils.setAlphaComponent(statusColour, PILL_ALPHA)));
        }

        /**
         * Shows the buttons the server allows, and explains when it allows
         * none.
         *
         * A cancelled or finished booking gets no buttons and no explanation.
         * Nothing more can happen to it, and that is already obvious from the
         * status on the same row.
         */
        private void showActions(@NonNull Reservation booking,
                                 @NonNull OnBookingAction actionListener) {

            boolean canChange = booking.canBeModified();
            boolean canCancel = booking.canBeCancelled();

            binding.btnChange.setVisibility(canChange ? View.VISIBLE : View.GONE);
            binding.btnCancel.setVisibility(canCancel ? View.VISIBLE : View.GONE);
            binding.groupActions.setVisibility(canChange || canCancel ? View.VISIBLE : View.GONE);

            // The 12 hour rule is only worth explaining while the booking is
            // still coming up.
            boolean tooLate = booking.isUpcoming() && !canChange && !canCancel;
            binding.tvLocked.setVisibility(tooLate ? View.VISIBLE : View.GONE);

            // The code only exists once the booking is approved, so the
            // button is hidden until then. Business rule BR-7.
            binding.btnShowQr.setVisibility(booking.hasQrCode() ? View.VISIBLE : View.GONE);

            binding.btnChange.setOnClickListener(view -> actionListener.onChangeTapped(booking));
            binding.btnCancel.setOnClickListener(view -> actionListener.onCancelTapped(booking));
            binding.btnShowQr.setOnClickListener(view -> actionListener.onShowQrTapped(booking));
        }
    }
}
