/*
 * ---------------------------------------------------------------------------
 * File        : Validators.java
 * Project     : Smart Solar Microgrid Trading System - SE4040 Assignment 1
 * Author      : M T A J Yapa (IT 23278530)
 * Created     : 2026-09-20
 * Description : The input formats the Web API accepts. Written down once.
 *
 * These copy the [RegularExpression] and [Range] rules on
 * RegisterProsumerRequestDto in the Web API.
 * Keeping the two the same matters. If this file were stricter, a user
 * could not enter something the system allows. If it were looser, they
 * would fill in a long form and then have the server reject it.
 *
 * Client checks help. They do not protect.
 *               Anything here can be skipped by calling the API directly.
 *               So the server checks everything again. These exist only to
 *               tell the user about a problem at once, with no round trip.
 *
 * SOLID        Single Responsibility. It answers yes or no about a value.
 *               It shows no messages and touches no views.
 * ---------------------------------------------------------------------------
 */
package lk.sliit.solarmicrogrid.util;

import android.text.TextUtils;
import android.util.Patterns;

import androidx.annotation.Nullable;

import java.util.regex.Pattern;

public final class Validators {

    /**
     * Sri Lankan NIC. Two formats are allowed.
     * The old one is 9 digits then V or X. The new one is 12 digits.
     * This must match the pattern in the Web API exactly.
     */
    private static final Pattern NIC =
            Pattern.compile("^(\\d{9}[VvXx]|\\d{12})$");

    /** Local mobile or land line: 10 digits beginning with 0. */
    private static final Pattern PHONE =
            Pattern.compile("^0\\d{9}$");

    /** The range the API accepts for solar panels, in kilowatts. */
    public static final double MIN_SOLAR_CAPACITY_KW = 0.1d;
    public static final double MAX_SOLAR_CAPACITY_KW = 1000d;

    /** The shortest password the API will accept. */
    public static final int MINIMUM_PASSWORD_LENGTH = 8;

    public static boolean isValidNic(@Nullable String value) {
        return value != null && NIC.matcher(value.trim()).matches();
    }

    public static boolean isValidPhone(@Nullable String value) {
        return value != null && PHONE.matcher(value.trim()).matches();
    }

    /**
     * Uses the email pattern that Android ships with.
     *
     * Email addresses vary much more than they look. A hand written pattern
     * usually ends up rejecting real ones.
     */
    public static boolean isValidEmail(@Nullable String value) {
        return value != null
                && !TextUtils.isEmpty(value.trim())
                && Patterns.EMAIL_ADDRESS.matcher(value.trim()).matches();
    }

    public static boolean isNotBlank(@Nullable String value) {
        return value != null && !value.trim().isEmpty();
    }

    public static boolean isValidPassword(@Nullable String value) {
        return value != null && value.length() >= MINIMUM_PASSWORD_LENGTH;
    }

    /**
     * True when the text is a number inside the range the API allows.
     *
     * The parse is guarded. The decimal keyboard still lets a user type
     * 1.2.3, or leave the box empty. An unguarded parse would crash.
     */
    public static boolean isValidSolarCapacity(@Nullable String value) {
        Double parsed = parseDoubleOrNull(value);
        return parsed != null
                && parsed >= MIN_SOLAR_CAPACITY_KW
                && parsed <= MAX_SOLAR_CAPACITY_KW;
    }

    /**
     * @return the number, or null when the text is not one.
     */
    @Nullable
    public static Double parseDoubleOrNull(@Nullable String value) {
        if (value == null || value.trim().isEmpty()) {
            return null;
        }
        try {
            return Double.valueOf(value.trim());
        } catch (NumberFormatException notANumber) {
            return null;
        }
    }

    /** Utility class: never instantiated. */
    private Validators() {
    }
}
