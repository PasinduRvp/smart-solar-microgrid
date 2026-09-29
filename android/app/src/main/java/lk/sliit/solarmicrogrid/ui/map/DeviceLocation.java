/*
 * ---------------------------------------------------------------------------
 * File        : DeviceLocation.java
 * Project     : Smart Solar Microgrid Trading System - SE4040 Assignment 1
 * Author      : NIMSARA R V P P (IT 23215306)
 * Created     : 2026-09-20
 * Description : Finds where the phone is. Used by the map and the list.
 *
 * Why not just getLastLocation()
 *              getLastLocation() returns the last fix any app on the phone
 *              made. It is instant when it works. But it returns null often:
 *              on a phone that was just switched on, on a phone that has not
 *              used a map app in days, and always when Location is off in
 *              Settings.
 *
 *              So this class tries the fast way first. If that gives nothing,
 *              it asks for a fresh fix. If that also fails, it says the
 *              location is not known, and the screen carries on without it.
 *
 * Why one class
 *              Both the map screen and the list screen need this. Written
 *              twice it would drift, and one of them would keep the old
 *              behaviour after a fix.
 *
 * SOLID       Single Responsibility. It finds a location. It shows nothing
 *              and calls no API.
 * ---------------------------------------------------------------------------
 */
package lk.sliit.solarmicrogrid.ui.map;

import android.Manifest;
import android.annotation.SuppressLint;
import android.app.Activity;
import android.content.Context;
import android.content.pm.PackageManager;
import android.location.Location;
import android.location.LocationManager;

import androidx.annotation.NonNull;
import androidx.annotation.Nullable;
import androidx.core.content.ContextCompat;

import com.google.android.gms.location.FusedLocationProviderClient;
import com.google.android.gms.location.LocationServices;
import com.google.android.gms.location.Priority;
import com.google.android.gms.tasks.CancellationTokenSource;

public final class DeviceLocation {

    /** What the screen is told once the search finishes. */
    public interface Callback {

        /**
         * @param location where the phone is, or null when it could not be
         *                 found. A null is normal and is not an error.
         */
        void onLocation(@Nullable Location location);
    }

    private final Activity activity;
    private final FusedLocationProviderClient client;

    /** Lets a pending request be dropped when the screen closes. */
    private final CancellationTokenSource cancellation = new CancellationTokenSource();

    public DeviceLocation(@NonNull Activity activity) {
        this.activity = activity;
        this.client = LocationServices.getFusedLocationProviderClient(activity);
    }

    /** True once the user has allowed the app to use location. */
    public static boolean hasPermission(@NonNull Context context) {
        return ContextCompat.checkSelfPermission(context, Manifest.permission.ACCESS_FINE_LOCATION)
                == PackageManager.PERMISSION_GRANTED;
    }

    /**
     * True when Location is switched on in the phone Settings.
     *
     * This is not the same as the app permission. The user can allow the app
     * and still have Location off for the whole phone. Then no fix is ever
     * possible, and the screen should say so instead of waiting.
     */
    public static boolean isSwitchedOn(@NonNull Context context) {
        LocationManager manager =
                (LocationManager) context.getSystemService(Context.LOCATION_SERVICE);
        if (manager == null) {
            return false;
        }
        return manager.isProviderEnabled(LocationManager.GPS_PROVIDER)
                || manager.isProviderEnabled(LocationManager.NETWORK_PROVIDER);
    }

    /**
     * Finds the phone position, then calls back on the main thread.
     *
     * The permission is checked here as well as by the caller. The user can
     * switch it off in Settings while the app is open, and Android throws a
     * SecurityException if it is used without permission.
     */
    @SuppressLint("MissingPermission")
    public void find(@NonNull Callback callback) {

        if (!hasPermission(activity) || !isSwitchedOn(activity)) {
            callback.onLocation(null);
            return;
        }

        client.getLastLocation()
                .addOnSuccessListener(activity, lastKnown -> {
                    if (lastKnown != null) {
                        callback.onLocation(lastKnown);
                        return;
                    }
                    // Nothing stored. Ask for a fresh fix.
                    requestFreshFix(callback);
                })
                .addOnFailureListener(activity, error -> requestFreshFix(callback));
    }

    /**
     * Asks the phone to work out where it is now.
     *
     * BALANCED_POWER_ACCURACY rather than HIGH_ACCURACY. It uses the network
     * and wifi as well as GPS, so it answers in a few seconds indoors. GPS
     * alone can take a minute inside a building, or never answer at all.
     *
     * A few hundred metres of error does not matter here. The result is only
     * used to sort nodes that are kilometres apart.
     */
    @SuppressLint("MissingPermission")
    private void requestFreshFix(@NonNull Callback callback) {
        client.getCurrentLocation(Priority.PRIORITY_BALANCED_POWER_ACCURACY,
                        cancellation.getToken())
                .addOnSuccessListener(activity, callback::onLocation)
                .addOnFailureListener(activity, error -> callback.onLocation(null));
    }

    /**
     * Drops a request that is still running.
     *
     * Called when the screen closes, so the phone does not keep looking for a
     * position nobody is waiting for.
     */
    public void cancel() {
        cancellation.cancel();
    }
}
