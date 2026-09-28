/*
 * ---------------------------------------------------------------------------
 * File        : MapActivity.java
 * Project     : Smart Solar Microgrid Trading System - SE4040 Assignment 1
 * Author      : NIMSARA R V P P (IT 23215306)
 * Created     : 2026-09-20
 * Description : Shows grid nodes on a Google Map. Tasks C1, C2 and C3.
 *
 * What it does
 *              1. Draws markers from the SQLite cache at once.
 *              2. Asks the server for a fresh list and redraws.
 *              3. Asks for location permission.
 *              4. If location is allowed, asks the server which node is
 *                 closest and shows it in a card.
 *              5. Tapping a marker opens the node detail screen.
 *
 * Why the cache first
 *              The map must not sit empty while the network answers.
 *              With no signal the user still sees the nodes from last time.
 *
 * Location is optional
 *              The map works without it. It then shows every node instead of
 *              the closest one. The app never blocks the user for saying no.
 *
 * SOLID       Single Responsibility - this class draws the map and handles
 *              taps. It does no HTTP and no SQL. StationRepository does that.
 * ---------------------------------------------------------------------------
 */
package lk.sliit.solarmicrogrid.ui.map;

import android.Manifest;
import android.annotation.SuppressLint;
import android.content.Intent;
import android.location.Location;
import android.os.Bundle;
import android.view.View;
import android.widget.Toast;

import androidx.activity.result.ActivityResultLauncher;
import androidx.activity.result.contract.ActivityResultContracts;
import androidx.annotation.NonNull;
import androidx.annotation.Nullable;
import androidx.appcompat.app.AlertDialog;

import com.google.android.gms.maps.CameraUpdateFactory;
import com.google.android.gms.maps.GoogleMap;
import com.google.android.gms.maps.OnMapReadyCallback;
import com.google.android.gms.maps.SupportMapFragment;
import com.google.android.gms.maps.model.BitmapDescriptorFactory;
import com.google.android.gms.maps.model.LatLng;
import com.google.android.gms.maps.model.LatLngBounds;
import com.google.android.gms.maps.model.Marker;
import com.google.android.gms.maps.model.MarkerOptions;

import java.util.List;

import lk.sliit.solarmicrogrid.R;
import lk.sliit.solarmicrogrid.data.repository.AuthRepository;
import lk.sliit.solarmicrogrid.data.repository.StationRepository;
import lk.sliit.solarmicrogrid.databinding.ActivityMapBinding;
import lk.sliit.solarmicrogrid.model.MicrogridNode;
import lk.sliit.solarmicrogrid.model.NearbyNode;
import lk.sliit.solarmicrogrid.ui.BaseActivity;
import lk.sliit.solarmicrogrid.util.TextFormat;

public final class MapActivity extends BaseActivity implements OnMapReadyCallback {

    /** Where the map starts before anything is known. Centre of Sri Lanka. */
    private static final LatLng SRI_LANKA = new LatLng(7.8731d, 80.7718d);
    private static final float COUNTRY_ZOOM = 7.2f;
    private static final float NEAR_ME_ZOOM = 12f;

    /** Space left around the markers when the camera fits them all, in pixels. */
    private static final int MAP_PADDING_PX = 120;

    private ActivityMapBinding binding;
    private StationRepository stationRepository;
    private DeviceLocation deviceLocation;

    @Nullable
    private GoogleMap map;

    /** The node shown in the bottom card. Null until we know it. */
    @Nullable
    private MicrogridNode nearestNode;

    /**
     * Handles the answer from the permission dialog.
     *
     * It is set up here, not inside a method. Android needs this ready before
     * the screen starts. Otherwise it throws when the dialog returns.
     */
    private final ActivityResultLauncher<String> locationPermissionLauncher =
            registerForActivityResult(new ActivityResultContracts.RequestPermission(), granted -> {
                if (granted) {
                    showMyLocationAndNearest();
                } else {
                    Toast.makeText(this, R.string.map_permission_denied, Toast.LENGTH_LONG).show();
                }
            });

    @Override
    protected void onCreate(@Nullable Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);

        binding = ActivityMapBinding.inflate(getLayoutInflater());
        setContentView(binding.getRoot());
        applySystemBarInsets(binding.root);

        stationRepository = StationRepository.create();
        deviceLocation = new DeviceLocation(this);

