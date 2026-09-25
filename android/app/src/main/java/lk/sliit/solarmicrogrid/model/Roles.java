/*
 * ---------------------------------------------------------------------------
 * File        : Roles.java
 * Project     : Smart Solar Microgrid Trading System - SE4040 Assignment 1
 * Author      : <Your Full Name> (<IT Number>)
 * Created     : 2026-09-20
 * Description : The three role names, spelled as the Web API spells them.
 *
 * Why a constants class
 *               These strings decide which home screen opens. If
 *               GridOperator were typed by hand in five screens, one typo
 *               would send an operator to the prosumer screen. Nothing
 *               would fail loudly enough to notice. Written once, a typo
 *               is a compile error.
 *
 * Must match   Common/Roles.cs and Models/Enums/UserRole.cs in the Web API.
 *               The API sends the enum by name, so these are the exact
 *               values that arrive in the JSON.
 *
 * Reminder     The role on the phone only picks a screen. It gives no
 *               access. Every protected endpoint checks the role inside
 *               the signed token. Editing this value on a rooted phone
 *               would change the menus and not the permissions.
 * ---------------------------------------------------------------------------
 */
package lk.sliit.solarmicrogrid.model;

import androidx.annotation.Nullable;

public final class Roles {

    public static final String BACKOFFICE = "Backoffice";
    public static final String GRID_OPERATOR = "GridOperator";
    public static final String PROSUMER = "Prosumer";

    public static boolean isProsumer(@Nullable String role) {
        return PROSUMER.equals(role);
    }

    public static boolean isGridOperator(@Nullable String role) {
        return GRID_OPERATOR.equals(role);
    }

    public static boolean isBackoffice(@Nullable String role) {
        return BACKOFFICE.equals(role);
    }

    /** Utility class: never instantiated. */
    private Roles() {
    }
}
