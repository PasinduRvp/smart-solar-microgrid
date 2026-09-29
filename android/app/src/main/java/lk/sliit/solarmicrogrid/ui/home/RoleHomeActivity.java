/*
 * ---------------------------------------------------------------------------
 * File        : RoleHomeActivity.java
 * Project     : Smart Solar Microgrid Trading System - SE4040 Assignment 1
 * Author      : M T A J Yapa (IT 23278530)
 * Created     : 2026-09-20
 * Description : What the Prosumer and Grid Operator home screens share:
 *               who is signed in, opening the profile and the map, and
 *               signing out.
 *
 * OOP          This is the Template Method pattern. The base class fixes
 *               the steps every home screen follows. Read the session.
 *               Guard against there being none. Fill in the header. Wire
 *               the buttons. It leaves one choice to the subclass: its
 *               title. A new role later means a new subclass and one
 *               method, not a copy of this file.
 *
 * SOLID        Open/Closed. Open to a new role by subclassing. Closed to
 *               editing what already works.
 * SOLID        Liskov Substitution. Navigation treats both subclasses as
 *               an Activity. Neither changes what it relies on.
 *
 * Why the session is read again in onStart
 *               Signing out clears the database. The profile screen can
 *               change the name on show. Reading again each time the screen
 *               comes to the front means neither an old name nor a cleared
 *               session is left on display.
 * ---------------------------------------------------------------------------
 */
package lk.sliit.solarmicrogrid.ui.home;

import android.content.Intent;
import android.os.Bundle;

import androidx.annotation.NonNull;
import androidx.annotation.Nullable;
import androidx.annotation.StringRes;

import lk.sliit.solarmicrogrid.R;
import lk.sliit.solarmicrogrid.SolarApp;
import lk.sliit.solarmicrogrid.databinding.ActivityRoleHomeBinding;
import lk.sliit.solarmicrogrid.model.Session;
import lk.sliit.solarmicrogrid.ui.BaseActivity;
import lk.sliit.solarmicrogrid.ui.Navigation;
import lk.sliit.solarmicrogrid.ui.booking.MyBookingsActivity;
import lk.sliit.solarmicrogrid.ui.map.NearbyNodesActivity;
import lk.sliit.solarmicrogrid.ui.profile.ProfileActivity;
import lk.sliit.solarmicrogrid.util.TextFormat;

public abstract class RoleHomeActivity extends BaseActivity {

    protected ActivityRoleHomeBinding binding;

    /**
     * The heading this home screen shows.
     * It is the one choice each subclass makes.
     */
    @StringRes
    protected abstract int titleResource();

    @Override
    protected void onCreate(@Nullable Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);

        binding = ActivityRoleHomeBinding.inflate(getLayoutInflater());
        setContentView(binding.getRoot());
        applySystemBarInsets(binding.root);

        binding.toolbar.setTitle(titleResource());

        binding.btnMyProfile.setOnClickListener(view ->
                startActivity(new Intent(this, ProfileActivity.class)));

        // This opens the list, not the map. The list needs no Google Maps
        // key, so it works on every phone. The map is one tap away from
        // there for anyone who has added a key.
        binding.btnNearbyNodes.setOnClickListener(view ->
                startActivity(new Intent(this, NearbyNodesActivity.class)));

        // My bookings belongs to a prosumer. The operator screen hides it
        // and puts the scan card in the same slot.
        binding.btnMyBookings.setOnClickListener(view ->
                startActivity(new Intent(this, MyBookingsActivity.class)));

        binding.btnSignOut.setOnClickListener(view -> signOut());
    }

    @Override
    protected void onStart() {
        super.onStart();

        Session session = SolarApp.get().sessions().read();

        // No session means the app was opened again after signing out.
        // Or the session was cleared while this screen was in the background.
        // An empty dashboard would be worse than going back to sign in.
        if (session == null) {
            Navigation.toLogin(this);
            return;
        }

        showSession(session);
    }

    /**
     * Fills in the header.
     * A subclass can override it to add more, without repeating this part.
     */
    protected void showSession(@NonNull Session session) {
        binding.tvAvatar.setText(TextFormat.initialsOf(session.getFullName()));
        binding.tvSignedInAs.setText(session.getFullName());
        binding.tvNic.setText(getString(R.string.home_nic_label, session.getNic()));
        binding.tvRole.setText(getString(R.string.home_role_label, session.getRole()));
    }

    /**
     * Clears the session and every cached copy of the data of this user.
     * Then goes back to sign in.
     */
    private void signOut() {
        SolarApp.get().signOut();
        Navigation.toLogin(this);
    }
}
