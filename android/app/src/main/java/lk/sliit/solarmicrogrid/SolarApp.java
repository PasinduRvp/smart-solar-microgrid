/*
 * ---------------------------------------------------------------------------
 * File        : SolarApp.java
 * Project     : Smart Solar Microgrid Trading System - SE4040 Assignment 1
 * Author      : <Your Full Name> (<IT Number>)
 * Created     : 2026-09-20
 * Description : The application object. It runs once, before any screen opens.
 *               It is where the parts of the app are joined together.
 *
 * This is the composition root. It is the Android version of Program.cs in
 * the Web API. It is the only place that knows SessionStore is what gives
 * the token to AuthInterceptor. Every other class is handed what it needs
 * and does not know where it came from. That is what makes each of them
 * easy to test on its own.
 *
 * It is listed in AndroidManifest.xml as android:name=".SolarApp".
 * Without that line Android uses the default Application class and none of
 * this runs. The first sign is the error:
 * ApiClient.init() was never called.
 *
 * Why the stores are made here
 *               Each store opens the same SQLite database. Making them once
 *               and sharing them stops every screen opening its own handle.
 * ---------------------------------------------------------------------------
 */
package lk.sliit.solarmicrogrid;

import android.app.Application;
import android.content.Intent;
import android.os.Handler;
import android.os.Looper;
import android.widget.Toast;

import androidx.annotation.NonNull;

import lk.sliit.solarmicrogrid.data.local.ProfileStore;
import lk.sliit.solarmicrogrid.data.local.SessionStore;
import lk.sliit.solarmicrogrid.data.local.StationStore;
import lk.sliit.solarmicrogrid.data.remote.ApiClient;
import lk.sliit.solarmicrogrid.data.remote.SessionExpiredHandler;
import lk.sliit.solarmicrogrid.ui.auth.LoginActivity;

public final class SolarApp extends Application implements SessionExpiredHandler {

    private static SolarApp instance;

    private SessionStore sessionStore;
    private ProfileStore profileStore;
    private StationStore stationStore;

    @Override
    public void onCreate() {
        super.onCreate();
        instance = this;

        sessionStore = new SessionStore(this);
        profileStore = new ProfileStore(this);
        stationStore = new StationStore(this);

        // The network layer is told where to get a token, and who to tell
        // when that token stops working. It is not told that tokens live in
        // SQLite, or that screens exist.
        ApiClient.init(sessionStore, this);
    }

    @NonNull
    public static SolarApp get() {
        if (instance == null) {
            throw new IllegalStateException(
                    "SolarApp has not started. Check android:name=\".SolarApp\" "
                            + "is present on <application> in AndroidManifest.xml.");
        }
        return instance;
    }

    @NonNull
    public SessionStore sessions() {
        return sessionStore;
    }

    @NonNull
    public ProfileStore profiles() {
        return profileStore;
    }

    @NonNull
    public StationStore stations() {
        return stationStore;
    }

    /**
     * Called when the server refuses the saved token.
     *
     * A token lasts two hours. After that every call fails, and there is
     * nothing the user can do on the screen they are on. So the session is
     * cleared and the sign in screen is opened, with a line saying why.
     *
     * This arrives on a background thread, because it comes from the HTTP
     * stack. Toast and startActivity have to run on the main thread, so the
     * work is posted there.
     */
    @Override
    public void onSessionExpired() {

        new Handler(Looper.getMainLooper()).post(() -> {

            signOut();

            Toast.makeText(this, R.string.session_expired, Toast.LENGTH_LONG).show();

            // NEW_TASK is required to start a screen from an Application.
            // CLEAR_TASK removes whatever the user was looking at, so Back
            // cannot return to a screen that can no longer load anything.
            Intent signIn = new Intent(this, LoginActivity.class);
            signIn.addFlags(Intent.FLAG_ACTIVITY_NEW_TASK | Intent.FLAG_ACTIVITY_CLEAR_TASK);
            startActivity(signIn);
        });
    }

    /**
     * Signs the user out. Removes everything kept about them on this phone.
     *
     * This is one method, not three calls from the sign out button. A table
     * added later cannot then be forgotten and left holding the data of the
     * user who just left.
     */
    public void signOut() {
        sessionStore.clear();
        profileStore.clear();
        stationStore.clear();
    }
}
