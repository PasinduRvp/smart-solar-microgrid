/*
 * ---------------------------------------------------------------------------
 * File        : NodeDetailActivity.java
 * Project     : Smart Solar Microgrid Trading System - SE4040 Assignment 1
 * Author      : NIMSARA R V P P (IT 23215306)
 * Created     : 2026-09-20
 * Description : One grid node in full, with its opening hours. Task C3.
 *
 * Why it fetches instead of being passed the node
 *              Only the node id arrives from the map. This screen then asks
 *              the server for the rest.
 *
 *              Two reasons. The free battery bays change as other prosumers
 *              book, so a copy passed from the map could already be wrong.
 *              And the opening hours are not in the SQLite cache at all.
 *
 *              The name is passed as well. That lets the title show at once
 *              instead of staying blank while the call runs.
 *
 * SOLID       Single Responsibility - it shows one node. No HTTP, no SQL.
 * ---------------------------------------------------------------------------
 */
package lk.sliit.solarmicrogrid.ui.map;

import android.content.res.ColorStateList;
import android.os.Bundle;
import android.view.View;
import android.widget.LinearLayout;
import android.widget.TextView;
import android.widget.Toast;

import androidx.annotation.NonNull;
import androidx.annotation.Nullable;
import androidx.core.content.ContextCompat;
import androidx.core.graphics.ColorUtils;

import java.util.List;

import lk.sliit.solarmicrogrid.R;
import lk.sliit.solarmicrogrid.SolarApp;
import lk.sliit.solarmicrogrid.model.Roles;
import lk.sliit.solarmicrogrid.model.Session;
import lk.sliit.solarmicrogrid.ui.booking.NewBookingActivity;
import lk.sliit.solarmicrogrid.data.remote.dto.ScheduleEntryDto;
import lk.sliit.solarmicrogrid.data.remote.dto.StationDto;
import lk.sliit.solarmicrogrid.data.repository.AuthRepository;
import lk.sliit.solarmicrogrid.data.repository.StationRepository;
import lk.sliit.solarmicrogrid.databinding.ActivityNodeDetailBinding;
import lk.sliit.solarmicrogrid.model.MicrogridNode;
import lk.sliit.solarmicrogrid.ui.BaseActivity;
import lk.sliit.solarmicrogrid.util.TextFormat;

public final class NodeDetailActivity extends BaseActivity {

    /** How solid the pill behind the status words is, out of 255. */
    private static final int PILL_ALPHA = 38;

    public static final String EXTRA_NODE_ID = "node_id";
    public static final String EXTRA_NODE_NAME = "node_name";

    private ActivityNodeDetailBinding binding;
    private StationRepository stationRepository;

    @Override
    protected void onCreate(@Nullable Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);

        binding = ActivityNodeDetailBinding.inflate(getLayoutInflater());
        setContentView(binding.getRoot());
        applySystemBarInsets(binding.root);

        stationRepository = StationRepository.create();
        binding.toolbar.setNavigationOnClickListener(view -> finish());

        String nodeId = getIntent().getStringExtra(EXTRA_NODE_ID);

        // Nothing to show without an id. This should not happen, but a
        // blank screen would be worse than closing.
        if (nodeId == null || nodeId.trim().isEmpty()) {
            finish();
            return;
        }

        // Show the name straight away so the screen is not blank while
        // the call runs.
        String nodeName = getIntent().getStringExtra(EXTRA_NODE_NAME);
        if (nodeName != null) {
            binding.tvName.setText(nodeName);
        }

