/*
 * ---------------------------------------------------------------------------
 * File        : Navigation.java
 * Project     : Smart Solar Microgrid Trading System - SE4040 Assignment 1
 * Author      : <Your Full Name> (<IT Number>)
 * Created     : 2026-09-20
 * Description : Decides which screen a signed in user belongs on, and takes
 *               them there.
 *
 * Why one place
 *               Role routing happens after login. It happens again every
 *               time the app is opened. Written in both places it would
 *               slowly differ, usually after a new role is added. Then an
 *               operator would land on the prosumer screen.
 *               Here it is decided once.
 *
 * Why the task is cleared
 *               CLEAR_TASK with NEW_TASK replaces the whole back stack.
 *               Without it, Back after signing in would return to the login
 *               screen. Back after signing out would return to the
 *               dashboard of the user who just left. That looks a lot like
 *               still being signed in.
 *
 * Reminder     Routing by role picks a screen. It gives no access. Every
 *               protected endpoint checks the role inside the signed token
 *               on the server.
 * ---------------------------------------------------------------------------
 */
package lk.sliit.solarmicrogrid.ui;

import android.app.Activity;
import android.content.Intent;

import androidx.annotation.NonNull;

import lk.sliit.solarmicrogrid.model.Session;
import lk.sliit.solarmicrogrid.ui.auth.LoginActivity;
import lk.sliit.solarmicrogrid.ui.home.OperatorHomeActivity;
import lk.sliit.solarmicrogrid.ui.home.ProsumerHomeActivity;

public final class Navigation {

    /** Opens the home screen for this user. Closes everything behind it. */
    public static void toHome(@NonNull Activity from, @NonNull Session session) {

        Class<? extends Activity> destination = session.isGridOperator()
                ? OperatorHomeActivity.class
                : ProsumerHomeActivity.class;

        replaceStack(from, new Intent(from, destination));
    }

    /** Goes back to the sign in screen. Closes everything behind it. */
    public static void toLogin(@NonNull Activity from) {
        replaceStack(from, new Intent(from, LoginActivity.class));
    }

    private static void replaceStack(@NonNull Activity from, @NonNull Intent intent) {
        intent.addFlags(Intent.FLAG_ACTIVITY_NEW_TASK | Intent.FLAG_ACTIVITY_CLEAR_TASK);
        from.startActivity(intent);
        from.finish();
    }

    /** Utility class: never instantiated. */
    private Navigation() {
    }
}
