/*
 * ---------------------------------------------------------------------------
 * File        : GridTime.cs
 * Project     : Smart Solar Microgrid Trading System - SE4040 Assignment 1
 * Author      : <Your Full Name> (<IT Number>)
 * Created     : 2026-09-21
 * Description : Turns the local opening hours of a node into a UTC instant.
 *
 * The problem this fixes
 *               A node schedule says a window opens at 06:00. That is a wall
 *               clock time at the node, in Sri Lanka. It is not 06:00 UTC.
 *
 *               The old code used DateTime.SpecifyKind, which only relabels
 *               a value as UTC. It does not convert it. So 06:00 was stored
 *               as 06:00 UTC, and every client showing local time displayed
 *               it as 11:30. The booking picker and the booking list then
 *               disagreed by the 5 hour 30 minute offset.
 *
 *               This class converts instead of relabelling.
 *
 * Why the zone is held here
 *               Every node in this system is in one country, so one zone
 *               serves them all. It is read from configuration once at
 *               startup, so a deployment in another country only changes a
 *               setting.
 *
 *               If nodes in several countries were ever needed, the zone
 *               would move onto the station record and this class would take
 *               it as a parameter. The conversion itself would not change.
 *
 * Note on the fallback
 *               Windows and Linux name time zones differently. .NET 8 accepts
 *               both, but a machine with a trimmed ICU database can still
 *               fail to find one. Sri Lanka has had no daylight saving since
 *               2006, so a fixed +05:30 offset is a safe fallback and keeps
 *               the API running rather than failing at startup.
 * ---------------------------------------------------------------------------
 */
using System.Globalization;

namespace SolarMicrogrid.Api.Common;

public static class GridTime
{
    /// <summary>The zone used when configuration does not name one.</summary>
    public const string DefaultTimeZoneId = "Asia/Colombo";

    /// <summary>Sri Lanka is UTC+05:30 all year. Used if the zone is unknown.</summary>
    private static readonly TimeSpan FallbackOffset = new(5, 30, 0);

    private static TimeZoneInfo _zone = Resolve(DefaultTimeZoneId);

    /// <summary>
    /// Sets the zone the nodes operate in. Called once, from Program.cs.
    /// </summary>
    public static void Configure(string? timeZoneId)
    {
        _zone = Resolve(string.IsNullOrWhiteSpace(timeZoneId) ? DefaultTimeZoneId : timeZoneId);
    }

    /// <summary>The zone currently in use. Exposed so it can be logged.</summary>
    public static string CurrentTimeZoneId => _zone.Id;

    /// <summary>
    /// Combines a date and a local wall clock time into a UTC instant.
    /// </summary>
    /// <param name="date">The day the window falls on.</param>
    /// <param name="localTime">The local time the window starts, as "HH:mm".</param>
    public static DateTime ToUtc(DateTime date, TimeOnly localTime)
    {
        DateTime localWallClock = DateTime.SpecifyKind(
            date.Date.Add(localTime.ToTimeSpan()),
            DateTimeKind.Unspecified);

        // Unspecified is required here. TimeZoneInfo refuses to convert a
        // value already marked Utc, and would silently do nothing.
        return TimeZoneInfo.ConvertTimeToUtc(localWallClock, _zone);
    }

    /// <summary>
    /// Reads an "HH:mm" opening time.
    /// </summary>
    /// <returns>The time, or midnight when the text cannot be read.</returns>
    public static TimeOnly ParseTimeOrMidnight(string? text)
    {
        return TimeOnly.TryParseExact(
            text,
            "HH:mm",
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out TimeOnly parsed)
            ? parsed
            : TimeOnly.MinValue;
    }

    private static TimeZoneInfo Resolve(string timeZoneId)
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
        }
        catch (Exception ex) when (ex is TimeZoneNotFoundException or InvalidTimeZoneException)
        {
            // The machine does not know that zone. A fixed offset keeps the
            // API working, and is correct for Sri Lanka, which has no
            // daylight saving.
            return TimeZoneInfo.CreateCustomTimeZone(
                "SolarMicrogrid.Fallback",
                FallbackOffset,
                "Grid time (UTC+05:30)",
                "Grid time (UTC+05:30)");
        }
    }
}