        binding.toolbar.setNavigationOnClickListener(view -> finish());
        binding.btnNearestDetails.setOnClickListener(view -> openNearestNode());

        SupportMapFragment mapFragment =
                (SupportMapFragment) getSupportFragmentManager().findFragmentById(R.id.map);
        if (mapFragment != null) {
            mapFragment.getMapAsync(this);
        }
    }

    /**
     * Google Play services calls this once the map is ready to use.
     * Nothing may touch the map before this runs.
     */
    @Override
    public void onMapReady(@NonNull GoogleMap readyMap) {

        map = readyMap;
        map.moveCamera(CameraUpdateFactory.newLatLngZoom(SRI_LANKA, COUNTRY_ZOOM));

        // Tapping the label above a marker opens that node.
        map.setOnInfoWindowClickListener(marker -> {
            Object tag = marker.getTag();
            if (tag instanceof MicrogridNode) {
                openNode((MicrogridNode) tag);
            }
        });

        showCachedNodes();
        refreshNodesFromServer();
        askForLocation();
    }

    // ----- Drawing the nodes ------------------------------------------------

    /** Draws whatever the phone already has, so the map is never empty. */
    private void showCachedNodes() {
        List<MicrogridNode> cached = stationRepository.cachedNodes();
        if (!cached.isEmpty()) {
            drawNodes(cached);
        }
    }

    private void refreshNodesFromServer() {

        binding.progress.setVisibility(View.VISIBLE);

        stationRepository.loadAll(new AuthRepository.Callback<List<MicrogridNode>>() {

            @Override
            public void onSuccess(@Nullable List<MicrogridNode> nodes) {
                if (isGone() || nodes == null) {
                    return;
                }
                binding.progress.setVisibility(View.GONE);

                if (nodes.isEmpty()) {
                    Toast.makeText(MapActivity.this, R.string.map_no_nodes, Toast.LENGTH_LONG).show();
                    return;
                }
                drawNodes(nodes);
            }

            @Override
            public void onFailure(@NonNull String message) {
                if (isGone()) {
                    return;
                }
                binding.progress.setVisibility(View.GONE);

                // The cached markers are still on screen, so this is only a
                // note. It is not a failure the user has to act on.
                Toast.makeText(MapActivity.this, message, Toast.LENGTH_LONG).show();
            }
        });
    }

    /**
     * Clears the map, then adds one marker for each node.
     *
     * Clearing first matters. Without it the new markers would sit on top of
     * the cached ones, and every node would show twice.
     */
    private void drawNodes(@NonNull List<MicrogridNode> nodes) {

        if (map == null) {
            return;
        }

        map.clear();
        LatLngBounds.Builder bounds = new LatLngBounds.Builder();
        boolean hasMarker = false;

        for (MicrogridNode node : nodes) {

            // A node saved with no coordinates would land at 0,0. That is
            // in the sea off Africa, and it would drag the camera there.
            if (node.getLatitude() == 0d && node.getLongitude() == 0d) {
                continue;
            }

            LatLng position = new LatLng(node.getLatitude(), node.getLongitude());

            Marker marker = map.addMarker(new MarkerOptions()
                    .position(position)
                    .title(node.getName())
                    .snippet(subtitleFor(node))
                    .icon(BitmapDescriptorFactory.defaultMarker(colourFor(node))));

            // The node is attached to its own marker. When the user taps
            // it, the node comes back with no searching by name or id.
            if (marker != null) {
                marker.setTag(node);
            }

            bounds.include(position);
            hasMarker = true;
        }

        // Only move the camera when we are not already following the user.
        if (hasMarker && nearestNode == null) {
            map.moveCamera(CameraUpdateFactory.newLatLngBounds(bounds.build(), MAP_PADDING_PX));
        }
    }

    /** Orange for an open node. Blue for a closed one. */
    private static float colourFor(@NonNull MicrogridNode node) {
        return node.isActive()
                ? BitmapDescriptorFactory.HUE_ORANGE
                : BitmapDescriptorFactory.HUE_AZURE;
    }

    /** The small line under the marker name. */
    @NonNull
    private String subtitleFor(@NonNull MicrogridNode node) {
        if (!node.isActive()) {
            return getString(R.string.node_status_inactive);
        }
        return getString(R.string.node_bays_value,
                node.getAvailableBatterySlots(),
                node.getTotalBatterySlots());
    }

    // ----- Location and the nearest node ------------------------------------

    /**
     * Asks for location permission. Explains why first.
     *
     * shouldShowRequestPermissionRationale is true when the user said no once
     * before. Explaining before asking again is the polite order. It is also
     * what the Google guidance asks for.
     */
    private void askForLocation() {

        if (hasLocationPermission()) {
            showMyLocationAndNearest();
            return;
        }

        if (shouldShowRequestPermissionRationale(Manifest.permission.ACCESS_FINE_LOCATION)) {
            new AlertDialog.Builder(this)
                    .setTitle(R.string.map_permission_title)
                    .setMessage(R.string.map_permission_message)
                    .setNegativeButton(R.string.action_not_now, null)
                    .setPositiveButton(R.string.action_allow, (dialog, which) ->
                            locationPermissionLauncher.launch(Manifest.permission.ACCESS_FINE_LOCATION))
                    .show();
            return;
        }

        locationPermissionLauncher.launch(Manifest.permission.ACCESS_FINE_LOCATION);
    }

    private boolean hasLocationPermission() {
        return DeviceLocation.hasPermission(this);
    }

    /**
     * Turns on the blue dot. Asks the server for the closest node.
     *
     * The permission is checked again, even though the caller checked it.
     * The user can switch it off in Settings while the app is open. Android
     * throws a SecurityException if it is used without permission.
     */
    @SuppressLint("MissingPermission")
    private void showMyLocationAndNearest() {

        if (map == null || !hasLocationPermission()) {
            return;
        }

        // Location off for the whole phone means no fix is possible. Say so,
        // instead of leaving the user waiting for a card that cannot appear.
        if (!DeviceLocation.isSwitchedOn(this)) {
            Toast.makeText(this, R.string.map_location_off, Toast.LENGTH_LONG).show();
            return;
        }

        // The permission is checked on the line above. Lint cannot follow a
        // check that sits inside a helper method, so it is told here.
        map.setMyLocationEnabled(true);
        binding.progress.setVisibility(View.VISIBLE);

        deviceLocation.find(location -> {
            if (isGone()) {
                return;
            }
            binding.progress.setVisibility(View.GONE);

            if (location == null) {
                // No fix could be made. The map still shows every node.
                Toast.makeText(this, R.string.map_location_off, Toast.LENGTH_LONG).show();
                return;
            }
            moveCameraTo(location);
            loadNearest(location);
        });
    }

    @Override
    protected void onDestroy() {
        super.onDestroy();
        // Stop looking for a position nobody is waiting for any more.
        deviceLocation.cancel();
    }

    private void moveCameraTo(@NonNull Location location) {
        if (map == null) {
            return;
        }
        LatLng here = new LatLng(location.getLatitude(), location.getLongitude());
        map.animateCamera(CameraUpdateFactory.newLatLngZoom(here, NEAR_ME_ZOOM));
    }

    /**
     * Asks the server which nodes are close. Shows the first one.
     *
     * The server does the distance sum, not the phone. So the app and the web
     * application always give the same answer.
     */
    private void loadNearest(@NonNull Location location) {

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
                        binding.progress.setVisibility(View.GONE);

                        if (nearby.isEmpty()) {
                            binding.cardNearest.setVisibility(View.GONE);
                            Toast.makeText(MapActivity.this,
                                    getString(R.string.map_no_nodes_nearby,
                                            TextFormat.capacity(StationRepository.DEFAULT_RADIUS_KM)),
                                    Toast.LENGTH_LONG).show();
                            return;
                        }

                        // The server sends them nearest first.
                        showNearest(nearby.get(0));
                    }

                    @Override
                    public void onFailure(@NonNull String message) {
                        if (isGone()) {
                            return;
                        }
                        binding.progress.setVisibility(View.GONE);
                        Toast.makeText(MapActivity.this, message, Toast.LENGTH_LONG).show();
                    }
                });
    }

    private void showNearest(@NonNull NearbyNode nearby) {
        nearestNode = nearby.getNode();
        binding.tvNearestName.setText(nearestNode.getName());
        binding.tvNearestDistance.setText(nearby.getDistanceText());
        binding.cardNearest.setVisibility(View.VISIBLE);
    }

    // ----- Opening a node ---------------------------------------------------

    private void openNearestNode() {
        if (nearestNode != null) {
            openNode(nearestNode);
        }
    }

    /** Only the id is passed. The detail screen fetches fresh data itself. */
    private void openNode(@NonNull MicrogridNode node) {
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
