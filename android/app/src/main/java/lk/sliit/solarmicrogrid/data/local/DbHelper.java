/*
 * ---------------------------------------------------------------------------
 * File        : DbHelper.java
 * Project     : Smart Solar Microgrid Trading System - SE4040 Assignment 1
 * Author      : <Your Full Name> (<IT Number>)
 * Created     : 2026-09-20
 * Description : Creates and upgrades the SQLite database on the phone.
 *
 * The assignment asks for local SQLite storage. This is where that is done.
 * Three tables:
 *
 *   session       who is signed in on this phone, and their token.
 *                 One row, ever. Keeping it in the database, not in memory,
 *                 is what lets the app reopen without asking for a password.
 *
 *   user_profile  the details of the signed in user. The key is the NIC,
 *                 because the assignment says NIC is the prosumer key.
 *                 It is cached so the profile screen can draw at once and
 *                 then refresh. Otherwise the form sits empty while the
 *                 network answers.
 *
 *   station_cache the nodes downloaded last time. The map then still has
 *                 markers to draw when the signal drops.
 *
 * What is NOT stored here
 *               Passwords. The phone never holds one. Signing in swaps the
 *               password for a token that expires. Only the token is kept.
 *               If the phone is lost, the damage stops when the token does.
 *
 * SOLID        Single Responsibility. Schema only. No screen talks to this
 *               class. The three Store classes wrap it and offer clear
 *               methods like saveSession() instead of raw SQL.
 * ---------------------------------------------------------------------------
 */
package lk.sliit.solarmicrogrid.data.local;

import android.content.Context;
import android.database.sqlite.SQLiteDatabase;
import android.database.sqlite.SQLiteOpenHelper;

import androidx.annotation.NonNull;

public final class DbHelper extends SQLiteOpenHelper {

    private static final String DATABASE_NAME = "solar_microgrid.db";

    /**
     * Raise this by one whenever a CREATE TABLE statement below changes, so
     * that onUpgrade runs on devices holding the older shape. Forgetting to
     * raise it is the usual cause of "no such column" after a schema edit.
     */
    private static final int DATABASE_VERSION = 2;

    // ----- session ---------------------------------------------------------
    public static final String TABLE_SESSION = "session";
    public static final String SESSION_ID = "id";
    public static final String SESSION_TOKEN = "token";
    public static final String SESSION_EXPIRES_AT_UTC = "expires_at_utc";
    public static final String SESSION_USER_ID = "user_id";
    public static final String SESSION_NIC = "nic";
    public static final String SESSION_FULL_NAME = "full_name";
    public static final String SESSION_EMAIL = "email";
    public static final String SESSION_ROLE = "role";

    // ----- user_profile ----------------------------------------------------
    public static final String TABLE_USER_PROFILE = "user_profile";
    public static final String PROFILE_NIC = "nic";
    public static final String PROFILE_FULL_NAME = "full_name";
    public static final String PROFILE_EMAIL = "email";
    public static final String PROFILE_PHONE = "phone";
    public static final String PROFILE_ADDRESS = "address";
    public static final String PROFILE_ROLE = "role";
    public static final String PROFILE_STATUS = "status";
    public static final String PROFILE_DEACTIVATION_REQUESTED = "deactivation_requested";
    public static final String PROFILE_SOLAR_CAPACITY_KW = "solar_capacity_kw";
    public static final String PROFILE_UPDATED_AT = "updated_at";

    // ----- station_cache ---------------------------------------------------
    public static final String TABLE_STATION_CACHE = "station_cache";
    public static final String STATION_ID = "id";
    public static final String STATION_CODE = "station_code";
    public static final String STATION_NAME = "name";
    public static final String STATION_LATITUDE = "latitude";
    public static final String STATION_LONGITUDE = "longitude";
    public static final String STATION_ADDRESS_LINE = "address_line";
    public static final String STATION_CAPACITY_KWH = "capacity_kwh";
    public static final String STATION_TOTAL_SLOTS = "total_battery_slots";
    public static final String STATION_AVAILABLE_SLOTS = "available_battery_slots";
    public static final String STATION_IS_ACTIVE = "is_active";
    public static final String STATION_CACHED_AT_UTC = "cached_at_utc";

