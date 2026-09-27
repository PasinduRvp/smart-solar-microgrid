/*
 * ---------------------------------------------------------------------------
 * File        : TextFormat.java
 * Project     : Smart Solar Microgrid Trading System - SE4040 Assignment 1
 * Author      : <Your Full Name> (<IT Number>)
 * Created     : 2026-09-20
 * Description : Small text helpers used by more than one screen.
 *
 * SOLID        Single Responsibility. It formats text and nothing else.
 * ---------------------------------------------------------------------------
 */
package lk.sliit.solarmicrogrid.util;

import androidx.annotation.NonNull;
import androidx.annotation.Nullable;

import java.util.Locale;

public final class TextFormat {

    /**
     * The initials to draw in the avatar circle.
     *
     * Kamal Perera gives KP. One name gives its first letter. An empty name
     * gives a question mark. An empty circle would look like a loading error.
     *
     * Locale.ROOT is used on purpose. In Turkish, i becomes a dotted capital.
     * A name starting with i would then show an odd letter.
     */
    @NonNull
    public static String initialsOf(@Nullable String fullName) {

        if (fullName == null || fullName.trim().isEmpty()) {
            return "?";
        }

        String[] parts = fullName.trim().split("\\s+");
        String first = String.valueOf(parts[0].charAt(0));

        if (parts.length == 1) {
            return first.toUpperCase(Locale.ROOT);
        }

        String last = String.valueOf(parts[parts.length - 1].charAt(0));
        return (first + last).toUpperCase(Locale.ROOT);
    }

    /**
     * Prints a capacity with no trailing .0 on whole numbers.
     * 5 kW reads as 5. And 4.5 kW reads as 4.5.
     *
     * Locale.ROOT keeps the decimal point a dot. The number goes back into a
     * field the server reads, and a comma would be refused.
     */
    @NonNull
    public static String capacity(double kilowatts) {
        if (kilowatts == Math.floor(kilowatts) && !Double.isInfinite(kilowatts)) {
            return String.format(Locale.ROOT, "%d", (long) kilowatts);
        }
        return String.format(Locale.ROOT, "%s", kilowatts);
    }

    /** Utility class: never instantiated. */
    private TextFormat() {
    }
}
