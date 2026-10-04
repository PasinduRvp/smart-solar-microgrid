/*
 * ---------------------------------------------------------------------------
 * File        : OsmMapActivity.java
 * Project     : Smart Solar Microgrid Trading System - SE4040 Assignment 1
 * Author      : NIMSARA R V P P (IT 23215306)
 * Created     : 2026-09-20
 * Description : Shows grid nodes on an OpenStreetMap. Tasks C1, C2 and C3.
 *
 * Why this screen exists
 *              Google Maps needs an API key, and that key needs a billing
 *              account with a card. OpenStreetMap needs neither.
 *              This screen gives the same feature with no key at all.
 *
 *              MapActivity does the same job with Google Maps. The app picks
 *              between them at run time using BuildConfig.HAS_MAPS_KEY. Add a
 *              key to local.properties and the Google screen is used instead.
 *              No code has to change.
 *
 * It does the same four things as the Google screen
 *              1. Draws markers from the SQLite cache at once.
 *              2. Asks the server for a fresh list and redraws.
 *              3. Asks for location permission, then centres on the user.
 *              4. Asks the server which node is closest and shows it in a
 *                 card at the bottom.
 *
 * Two rules osmdroid asks for
 *              The user agent must be set to the package name. Without it the
 *              tile servers refuse the requests and the map stays blank.
 *              The map data must be credited. That is why the toolbar has
 *              the OpenStreetMap line under its title.
 *
 * SOLID       Single Responsibility. It draws the map and handles taps.
 *              No HTTP and no SQL. StationRepository does that.
 * ---------------------------------------------------------------------------
 */
package lk.sliit.solarmicrogrid.ui.map;

import android.Manifest;
import android.content.Intent;
import android.content.pm.PackageManager;
import android.graphics.drawable.Drawable;
import android.location.Location;
import android.os.Bundle;
import android.view.View;
import android.widget.Toast;

import androidx.activity.result.ActivityResultLauncher;
import androidx.activity.result.contract.ActivityResultContracts;
import androidx.annotation.ColorRes;
import androidx.annotation.NonNull;
import androidx.annotation.Nullable;
import androidx.core.content.ContextCompat;
import androidx.core.graphics.drawable.DrawableCompat;

import org.osmdroid.config.Configuration;
import org.osmdroid.tileprovider.tilesource.TileSourceFactory;
import org.osmdroid.util.BoundingBox;
import org.osmdroid.util.GeoPoint;
import org.osmdroid.views.overlay.Marker;

import java.util.ArrayList;
import java.util.List;

import lk.sliit.solarmicrogrid.BuildConfig;
import lk.sliit.solarmicrogrid.R;
import lk.sliit.solarmicrogrid.data.repository.AuthRepository;
import lk.sliit.solarmicrogrid.data.repository.StationRepository;
import lk.sliit.solarmicrogrid.databinding.ActivityOsmMapBinding;
import lk.sliit.solarmicrogrid.model.MicrogridNode;
import lk.sliit.solarmicrogrid.model.NearbyNode;
import lk.sliit.solarmicrogrid.ui.BaseActivity;
import lk.sliit.solarmicrogrid.util.TextFormat;

public final class OsmMapActivity extends BaseActivity {

    /** Where the map starts before anything is known. Centre of Sri Lanka. */
    private static final GeoPoint SRI_LANKA = new GeoPoint(7.8731d, 80.7718d);
    private static final double COUNTRY_ZOOM = 7.5d;
    private static final double NEAR_ME_ZOOM = 13d;

    /** Space left around the markers when the map fits them all, in pixels. */
    private static final int MAP_PADDING_PX = 80;

    /** Where osmdroid keeps its own settings and its tile cache. */
    private static final String OSMDROID_PREFS = "osmdroid";

    private ActivityOsmMapBinding binding;
    private StationRepository stationRepository;
    private DeviceLocation deviceLocation;

