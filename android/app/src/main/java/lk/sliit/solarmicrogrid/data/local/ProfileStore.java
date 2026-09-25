/*
 * ---------------------------------------------------------------------------
 * File        : ProfileStore.java
 * Project     : Smart Solar Microgrid Trading System - SE4040 Assignment 1
 * Author      : <Your Full Name> (<IT Number>)
 * Created     : 2026-09-20
 * Description : Keeps a copy of the profile of the signed in user, in SQLite.
 *
 * Why cache it
 *               The profile screen should show the details at once and then
 *               refresh quietly. Without a cache it shows an empty form
 *               while the network answers. It also means the details can
 *               still be read with no signal.
 *
 * Key is NIC   The assignment says the NIC is the prosumer key. So the
 *               local table uses it as the key too. The phone and the
 *               server then agree on what identifies a person.
 *
 * SOLID        Single Responsibility. It reads and writes one table. It
 *               never calls the API. Whoever fetched the profile passes it
 *               in. It works in UserProfile, not the DTO. So a change to
 *               the API reply stops at UserProfile.from().
 * ---------------------------------------------------------------------------
 */
package lk.sliit.solarmicrogrid.data.local;

import android.content.ContentValues;
import android.content.Context;
import android.database.Cursor;
import android.database.sqlite.SQLiteDatabase;

import androidx.annotation.NonNull;
import androidx.annotation.Nullable;

import lk.sliit.solarmicrogrid.model.UserProfile;

public final class ProfileStore {

    private final DbHelper dbHelper;

    public ProfileStore(@NonNull Context context) {
        this.dbHelper = new DbHelper(context);
    }

    /**
     * Saves, or overwrites, the cached profile for this user.
     */
    public void save(@NonNull UserProfile profile) {

        ContentValues values = new ContentValues();
        values.put(DbHelper.PROFILE_NIC, profile.getNic());
        values.put(DbHelper.PROFILE_FULL_NAME, profile.getFullName());
        values.put(DbHelper.PROFILE_EMAIL, profile.getEmail());
        values.put(DbHelper.PROFILE_PHONE, profile.getPhone());
        values.put(DbHelper.PROFILE_ADDRESS, profile.getAddress());
        values.put(DbHelper.PROFILE_ROLE, profile.getRole());
        values.put(DbHelper.PROFILE_STATUS, profile.getStatus());
        values.put(DbHelper.PROFILE_DEACTIVATION_REQUESTED, profile.isDeactivationRequested() ? 1 : 0);
        values.put(DbHelper.PROFILE_SOLAR_CAPACITY_KW, profile.getSolarCapacityKw());
        values.put(DbHelper.PROFILE_UPDATED_AT, profile.getUpdatedAt());

        SQLiteDatabase db = dbHelper.getWritableDatabase();

        // CONFLICT_REPLACE so a refresh updates the row that is there.
        // Without it the insert would fail on the NIC key.
        db.insertWithOnConflict(
                DbHelper.TABLE_USER_PROFILE,
                null,
                values,
                SQLiteDatabase.CONFLICT_REPLACE);
    }

    /**
     * @return the cached profile for that NIC, or null if none was saved yet.
     */
    @Nullable
    public UserProfile read(@NonNull String nic) {

        SQLiteDatabase db = dbHelper.getReadableDatabase();

        // try-with-resources closes the cursor even if a read throws.
        // A cursor left open is reported much later, on a line that has
        // nothing to do with it. That is very hard to track down.
        try (Cursor cursor = db.query(
                DbHelper.TABLE_USER_PROFILE,
                null,
                DbHelper.PROFILE_NIC + " = ?",
                new String[]{nic},
                null, null, null)) {

            if (!cursor.moveToFirst()) {
                return null;
            }

            return new UserProfile(
                    text(cursor, DbHelper.PROFILE_NIC),
                    text(cursor, DbHelper.PROFILE_FULL_NAME),
                    text(cursor, DbHelper.PROFILE_EMAIL),
                    text(cursor, DbHelper.PROFILE_PHONE),
                    text(cursor, DbHelper.PROFILE_ADDRESS),
                    text(cursor, DbHelper.PROFILE_ROLE),
                    text(cursor, DbHelper.PROFILE_STATUS),
                    cursor.getInt(cursor.getColumnIndexOrThrow(
                            DbHelper.PROFILE_DEACTIVATION_REQUESTED)) == 1,
                    cursor.getDouble(cursor.getColumnIndexOrThrow(
                            DbHelper.PROFILE_SOLAR_CAPACITY_KW)),
                    text(cursor, DbHelper.PROFILE_UPDATED_AT));
        }
    }

    /**
     * Removes every cached profile.
     *
     * Called on sign out. The next person to use the phone must not be able
     * to read the details of the last user.
     */
    public void clear() {
        SQLiteDatabase db = dbHelper.getWritableDatabase();
        db.delete(DbHelper.TABLE_USER_PROFILE, null, null);
    }

    @NonNull
    private static String text(@NonNull Cursor cursor, @NonNull String columnName) {
        String value = cursor.getString(cursor.getColumnIndexOrThrow(columnName));
        return value == null ? "" : value;
    }
}
