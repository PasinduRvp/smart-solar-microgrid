/*
 * ---------------------------------------------------------------------------
 * File        : NearbyNodesActivity.java
 * Project     : Smart Solar Microgrid Trading System - SE4040 Assignment 1
 * Author      : NIMSARA R V P P (IT 23215306)
 * Created     : 2026-09-20
 * Description : Grid nodes as a list, nearest first. Task C2.
 *
 * Why this screen exists next to the map
 *              The map needs a Google Maps key, and that key needs a billing
 *              account. This screen needs neither. It uses the same
 *              /api/stations/nearby endpoint, so the nearby node feature
 *              works on any phone.
 *
 *              It is also quicker to read. The closest node is the first
 *              row. Nobody has to pan or zoom to find it.
 *
 * Three ways it can fill
 *              1. Cache first. Nodes from SQLite, with no distance, so the
 *                 screen is never blank.
 *              2. Location allowed. The server sorts by distance and each
 *                 row shows how far away it is.
 *              3. Location refused. Every node, still with no distance. The
 *                 screen still works. It is never blocked.
 *
 * SOLID       Single Responsibility. It loads a list and shows it. No HTTP
 *              and no SQL of its own. StationRepository does that.
 * ---------------------------------------------------------------------------
 */
package lk.sliit.solarmicrogrid.ui.map;

import android.Manifest;
import android.content.Intent;
import android.content.pm.PackageManager;
import android.location.Location;
import android.os.Bundle;
import android.view.View;
import android.widget.Toast;

import androidx.activity.result.ActivityResultLauncher;
import androidx.activity.result.contract.ActivityResultContracts;
import androidx.annotation.NonNull;
import androidx.annotation.Nullable;
import androidx.core.content.ContextCompat;
import androidx.recyclerview.widget.LinearLayoutManager;

import java.util.ArrayList;
import java.util.List;

import lk.sliit.solarmicrogrid.BuildConfig;
import lk.sliit.solarmicrogrid.R;
import lk.sliit.solarmicrogrid.data.repository.AuthRepository;
import lk.sliit.solarmicrogrid.data.repository.StationRepository;
import lk.sliit.solarmicrogrid.databinding.ActivityNearbyNodesBinding;
import lk.sliit.solarmicrogrid.model.MicrogridNode;
import lk.sliit.solarmicrogrid.model.NearbyNode;
import lk.sliit.solarmicrogrid.ui.BaseActivity;

