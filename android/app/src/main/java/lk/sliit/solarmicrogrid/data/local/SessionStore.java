/*
 * ---------------------------------------------------------------------------
 * File        : SessionStore.java
 * Project     : Smart Solar Microgrid Trading System - SE4040 Assignment 1
 * Author      : <Your Full Name> (<IT Number>)
 * Created     : 2026-09-20
 * Description : Saves, reads and clears the signed in user, in SQLite.
 *
 * This is what makes the session survive closing the app.
 * It also implements TokenProvider. That is how AuthInterceptor gets the
 * token without the two classes knowing about each other.
 *
 * SOLID        Single Responsibility. One table, three jobs.
 * SOLID        Dependency Inversion. The network layer depends on the
 *               TokenProvider interface, never on this class.
 *
 * Why one row  save() replaces instead of adding. Signing in as another
 *               user cannot leave the old token behind. The CHECK (id = 1)
 *               rule in DbHelper makes that certain in the database too.
 * ---------------------------------------------------------------------------
 */
package lk.sliit.solarmicrogrid.data.local;

import android.content.ContentValues;
import android.content.Context;
import android.database.Cursor;
import android.database.sqlite.SQLiteDatabase;

import androidx.annotation.NonNull;
import androidx.annotation.Nullable;

import lk.sliit.solarmicrogrid.data.remote.TokenProvider;
import lk.sliit.solarmicrogrid.data.remote.dto.AuthResponse;
import lk.sliit.solarmicrogrid.model.Session;

public final class SessionStore implements TokenProvider {

    /** There is only ever one session row, and this is its key. */
    private static final int SINGLE_ROW_ID = 1;

    private final DbHelper dbHelper;

    public SessionStore(@NonNull Context context) {
        this.dbHelper = new DbHelper(context);
    }

    /** Saves a successful login. Any old session is replaced. */
    public void save(@NonNull AuthResponse response) {

        ContentValues values = new ContentValues();
        values.put(DbHelper.SESSION_ID, SINGLE_ROW_ID);
        values.put(DbHelper.SESSION_TOKEN, response.getToken());
        values.put(DbHelper.SESSION_EXPIRES_AT_UTC, response.getExpiresAtUtc());
        values.put(DbHelper.SESSION_USER_ID, response.getUserId());
        values.put(DbHelper.SESSION_NIC, response.getNic());
        values.put(DbHelper.SESSION_FULL_NAME, response.getFullName());
        values.put(DbHelper.SESSION_EMAIL, response.getEmail());
        values.put(DbHelper.SESSION_ROLE, response.getRole());

        SQLiteDatabase db = dbHelper.getWritableDatabase();

        // CONFLICT_REPLACE makes a second login update the same row.
        // Without it the insert would fail on the key.
        db.insertWithOnConflict(
                DbHelper.TABLE_SESSION,
                null,
                values,
                SQLiteDatabase.CONFLICT_REPLACE);
    }

    /**
     * @return the stored session, or null when nobody is signed in.
     */
    @Nullable
    public Session read() {

        SQLiteDatabase db = dbHelper.getReadableDatabase();

        // try-with-resources closes the cursor even if a read throws.
        // A cursor left open is reported much later, on a line that has
        // nothing to do with it. That is very hard to track down.
        try (Cursor cursor = db.query(
                DbHelper.TABLE_SESSION,
                null,
                DbHelper.SESSION_ID + " = ?",
                new String[]{String.valueOf(SINGLE_ROW_ID)},
                null, null, null)) {

            if (!cursor.moveToFirst()) {
                return null;
            }

            return new Session(
                    textAt(cursor, DbHelper.SESSION_TOKEN),
                    textAt(cursor, DbHelper.SESSION_EXPIRES_AT_UTC),
                    textAt(cursor, DbHelper.SESSION_USER_ID),
                    textAt(cursor, DbHelper.SESSION_NIC),
                    textAt(cursor, DbHelper.SESSION_FULL_NAME),
                    textAt(cursor, DbHelper.SESSION_EMAIL),
                    textAt(cursor, DbHelper.SESSION_ROLE));
        }
    }

    /**
     * @return true when a session exists on this phone.
     *
     * This answers one question. Open the login screen, or the dashboard?
     *
     * It does not check whether the token has expired. The server decides
     * that, and it says so with a 401.
     */
    public boolean isSignedIn() {
        return read() != null;
    }

    /**
     * Signs out.
     *
     * Called when the user taps Sign out. Also called when the server answers
     * 401, which means the token is no longer accepted.
     */
    public void clear() {
        SQLiteDatabase db = dbHelper.getWritableDatabase();
        db.delete(DbHelper.TABLE_SESSION, null, null);
    }

    /**
     * Gives AuthInterceptor the token of the current user.
     *
     * This runs on a background thread for every request. That is safe.
     * SQLite on Android handles one connection at a time.
     */
    @Nullable
    @Override
    public String getBearerToken() {
        Session session = read();
        return session == null ? null : session.getToken();
    }

    @NonNull
    private static String textAt(@NonNull Cursor cursor, @NonNull String columnName) {
        int index = cursor.getColumnIndexOrThrow(columnName);
        String value = cursor.getString(index);
        return value == null ? "" : value;
    }
}