    /*
     * CHECK (id = 1) keeps the rule of one signed in user in the database,
     * not in Java. Two sessions cannot exist, even if a bug tried to add one.
     * So the question of whose token this is never comes up.
     */
    private static final String CREATE_SESSION =
            "CREATE TABLE " + TABLE_SESSION + " (" +
                    SESSION_ID + " INTEGER PRIMARY KEY CHECK (" + SESSION_ID + " = 1), " +
                    SESSION_TOKEN + " TEXT NOT NULL, " +
                    SESSION_EXPIRES_AT_UTC + " TEXT NOT NULL, " +
                    SESSION_USER_ID + " TEXT NOT NULL, " +
                    SESSION_NIC + " TEXT NOT NULL, " +
                    SESSION_FULL_NAME + " TEXT NOT NULL, " +
                    SESSION_EMAIL + " TEXT NOT NULL, " +
                    SESSION_ROLE + " TEXT NOT NULL)";

    private static final String CREATE_USER_PROFILE =
            "CREATE TABLE " + TABLE_USER_PROFILE + " (" +
                    PROFILE_NIC + " TEXT PRIMARY KEY, " +
                    PROFILE_FULL_NAME + " TEXT NOT NULL, " +
                    PROFILE_EMAIL + " TEXT NOT NULL, " +
                    PROFILE_PHONE + " TEXT, " +
                    PROFILE_ADDRESS + " TEXT, " +
                    PROFILE_ROLE + " TEXT NOT NULL, " +
                    PROFILE_STATUS + " TEXT NOT NULL, " +
                    PROFILE_DEACTIVATION_REQUESTED + " INTEGER NOT NULL DEFAULT 0, " +
                    PROFILE_SOLAR_CAPACITY_KW + " REAL NOT NULL DEFAULT 0, " +
                    PROFILE_UPDATED_AT + " TEXT)";

    private static final String CREATE_STATION_CACHE =
            "CREATE TABLE " + TABLE_STATION_CACHE + " (" +
                    STATION_ID + " TEXT PRIMARY KEY, " +
                    STATION_CODE + " TEXT NOT NULL, " +
                    STATION_NAME + " TEXT NOT NULL, " +
                    STATION_LATITUDE + " REAL NOT NULL, " +
                    STATION_LONGITUDE + " REAL NOT NULL, " +
                    STATION_ADDRESS_LINE + " TEXT, " +
                    STATION_CAPACITY_KWH + " REAL NOT NULL DEFAULT 0, " +
                    STATION_TOTAL_SLOTS + " INTEGER NOT NULL DEFAULT 0, " +
                    STATION_AVAILABLE_SLOTS + " INTEGER NOT NULL DEFAULT 0, " +
                    STATION_IS_ACTIVE + " INTEGER NOT NULL DEFAULT 1, " +
                    STATION_CACHED_AT_UTC + " TEXT NOT NULL)";

    public DbHelper(Context context) {
        // getApplicationContext() so this helper can never hold on to a
        // closed screen. That is a common and easily missed memory leak.
        super(context.getApplicationContext(), DATABASE_NAME, null, DATABASE_VERSION);
    }

    @Override
    public void onCreate(@NonNull SQLiteDatabase db) {
        db.execSQL(CREATE_SESSION);
        db.execSQL(CREATE_USER_PROFILE);
        db.execSQL(CREATE_STATION_CACHE);
    }

    @Override
    public void onUpgrade(@NonNull SQLiteDatabase db, int oldVersion, int newVersion) {
        /*
         * Dropping and creating again is safe here. Every table holds a copy
         * of something the server owns. The user can sign in again. The
         * caches can be downloaded again. Nothing is lost that exists only on
         * the phone.
         *
         * A table holding data made offline would need a real migration.
         */
        db.execSQL("DROP TABLE IF EXISTS " + TABLE_STATION_CACHE);
        db.execSQL("DROP TABLE IF EXISTS " + TABLE_USER_PROFILE);
        db.execSQL("DROP TABLE IF EXISTS " + TABLE_SESSION);
        onCreate(db);
    }
}
