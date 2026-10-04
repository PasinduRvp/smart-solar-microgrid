/*
 * ---------------------------------------------------------------------------
 * File        : MainActivity.java
 * Project     : Smart Solar Microgrid Trading System - SE4040 Assignment 1
 * Author      : M T A J Yapa (IT 23278530)
 * Created     : 2026-09-20
 * Description : The first screen. It shows nothing and decides where to go.
 *               Task B3.
 *
 * This is where saved sessions become visible to the user. The app asks
 * SQLite whether anyone is signed in:
 *
 *   no session  -> the sign in screen
 *   a session   -> straight to that role home screen, no password needed
 *
 * That is the whole point of keeping the session on the phone. Closing the
 * app and opening it again does not mean signing in again.
 *
 * Why this screen closes itself
 *               Navigation clears the task, so this screen is removed from
 *               the back stack. Without that, pressing Back from the
 *               dashboard would land on a blank screen that at once
 *               redirects. Back would look broken.
 *
 * A Backoffice session is signed out here as well as in LoginActivity.
 * It should not happen. But a role can be changed on the server after a
 * user has signed in on the phone. This is when that would be noticed.
 * ---------------------------------------------------------------------------
 */
package lk.sliit.solarmicrogrid;

import android.os.Bundle;

import androidx.annotation.Nullable;

import lk.sliit.solarmicrogrid.databinding.ActivityMainBinding;
import lk.sliit.solarmicrogrid.model.Session;
import lk.sliit.solarmicrogrid.ui.BaseActivity;
import lk.sliit.solarmicrogrid.ui.Navigation;

public final class MainActivity extends BaseActivity {

    private ActivityMainBinding binding;

    @Override
    protected void onCreate(@Nullable Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);

        binding = ActivityMainBinding.inflate(getLayoutInflater());
        setContentView(binding.getRoot());
        applySystemBarInsets(binding.main);

        routeToNextScreen();
    }

    private void routeToNextScreen() {

        Session session = SolarApp.get().sessions().read();

        if (session == null) {
            Navigation.toLogin(this);
            return;
        }

        if (session.isBackoffice()) {
            SolarApp.get().signOut();
            Navigation.toLogin(this);
            return;
        }

        Navigation.toHome(this, session);
    }
}