    /** The node shown in the bottom card. Null until it is known. */
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
                    centreOnUserAndFindNearest();
                } else {
                    Toast.makeText(this, R.string.map_permission_denied, Toast.LENGTH_LONG).show();
                }
            });

    @Override
    protected void onCreate(@Nullable Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);

        // osmdroid has to be set up BEFORE the layout is inflated. The MapView
        // reads this configuration while it is being created.
        setUpOsmdroid();

        binding = ActivityOsmMapBinding.inflate(getLayoutInflater());
        setContentView(binding.getRoot());
        applySystemBarInsets(binding.root);

        stationRepository = StationRepository.create();
        deviceLocation = new DeviceLocation(this);

        binding.toolbar.setNavigationOnClickListener(view -> finish());

        // OpenStreetMap asks for credit wherever its map data is shown.
        binding.toolbar.setSubtitle(R.string.map_attribution);

        binding.btnNearestDetails.setOnClickListener(view -> openNearestNode());

        setUpMapView();
        showCachedNodes();
        refreshNodesFromServer();
        askForLocation();
    }

    /**
     * Loads the osmdroid settings and sets the user agent.
     *
     * The user agent is not optional. The public tile servers refuse requests
     * that do not name the app, and the map then stays blank with no error on
     * screen.
     */
    private void setUpOsmdroid() {
        Configuration.getInstance().load(
                this, getSharedPreferences(OSMDROID_PREFS, MODE_PRIVATE));
        Configuration.getInstance().setUserAgentValue(BuildConfig.APPLICATION_ID);
    }

    private void setUpMapView() {
        binding.map.setTileSource(TileSourceFactory.MAPNIK);

        // Lets the user pinch to zoom. Off by default in osmdroid.
        binding.map.setMultiTouchControls(true);

        binding.map.getController().setZoom(COUNTRY_ZOOM);
        binding.map.getController().setCenter(SRI_LANKA);
    }

    /*
     * osmdroid needs these two calls. onResume starts the tile downloads
     * again. onPause stops them, so a screen in the background does not keep
     * using data and battery.
     */
    @Override
    protected void onResume() {
        super.onResume();
        binding.map.onResume();
    }

    @Override
    protected void onPause() {
        super.onPause();
        binding.map.onPause();
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
                    Toast.makeText(OsmMapActivity.this,
                            R.string.map_no_nodes, Toast.LENGTH_LONG).show();
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
                Toast.makeText(OsmMapActivity.this, message, Toast.LENGTH_LONG).show();
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

        binding.map.getOverlays().clear();

        List<GeoPoint> drawn = new ArrayList<>();

        for (MicrogridNode node : nodes) {

            // A node saved with no coordinates would land at 0,0. That is in
            // the sea off Africa, and it would drag the map there.
            if (node.getLatitude() == 0d && node.getLongitude() == 0d) {
                continue;
            }

            GeoPoint position = new GeoPoint(node.getLatitude(), node.getLongitude());

            Marker marker = new Marker(binding.map);
            marker.setPosition(position);

            // The point of the pin sits on the place, not its middle.
            marker.setAnchor(Marker.ANCHOR_CENTER, Marker.ANCHOR_BOTTOM);
            marker.setTitle(node.getName());
            marker.setSubDescription(subtitleFor(node));
            // Brand green for an open node, muted for a closed one.
            marker.setIcon(markerIcon(node.isActive()
                    ? R.color.brand_green_deep
                    : R.color.ink_muted));

            // Tapping a marker opens that node. The node is attached to the
            // marker, so nothing has to be looked up by name or id.
            marker.setRelatedObject(node);
            marker.setOnMarkerClickListener((tapped, mapView) -> {
                Object related = tapped.getRelatedObject();
                if (related instanceof MicrogridNode) {
                    openNode((MicrogridNode) related);
                }
                return true;
            });

            binding.map.getOverlays().add(marker);
            drawn.add(position);
        }

        // Only move the map when it is not already following the user.
        if (nearestNode == null) {
            fitMapTo(drawn);
        }

        // invalidate() is what redraws the map. Without it the markers are
        // added but nothing changes on screen.
        binding.map.invalidate();
    }

    /**
     * Moves the map so every marker is visible.
     *
     * Two things here would crash if they were not handled.
     *
     * 1. Size not known yet.
     *    This runs from onCreate, before Android has measured the map. Its
     *    width and height are still 0 then. osmdroid works out the zoom from
     *    those numbers, and the sum breaks when they are zero.
     *    So if the map has no width yet, the work is posted and runs again
     *    after the first layout pass.
     *
     * 2. One marker, or several at the same spot.
     *    A box around a single point has no width and no height. Zooming to
     *    fit it asks for infinite zoom. That case just centres instead.
     */
    private void fitMapTo(@NonNull List<GeoPoint> points) {

        if (points.isEmpty()) {
            return;
        }

        // Not measured yet. Try again once the first layout pass is done.
        if (binding.map.getWidth() == 0 || binding.map.getHeight() == 0) {
            binding.map.post(() -> {
                if (!isGone()) {
                    fitMapTo(points);
                }
            });
            return;
        }

        BoundingBox box = BoundingBox.fromGeoPoints(points);

        if (hasNoArea(box)) {
            binding.map.getController().setZoom(NEAR_ME_ZOOM);
            binding.map.getController().setCenter(points.get(0));
            return;
        }

        binding.map.zoomToBoundingBox(box, false, MAP_PADDING_PX);
    }

    /** True when every point is at the same place, so the box is a dot. */
    private static boolean hasNoArea(@NonNull BoundingBox box) {
        return box.getLatNorth() == box.getLatSouth()
                || box.getLonEast() == box.getLonWest();
    }

    /** A pin in the given colour. Amber for open nodes, grey for closed ones. */
    @Nullable
    private Drawable markerIcon(@ColorRes int colourRes) {

        Drawable pin = ContextCompat.getDrawable(this, R.drawable.ic_place_24);
        if (pin == null) {
            return null;
        }

        // The drawable is wrapped before it is tinted. Without the wrap the
        // tint would also change every other view using the same drawable,
        // because Android shares one instance between them.
        Drawable tintable = DrawableCompat.wrap(pin.mutate());
        DrawableCompat.setTint(tintable, ContextCompat.getColor(this, colourRes));
        return tintable;
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

    private void askForLocation() {
        if (hasLocationPermission()) {
            centreOnUserAndFindNearest();
            return;
        }
        locationPermissionLauncher.launch(Manifest.permission.ACCESS_FINE_LOCATION);
    }

    private boolean hasLocationPermission() {
        return ContextCompat.checkSelfPermission(this, Manifest.permission.ACCESS_FINE_LOCATION)
                == PackageManager.PERMISSION_GRANTED;
    }

    /**
     * Finds where the phone is, centres the map there, and asks the server
     * which node is closest.
     *
     * The permission is checked again, even though the caller checked it. The
     * user can switch it off in Settings while the app is open.
     */
    private void centreOnUserAndFindNearest() {

        if (!hasLocationPermission()) {
            return;
        }

        // Location off for the whole phone means no fix is possible. Say so,
        // instead of leaving the user waiting for a card that cannot appear.
        if (!DeviceLocation.isSwitchedOn(this)) {
            Toast.makeText(this, R.string.map_location_off, Toast.LENGTH_LONG).show();
            return;
        }

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
            centreOn(location);
            loadNearest(location);
        });
    }

    @Override
    protected void onDestroy() {
        super.onDestroy();
        // Stop looking for a position nobody is waiting for any more.
        deviceLocation.cancel();
    }

    private void centreOn(@NonNull Location location) {
        GeoPoint here = new GeoPoint(location.getLatitude(), location.getLongitude());
        binding.map.getController().setZoom(NEAR_ME_ZOOM);
        binding.map.getController().animateTo(here);
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
                            Toast.makeText(OsmMapActivity.this,
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
                        Toast.makeText(OsmMapActivity.this, message, Toast.LENGTH_LONG).show();
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
