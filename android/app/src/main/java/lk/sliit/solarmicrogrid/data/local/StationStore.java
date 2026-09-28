/*
 * ---------------------------------------------------------------------------
 * File        : StationStore.java
 * Project     : Smart Solar Microgrid Trading System - SE4040 Assignment 1
 * Author      : NIMSARA R V P P (IT 23215306)
 * Created     : 2026-09-20
 * Description : Caches the microgrid nodes downloaded from the Web API.
 *
 * Why cache them
 *               The map needs coordinates to draw markers. Fetching them
 *               again on every pan, or on every reopen, would be slow. It
 *               would also leave the map blank when the signal drops.
 *               The cache lets the map draw at once from SQLite while a
 *               fresh copy is fetched in the background.
 *
 * Why replaceAll and not merge
 *               A node a Backoffice officer deleted must also go from the
 *               phone. Inserting downloaded nodes one by one would leave
 *               the deleted one behind for ever.
 *               Replacing the whole set keeps the cache equal to what the
 *               server last said. Doing it in a transaction means a failure
 *               halfway through leaves the old rows, not an empty table.
 *
 * SOLID        Single Responsibility. One table. No HTTP. No map code.
 * ---------------------------------------------------------------------------
 */
package lk.sliit.solarmicrogrid.data.local;

import android.content.ContentValues;
import android.content.Context;
import android.database.Cursor;
import android.database.sqlite.SQLiteDatabase;

import androidx.annotation.NonNull;

import java.text.SimpleDateFormat;
import java.util.ArrayList;
import java.util.Date;
import java.util.List;
import java.util.Locale;
import java.util.TimeZone;

import lk.sliit.solarmicrogrid.model.MicrogridNode;

public final class StationStore {

    private final DbHelper dbHelper;

    public StationStore(@NonNull Context context) {
        this.dbHelper = new DbHelper(context);
    }

    /**
     * Replaces the whole cache with the nodes just downloaded.
     */
    public void replaceAll(@NonNull List<MicrogridNode> nodes) {

        String cachedAtUtc = nowAsIsoUtc();
        SQLiteDatabase db = dbHelper.getWritableDatabase();

        db.beginTransaction();
        try {
            db.delete(DbHelper.TABLE_STATION_CACHE, null, null);

            for (MicrogridNode node : nodes) {
                db.insertWithOnConflict(
                        DbHelper.TABLE_STATION_CACHE,
                        null,
                        toRow(node, cachedAtUtc),
                        SQLiteDatabase.CONFLICT_REPLACE);
            }

            // Nothing is written until this line runs.
            // Forgetting it is a common SQLite bug. No error, and no data.
            db.setTransactionSuccessful();
        } finally {
            db.endTransaction();
        }
    }

    /**
     * @return every cached node, sorted by name.
     *         Sorting keeps the order the same on every screen.
     */
    @NonNull
    public List<MicrogridNode> readAll() {

        List<MicrogridNode> nodes = new ArrayList<>();
        SQLiteDatabase db = dbHelper.getReadableDatabase();

        try (Cursor cursor = db.query(
                DbHelper.TABLE_STATION_CACHE,
                null, null, null, null, null,
                DbHelper.STATION_NAME + " ASC")) {

            while (cursor.moveToNext()) {
                nodes.add(fromRow(cursor));
            }
        }
        return nodes;
    }

    /**
     * @return how many nodes are cached.
     *         Tells the map whether it can draw now, or must wait for the
     *         first download.
     */
    public int count() {
        SQLiteDatabase db = dbHelper.getReadableDatabase();
        try (Cursor cursor = db.rawQuery(
                "SELECT COUNT(*) FROM " + DbHelper.TABLE_STATION_CACHE, null)) {
            return cursor.moveToFirst() ? cursor.getInt(0) : 0;
        }
    }

    public void clear() {
        SQLiteDatabase db = dbHelper.getWritableDatabase();
        db.delete(DbHelper.TABLE_STATION_CACHE, null, null);
    }

    @NonNull
    private static ContentValues toRow(@NonNull MicrogridNode node, @NonNull String cachedAtUtc) {
        ContentValues values = new ContentValues();
        values.put(DbHelper.STATION_ID, node.getId());
        values.put(DbHelper.STATION_CODE, node.getStationCode());
        values.put(DbHelper.STATION_NAME, node.getName());
        values.put(DbHelper.STATION_LATITUDE, node.getLatitude());
        values.put(DbHelper.STATION_LONGITUDE, node.getLongitude());
        values.put(DbHelper.STATION_ADDRESS_LINE, node.getAddressLine());
        values.put(DbHelper.STATION_CAPACITY_KWH, node.getCapacityKwh());
        values.put(DbHelper.STATION_TOTAL_SLOTS, node.getTotalBatterySlots());
        values.put(DbHelper.STATION_AVAILABLE_SLOTS, node.getAvailableBatterySlots());
        values.put(DbHelper.STATION_IS_ACTIVE, node.isActive() ? 1 : 0);
        values.put(DbHelper.STATION_CACHED_AT_UTC, cachedAtUtc);
        return values;
    }

    @NonNull
    private static MicrogridNode fromRow(@NonNull Cursor cursor) {
        return new MicrogridNode(
                text(cursor, DbHelper.STATION_ID),
                text(cursor, DbHelper.STATION_CODE),
                text(cursor, DbHelper.STATION_NAME),
                cursor.getDouble(cursor.getColumnIndexOrThrow(DbHelper.STATION_LATITUDE)),
                cursor.getDouble(cursor.getColumnIndexOrThrow(DbHelper.STATION_LONGITUDE)),
                text(cursor, DbHelper.STATION_ADDRESS_LINE),
                cursor.getDouble(cursor.getColumnIndexOrThrow(DbHelper.STATION_CAPACITY_KWH)),
                cursor.getInt(cursor.getColumnIndexOrThrow(DbHelper.STATION_TOTAL_SLOTS)),
                cursor.getInt(cursor.getColumnIndexOrThrow(DbHelper.STATION_AVAILABLE_SLOTS)),
                cursor.getInt(cursor.getColumnIndexOrThrow(DbHelper.STATION_IS_ACTIVE)) == 1);
    }

    @NonNull
    private static String text(@NonNull Cursor cursor, @NonNull String columnName) {
        String value = cursor.getString(cursor.getColumnIndexOrThrow(columnName));
        return value == null ? "" : value;
    }

    /**
     * Locale.US and UTC are both set on purpose.
     *
     * The phone locale is not used. On a phone set to Arabic it would write
     * digits that do not sort or parse.
     *
     * UTC is used because the API also sends UTC. The two can then be
     * compared.
     */
    @NonNull
    private static String nowAsIsoUtc() {
        SimpleDateFormat format =
                new SimpleDateFormat("yyyy-MM-dd'T'HH:mm:ss'Z'", Locale.US);
        format.setTimeZone(TimeZone.getTimeZone("UTC"));
        return format.format(new Date());
    }
}