public final class NearbyNodesActivity extends BaseActivity
        implements NearbyNodeAdapter.OnNodeTapped {

    private ActivityNearbyNodesBinding binding;
    private StationRepository stationRepository;
    private DeviceLocation deviceLocation;
    private NearbyNodeAdapter adapter;

    /**
     * Handles the answer from the permission dialog.
     *
     * It is set up here, not inside a method. Android needs this ready before
     * the screen starts. Otherwise it throws when the dialog returns.
     */
    private final ActivityResultLauncher<String> locationPermissionLauncher =
            registerForActivityResult(new ActivityResultContracts.RequestPermission(), granted -> {
                if (granted) {
                    loadSortedByDistance();
                } else {
                    // Saying no is allowed. The list still shows every node.
                    loadAllNodes();
                }
            });

    @Override
    protected void onCreate(@Nullable Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);

        binding = ActivityNearbyNodesBinding.inflate(getLayoutInflater());
        setContentView(binding.getRoot());
        applySystemBarInsets(binding.root);

        stationRepository = StationRepository.create();
        deviceLocation = new DeviceLocation(this);

        adapter = new NearbyNodeAdapter(this);
        binding.list.setLayoutManager(new LinearLayoutManager(this));
        binding.list.setAdapter(adapter);

        binding.toolbar.setNavigationOnClickListener(view -> finish());
        binding.refresh.setOnRefreshListener(this::askForLocation);
        binding.toolbar.inflateMenu(R.menu.menu_nearby);
        binding.toolbar.setOnMenuItemClickListener(item -> {
            if (item.getItemId() == R.id.action_map) {
                startActivity(new Intent(this, mapScreen()));
                return true;
            }
            return false;
        });

        showCachedNodes();
        askForLocation();
    }

    /**
     * Picks the map screen that will actually work on this build.
     *
     * Google Maps needs an API key in local.properties. Without one its map
     * draws an empty grey grid. OpenStreetMap needs no key at all.
     *
     * So the choice is made from the key, not written into the code. Add a
     * key later and the Google screen is used, with nothing else to change.
     */
    @NonNull
    private Class<?> mapScreen() {
        return BuildConfig.HAS_MAPS_KEY ? MapActivity.class : OsmMapActivity.class;
    }

    /** Draws whatever the phone already has, so the list is never blank. */
    private void showCachedNodes() {
        List<MicrogridNode> cached = stationRepository.cachedNodes();
        if (!cached.isEmpty()) {
            showNodesWithoutDistance(cached);
        }
    }

    // ----- Location ---------------------------------------------------------

    private void askForLocation() {
        if (hasLocationPermission()) {
            loadSortedByDistance();
            return;
        }
        locationPermissionLauncher.launch(Manifest.permission.ACCESS_FINE_LOCATION);
    }

    private boolean hasLocationPermission() {
        return DeviceLocation.hasPermission(this);
    }

    /**
     * Finds where the phone is, then asks the server for nodes near it.
     *
     * The permission is checked again, even though the caller checked it. The
     * user can switch it off in Settings while the app is open. Android
     * throws a SecurityException if it is used without permission.
     */
    private void loadSortedByDistance() {

        if (!hasLocationPermission() || !DeviceLocation.isSwitchedOn(this)) {
            loadAllNodes();
            return;
        }

        showBusy(true);

        deviceLocation.find(location -> {
            if (isGone()) {
                return;
            }
            if (location == null) {
                // No fix could be made. Show every node rather than nothing.
                loadAllNodes();
                return;
            }
            loadNearby(location);
        });
    }

    @Override
    protected void onDestroy() {
        super.onDestroy();
        // Stop looking for a position nobody is waiting for any more.
        deviceLocation.cancel();
    }

    private void loadNearby(@NonNull Location location) {

        stationRepository.loadNearby(
                location.getLatitude(),
                location.getLongitude(),
                StationRepository.DEFAULT_RADIUS_KM,
                new AuthRepository.Callback<List<NearbyNode>>() {

                    @Override
                    public void onSuccess(@Nullable List<NearbyNode> nearby) {
                        if (isGone() || nearby == null) {
                            return;
                        }
                        showBusy(false);

                        // Nothing within the radius does not mean there are no
                        // nodes at all. Fall back to the full list.
                        if (nearby.isEmpty()) {
                            loadAllNodes();
                            return;
                        }

                        binding.tvHint.setText(R.string.nearby_sorted_by_distance);
                        showRows(nearby);
                    }

                    @Override
                    public void onFailure(@NonNull String message) {
                        if (isGone()) {
                            return;
                        }
                        showBusy(false);
                        binding.refresh.setRefreshing(false);
                        Toast.makeText(NearbyNodesActivity.this, message, Toast.LENGTH_LONG).show();
                    }
                });
    }

    // ----- Every node, with no distance --------------------------------------

    private void loadAllNodes() {

        showBusy(true);
        binding.tvHint.setText(R.string.nearby_all_nodes);

        stationRepository.loadAll(new AuthRepository.Callback<List<MicrogridNode>>() {

            @Override
            public void onSuccess(@Nullable List<MicrogridNode> nodes) {
                if (isGone() || nodes == null) {
                    return;
                }
                showBusy(false);
                showNodesWithoutDistance(nodes);
            }

            @Override
            public void onFailure(@NonNull String message) {
                if (isGone()) {
                    return;
                }
                showBusy(false);

                // The cached rows may already be on screen, so this is only a
                // note. It is not a failure the user has to act on.
                Toast.makeText(NearbyNodesActivity.this, message, Toast.LENGTH_LONG).show();
                showEmptyIfNoRows();
            }
        });
    }

    /** Wraps plain nodes as rows with an unknown distance. */
    private void showNodesWithoutDistance(@NonNull List<MicrogridNode> nodes) {
        List<NearbyNode> rows = new ArrayList<>(nodes.size());
        for (MicrogridNode node : nodes) {
            rows.add(new NearbyNode(node, NearbyNode.UNKNOWN_DISTANCE));
        }
        showRows(rows);
    }

    // ----- Drawing -----------------------------------------------------------

    private void showRows(@NonNull List<NearbyNode> rows) {
        binding.refresh.setRefreshing(false);
        adapter.replaceAll(rows);
        binding.tvEmpty.setVisibility(rows.isEmpty() ? View.VISIBLE : View.GONE);
    }

    private void showEmptyIfNoRows() {
        binding.tvEmpty.setVisibility(adapter.getItemCount() == 0 ? View.VISIBLE : View.GONE);
    }

    private void showBusy(boolean busy) {
        // The spinner is only shown when there is nothing to look at yet.
        // Covering a list the user is already reading would be worse.
        boolean listIsEmpty = adapter.getItemCount() == 0;
        binding.progress.setVisibility(busy && listIsEmpty ? View.VISIBLE : View.GONE);
    }

    @Override
    public void onNodeTapped(@NonNull MicrogridNode node) {
        Intent intent = new Intent(this, NodeDetailActivity.class);
        intent.putExtra(NodeDetailActivity.EXTRA_NODE_ID, node.getId());
        intent.putExtra(NodeDetailActivity.EXTRA_NODE_NAME, node.getName());
        startActivity(intent);
    }

    /** True when the screen is closing. A late reply must then be dropped. */
    private boolean isGone() {
        return isFinishing() || isDestroyed();
    }
}