        loadNode(nodeId);
    }

    private void loadNode(@NonNull String nodeId) {

        binding.progress.setVisibility(View.VISIBLE);

        stationRepository.loadById(nodeId, new AuthRepository.Callback<StationDto>() {

            @Override
            public void onSuccess(@Nullable StationDto station) {
                if (isGone() || station == null) {
                    return;
                }
                binding.progress.setVisibility(View.GONE);
                showNode(station);
            }

            @Override
            public void onFailure(@NonNull String message) {
                if (isGone()) {
                    return;
                }
                binding.progress.setVisibility(View.GONE);
                Toast.makeText(NodeDetailActivity.this, message, Toast.LENGTH_LONG).show();
            }
        });
    }

    private void showNode(@NonNull StationDto station) {

        MicrogridNode node = MicrogridNode.from(station);

        binding.tvName.setText(node.getName());
        binding.tvCode.setText(node.getStationCode());
        binding.tvAddress.setText(addressOf(node));

        binding.tvCapacity.setText(getString(
                R.string.node_capacity_value,
                TextFormat.capacity(node.getCapacityKwh())));

        binding.tvBays.setText(getString(
                R.string.node_bays_value,
                node.getAvailableBatterySlots(),
                node.getTotalBatterySlots()));

        showStatus(node);
        showSchedule(station.getSchedule());
        showBookButton(node);
    }

    /**
     * Shows the Book button only when booking is actually possible.
     *
     * A closed node takes no bookings. A Grid Operator does not book energy,
     * they approve and complete it. Showing the button to either would lead
     * to a refusal the user could not have predicted.
     */
    private void showBookButton(@NonNull MicrogridNode node) {

        Session session = SolarApp.get().sessions().read();
        boolean prosumer = session != null && Roles.isProsumer(session.getRole());
        boolean canBook = prosumer && node.isActive();

        binding.btnBook.setVisibility(canBook ? View.VISIBLE : View.GONE);
        binding.btnBook.setOnClickListener(view ->
                startActivity(NewBookingActivity.bookIntent(
                        this, node.getId(), node.getName())));
    }

    /** Green when the node takes bookings, red when it does not. */
    private void showStatus(@NonNull MicrogridNode node) {

        int textRes = node.isActive()
                ? R.string.node_status_active
                : R.string.node_status_inactive;

        int colourRes = node.isActive()
                ? R.color.status_success
                : R.color.status_danger;

        int statusColour = ContextCompat.getColor(this, colourRes);

        binding.tvStatus.setText(textRes);
        binding.tvStatus.setTextColor(statusColour);

        // The pill behind the words is the same colour, faded right back.
        binding.tvStatus.setBackgroundTintList(ColorStateList.valueOf(
                ColorUtils.setAlphaComponent(statusColour, PILL_ALPHA)));
    }

    /**
     * Adds one row for each day.
     *
     * A node has seven rows at most. So the rows are built here, not with a
     * RecyclerView. A RecyclerView is for reusing views while scrolling a
     * long list. There is no long list here.
     */
    private void showSchedule(@Nullable List<ScheduleEntryDto> schedule) {

        LinearLayout container = binding.containerSchedule;

        // Remove the old rows first. Without this, opening the screen
        // twice would show every day two times.
        container.removeAllViews();

        if (schedule == null || schedule.isEmpty()) {
            container.addView(makeRow(getString(R.string.node_no_schedule)));
            return;
        }

        for (ScheduleEntryDto entry : schedule) {
            container.addView(makeRow(getString(
                    R.string.node_schedule_row,
                    entry.getDayOfWeek(),
                    entry.getOpenTime(),
                    entry.getCloseTime())));
        }
    }

    @NonNull
    private TextView makeRow(@NonNull String text) {

        TextView row = new TextView(this);
        row.setText(text);
        // No colour is set here on purpose. The text appearance takes the
        // colour from the active theme, so the row stays readable in dark mode.
        row.setTextAppearance(com.google.android.material.R.style.TextAppearance_Material3_BodyLarge);

        LinearLayout.LayoutParams params = new LinearLayout.LayoutParams(
                LinearLayout.LayoutParams.MATCH_PARENT,
                LinearLayout.LayoutParams.WRAP_CONTENT);
        params.topMargin = getResources().getDimensionPixelSize(R.dimen.space_xs);
        row.setLayoutParams(params);

        return row;
    }

    /** Shows Not set when the node has no address. */
    @NonNull
    private String addressOf(@NonNull MicrogridNode node) {
        if (node.getAddressLine() == null || node.getAddressLine().trim().isEmpty()) {
            return getString(R.string.profile_value_not_set);
        }
        return node.getAddressLine();
    }

    /** True when the screen is closing. A late reply must then be dropped. */
    private boolean isGone() {
        return isFinishing() || isDestroyed();
    }
}
