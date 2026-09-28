/*
 * ---------------------------------------------------------------------------
 * File        : NearbyNodeAdapter.java
 * Project     : Smart Solar Microgrid Trading System - SE4040 Assignment 1
 * Author      : NIMSARA R V P P (IT 23215306)
 * Created     : 2026-09-20
 * Description : Fills the rows of the nearby grid nodes list.
 *
 * What an adapter does
 *              A RecyclerView draws rows. It does not know what is in them.
 *              The adapter is the bridge. It says how many rows there are,
 *              and it fills each row with one node.
 *
 * Why RecyclerView here
 *              The node list can be long. RecyclerView keeps only the rows
 *              on screen in memory and reuses them as the user scrolls.
 *              The schedule list in NodeDetailActivity is at most seven
 *              rows, so that one is built by hand instead.
 *
 * Distance may be missing
 *              The list also works with no location permission. It then
 *              shows every node with no distance. So the distance text is
 *              hidden when there is none, instead of showing 0 km.
 *
 * SOLID       Single Responsibility. It fills rows. It does no HTTP and no
 *              SQL, and it does not decide what happens on a tap. The screen
 *              passes that in as a listener.
 * ---------------------------------------------------------------------------
 */
package lk.sliit.solarmicrogrid.ui.map;

import android.view.LayoutInflater;
import android.view.View;
import android.view.ViewGroup;

import androidx.annotation.NonNull;
import androidx.recyclerview.widget.RecyclerView;

import java.util.ArrayList;
import java.util.List;

import lk.sliit.solarmicrogrid.R;
import lk.sliit.solarmicrogrid.databinding.ItemNearbyNodeBinding;
import lk.sliit.solarmicrogrid.model.MicrogridNode;
import lk.sliit.solarmicrogrid.model.NearbyNode;

public final class NearbyNodeAdapter
        extends RecyclerView.Adapter<NearbyNodeAdapter.NodeRow> {

    /** What the screen does when a row is tapped. */
    public interface OnNodeTapped {
        void onNodeTapped(@NonNull MicrogridNode node);
    }

    private final List<NearbyNode> rows = new ArrayList<>();
    private final OnNodeTapped tapListener;

    public NearbyNodeAdapter(@NonNull OnNodeTapped tapListener) {
        this.tapListener = tapListener;
    }

    /**
     * Replaces every row with a new list.
     *
     * notifyDataSetChanged() redraws the whole list. That is fine here. The
     * list is short and it is replaced in one go, not edited row by row.
     */
    @SuppressWarnings("NotifyDataSetChanged")
    public void replaceAll(@NonNull List<NearbyNode> newRows) {
        rows.clear();
        rows.addAll(newRows);
        notifyDataSetChanged();
    }

    @NonNull
    @Override
    public NodeRow onCreateViewHolder(@NonNull ViewGroup parent, int viewType) {
        ItemNearbyNodeBinding binding = ItemNearbyNodeBinding.inflate(
                LayoutInflater.from(parent.getContext()), parent, false);
        return new NodeRow(binding);
    }

    @Override
    public void onBindViewHolder(@NonNull NodeRow holder, int position) {
        holder.show(rows.get(position), tapListener);
    }

    @Override
    public int getItemCount() {
        return rows.size();
    }

    /** Holds the views of one row, so they are not looked up again on scroll. */
    static final class NodeRow extends RecyclerView.ViewHolder {

        private final ItemNearbyNodeBinding binding;

        NodeRow(@NonNull ItemNearbyNodeBinding binding) {
            super(binding.getRoot());
            this.binding = binding;
        }

        void show(@NonNull NearbyNode nearby, @NonNull OnNodeTapped tapListener) {

            MicrogridNode node = nearby.getNode();

            binding.tvName.setText(node.getName());
            binding.tvAddress.setText(
                    node.getAddressLine().trim().isEmpty()
                            ? node.getStationCode()
                            : node.getAddressLine());

            // A closed node cannot be booked, so the free bay count would only
            // mislead. It says Closed instead.
            if (node.isActive()) {
                // getQuantityString, so one free bay does not read "1 bays free".
                binding.tvBays.setText(binding.getRoot().getContext().getResources()
                        .getQuantityString(
                                R.plurals.nearby_bays,
                                node.getTotalBatterySlots(),
                                node.getAvailableBatterySlots(),
                                node.getTotalBatterySlots()));
            } else {
                binding.tvBays.setText(R.string.nearby_closed);
            }

            // A distance below zero means it is not known. That happens when
            // the user did not allow location.
            if (nearby.getDistanceKm() < 0d) {
                binding.tvDistance.setVisibility(View.GONE);
            } else {
                binding.tvDistance.setVisibility(View.VISIBLE);
                binding.tvDistance.setText(nearby.getDistanceText());
            }

            binding.cardRow.setOnClickListener(view -> tapListener.onNodeTapped(node));
        }
    }
}
