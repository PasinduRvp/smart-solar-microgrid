/*
 * ---------------------------------------------------------------------------
 * File        : DateTimes.java
 * Project     : Smart Solar Microgrid Trading System - SE4040 Assignment 1
 * Author      : L K P Yasith (IT 23380196)
 * Created     : 2026-09-20
 * Description : Reads the dates the Web API sends, and writes them back in
 *               the form it expects.
 *
 * Why dates are kept as text in the DTOs
 *              Gson can turn a date string into a Date object, but only in
 *              the one format it was set up for. The API sends ISO 8601, and
 *              sometimes with fractional seconds and sometimes without.
 *              Keeping the raw text and parsing it here means one place to
 *              handle both, instead of a crash the first time the server
 *              sends the shorter form.
 *
 * UTC in, local out
 *              The API works in UTC. People read local time. So every value
 *              is parsed as UTC and shown in the time zone of the phone.
 *              Skipping that step would show a Sri Lankan user a booking at
 *              02:30 when they made it for 08:00.
 *
 * SOLID       Single Responsibility. It converts dates. Nothing else.
 * ---------------------------------------------------------------------------
 */
package lk.sliit.solarmicrogrid.util;

import androidx.annotation.NonNull;
import androidx.annotation.Nullable;

import java.text.ParseException;
import java.text.SimpleDateFormat;
import java.util.Calendar;
import java.util.Date;
import java.util.Locale;
import java.util.TimeZone;

public final class DateTimes {

    /** The forms the API may send. The longer one is tried first. */
    private static final String[] INCOMING_PATTERNS = {
            "yyyy-MM-dd'T'HH:mm:ss.SSS",
            "yyyy-MM-dd'T'HH:mm:ss",
            "yyyy-MM-dd",
    };

    /** What a date only query parameter must look like. */
    private static final String QUERY_DATE = "yyyy-MM-dd";

    /** How a date and time are shown to the user. */
    private static final String DISPLAY_DATE_TIME = "d MMM yyyy, h:mm a";
    private static final String DISPLAY_DATE = "EEE d MMM";
    private static final String DISPLAY_DAY_SHORT = "EEE";
    private static final String DISPLAY_DAY_NUMBER = "d MMM";

    /** Just the date in the month, for the day chips. For example: 29. */
    private static final String DISPLAY_DAY_OF_MONTH = "d";

    /**
     * Reads a date sent by the API.
     *
     * @return the moment in time, or null when the text cannot be read.
     *         A null is handled by the caller. It never crashes a screen.
     */
    @Nullable
    public static Date parseUtc(@Nullable String text) {

        if (text == null || text.trim().isEmpty()) {
            return null;
        }

        // A trailing Z only means UTC, and the patterns below already assume
        // UTC, so it is removed before parsing.
        String cleaned = text.trim();
        if (cleaned.endsWith("Z")) {
            cleaned = cleaned.substring(0, cleaned.length() - 1);
        }

        for (String pattern : INCOMING_PATTERNS) {
            try {
                SimpleDateFormat format = new SimpleDateFormat(pattern, Locale.US);
                format.setTimeZone(TimeZone.getTimeZone("UTC"));
                format.setLenient(false);
                return format.parse(cleaned);
            } catch (ParseException tryTheNextPattern) {
                // Expected while working through the list. Not an error until
                // every pattern has been tried.
            }
        }
        return null;
    }

    /** For example: 25 Sep 2026, 8:00 AM. Shown in the phone time zone. */
    @NonNull
    public static String showDateAndTime(@Nullable String utcText) {
        return show(utcText, DISPLAY_DATE_TIME);
    }

    /** For example: Fri 25 Sep. */
    @NonNull
    public static String showDate(@Nullable String utcText) {
        return show(utcText, DISPLAY_DATE);
    }

    /** The short day name of a date, for the day chips. For example: Fri. */
    @NonNull
    public static String showDayName(@NonNull Date date) {
        return format(date, DISPLAY_DAY_SHORT);
    }

    /** The day and month of a date, for sentences. For example: 25 Sep. */
    @NonNull
    public static String showDayNumber(@NonNull Date date) {
        return format(date, DISPLAY_DAY_NUMBER);
    }

    /**
     * Just the date in the month, for the day chips. For example: 25.
     *
     * A chip is one short line. "Tue 25 Sep" across seven chips makes a row
     * far wider than the screen, so the month is left off. The month is
     * still shown in the empty message below, where there is room.
     */
    @NonNull
    public static String showDayOfMonth(@NonNull Date date) {
        return format(date, DISPLAY_DAY_OF_MONTH);
    }

    /** Turns a date into the yyyy-MM-dd a query parameter needs. */
    @NonNull
    public static String asQueryDate(@NonNull Date date) {
        SimpleDateFormat format = new SimpleDateFormat(QUERY_DATE, Locale.US);
        return format.format(date);
    }

    /**
     * The next numberOfDays days, starting today.
     *
     * This is business rule BR-1 on screen. A booking must be inside the next
     * 7 days, so the picker never offers a day the server would refuse.
     */
    @NonNull
    public static Date[] nextDays(int numberOfDays) {

        Date[] days = new Date[numberOfDays];
        Calendar calendar = Calendar.getInstance();

        // Start at midnight, so a booking made at 11pm still counts today as
        // day one rather than rolling on to tomorrow.
        calendar.set(Calendar.HOUR_OF_DAY, 0);
        calendar.set(Calendar.MINUTE, 0);
        calendar.set(Calendar.SECOND, 0);
        calendar.set(Calendar.MILLISECOND, 0);

        for (int index = 0; index < numberOfDays; index++) {
            days[index] = calendar.getTime();
            calendar.add(Calendar.DAY_OF_MONTH, 1);
        }
        return days;
    }

    @NonNull
    private static String show(@Nullable String utcText, @NonNull String pattern) {
        Date moment = parseUtc(utcText);
        return moment == null ? "" : format(moment, pattern);
    }

    /**
     * Uses the default time zone on purpose, so the user reads local time.
     * Locale.US keeps the month names in English, which matches the rest of
     * the screens.
     */
    @NonNull
    private static String format(@NonNull Date date, @NonNull String pattern) {
        return new SimpleDateFormat(pattern, Locale.US).format(date);
    }

    /** Utility class: never instantiated. */
    private DateTimes() {
    }
}
