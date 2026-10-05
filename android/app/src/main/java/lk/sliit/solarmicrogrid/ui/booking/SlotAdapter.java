/*
 * ---------------------------------------------------------------------------
 * File        : SlotAdapter.java
 * Project     : Smart Solar Microgrid Trading System - SE4040 Assignment 1
 * Author      : L K P Yasith (IT 23380196)
 * Created     : 2026-09-20
 * Description : Fills the rows of the booking window picker. Task D1.
 *
 * One window can be chosen at a time
 *              The adapter remembers which row is chosen and redraws only
 *              the two rows that changed. Redrawing the whole list would
 *              make it flicker on every tap.
 *
 * A full window stays in the list
 *              It is drawn faded and cannot be tapped. Hiding it would make
 *              the list jump about as other prosumers book, and the user
 *              would not know the window exists.
 *
 * SOLID       Single Responsibility. It fills rows and tracks the choice.
 *              It does no HTTP and no SQL, and it does not book anything.
 * ---------------------------------------------------------------------------
 */
package lk.sliit.solarmicrogrid.ui.booking;

import android.view.LayoutInflater;
import android.view.ViewGroup;

import androidx.annotation.NonNull;
import androidx.annotation.Nullable;
import androidx.recyclerview.widget.RecyclerView;

import java.util.ArrayList;
import java.util.List;

import lk.sliit.solarmicrogrid.R;
import lk.sliit.solarmicrogrid.databinding.ItemSlotBinding;
import lk.sliit.solarmicrogrid.model.EnergySlot;

public final class SlotAdapter extends RecyclerView.Adapter<SlotAdapter.SlotRow> {

    /** Nothing chosen yet. */
    private static final int NOTHING_CHOSEN = -1;

    /** How faded a window that cannot be booked looks. */
    private static final float FADED = 0.45f;
    private static final float NORMAL = 1f;

    private final List<EnergySlot> rows = new ArrayList<>();
    private int chosenPosition = NOTHING_CHOSEN;

    /** Replaces every row, and forgets the old choice. */
    @SuppressWarnings("NotifyDataSetChanged")
    public void replaceAll(@NonNull List<EnergySlot> newRows) {
        rows.clear();
        rows.addAll(newRows);
        chosenPosition = NOTHING_CHOSEN;
        notifyDataSetChanged();
    }

    /**
     * Chooses the window with this id, if it is in the list.
     *
     * Used when a booking is being changed, so the window it already has
     * starts off chosen.
     */
    public void chooseById(@Nullable String slotId) {
        if (slotId == null) {
            return;
        }
        for (int index = 0; index < rows.size(); index++) {
            if (slotId.equals(rows.get(index).getId())) {
                choose(index);
                return;
            }
        }
    }

    /** @return the chosen window, or null when none has been chosen. */
    @Nullable
    public EnergySlot getChosen() {
        if (chosenPosition == NOTHING_CHOSEN || chosenPosition >= rows.size()) {
            return null;
        }
        return rows.get(chosenPosition);
    }

    @NonNull
    @Override
    public SlotRow onCreateViewHolder(@NonNull ViewGroup parent, int viewType) {
        ItemSlotBinding binding = ItemSlotBinding.inflate(
                LayoutInflater.from(parent.getContext()), parent, false);
        return new SlotRow(binding);
    }

    @Override
    public void onBindViewHolder(@NonNull SlotRow holder, int position) {
        holder.show(rows.get(position), position == chosenPosition, this::choose);
    }

    @Override
    public int getItemCount() {
        return rows.size();
    }

    /**
     * Marks one row as chosen and clears the one before it.
     *
     * Only the two rows that changed are redrawn.
     */
    private void choose(int position) {
        int previous = chosenPosition;
        chosenPosition = position;

        if (previous != NOTHING_CHOSEN) {
            notifyItemChanged(previous);
        }
        notifyItemChanged(position);
    }

    /** Told which row was tapped. */
    interface OnRowTapped {
        void onRowTapped(int position);
    }

    /** Holds the views of one row, so they are not looked up again on scroll. */
    static final class SlotRow extends RecyclerView.ViewHolder {

        private final ItemSlotBinding binding;

        SlotRow(@NonNull ItemSlotBinding binding) {
            super(binding.getRoot());
            this.binding = binding;
        }

        void show(@NonNull EnergySlot slot, boolean chosen, @NonNull OnRowTapped tapListener) {

            binding.tvTime.setText(binding.getRoot().getContext().getString(
                    R.string.booking_window_row, slot.getStartTime(), slot.getEndTime()));

            if (slot.isBookable()) {
                // getQuantityString, so one free place does not read "1 places".
                binding.tvCapacity.setText(binding.getRoot().getContext().getResources()
                        .getQuantityString(
                                R.plurals.booking_window_free,
                                slot.getTotalCapacitySlots(),
                                slot.getRemainingCapacity(),
                                slot.getTotalCapacitySlots()));
            } else {
                binding.tvCapacity.setText(R.string.booking_window_full);
            }

            // A window that cannot be booked is faded and does not react to a
            // tap. It is still readable, so the user knows it exists.
            binding.cardSlot.setAlpha(slot.isBookable() ? NORMAL : FADED);
            binding.cardSlot.setEnabled(slot.isBookable());
            binding.cardSlot.setCheckable(slot.isBookable());
            binding.cardSlot.setChecked(chosen && slot.isBookable());

            binding.cardSlot.setOnClickListener(view -> {
                if (!slot.isBookable()) {
                    return;
                }
                int position = getBindingAdapterPosition();
                if (position != RecyclerView.NO_POSITION) {
                    tapListener.onRowTapped(position);
                }
            });
        }
    }
}
