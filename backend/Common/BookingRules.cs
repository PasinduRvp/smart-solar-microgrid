/*
 * ---------------------------------------------------------------------------
 * File        : BookingRules.cs
 * Project     : Smart Solar Microgrid Trading System — SE4040 Assignment 1
 * Author      : <Your Full Name> (<IT Number>)
 * Created     : 2026-09-19
 * Description : The numeric limits the assignment specifies for reservations,
 *               together with the two questions those limits answer.
 *
 * Why one file: The 7 day window and the 12 hour notice period are each used in
 *               more than one place — when a booking is created, when it is
 *               changed, when it is cancelled, and when the API tells a client
 *               whether its buttons should be enabled. Writing "12" in each of
 *               those places would be a magic number repeated five times, and
 *               a change to the rule would then have to find every copy.
 *               Declaring them once means the rule has exactly one definition.
 *
 * Business    : BR-1  A booking must fall within 7 days of being created.
 *               BR-2  A booking may be changed only 12 hours or more before it.
 *               BR-3  A booking may be cancelled only 12 hours or more before it.
 * ---------------------------------------------------------------------------
 */

namespace SolarMicrogrid.Api.Common;

/// <summary>
/// The booking limits defined by the assignment, and the checks that use them.
/// </summary>
public static class BookingRules
{
    /// <summary>
    /// BR-1. How far ahead a reservation may be scheduled, measured from the
    /// moment it is created.
    /// </summary>
    public const int MaximumBookingWindowDays = 7;

    /// <summary>
    /// BR-2 and BR-3. How much notice is required before a booking's scheduled
    /// time for it to be changed or cancelled.
    /// </summary>
    public const int MinimumChangeNoticeHours = 12;

    /// <summary>
    /// BR-1: true when the given instant is in the future and no more than
    /// seven days from now.
    /// </summary>
    /// <param name="reservationDateTimeUtc">The proposed booking time, in UTC.</param>
    /// <param name="nowUtc">The current instant, in UTC.</param>
    public static bool IsWithinBookingWindow(DateTime reservationDateTimeUtc, DateTime nowUtc)
    {
        // Both halves matter. A booking in the past is meaningless, and one
        // beyond seven days is what the assignment forbids.
        return reservationDateTimeUtc > nowUtc
            && reservationDateTimeUtc <= nowUtc.AddDays(MaximumBookingWindowDays);
    }

    /// <summary>
    /// BR-2 and BR-3: true when there is still at least twelve hours before the
    /// booking, so it may be changed or cancelled.
    /// </summary>
    /// <param name="reservationDateTimeUtc">The booking time, in UTC.</param>
    /// <param name="nowUtc">The current instant, in UTC.</param>
    public static bool IsOutsideNoticePeriod(DateTime reservationDateTimeUtc, DateTime nowUtc)
    {
        return reservationDateTimeUtc - nowUtc >= TimeSpan.FromHours(MinimumChangeNoticeHours);
    }

    /// <summary>
    /// Hours remaining until a booking. Negative once the time has passed.
    /// Used to tell a client how close the notice period is.
    /// </summary>
    public static double HoursUntil(DateTime reservationDateTimeUtc, DateTime nowUtc)
    {
        return Math.Round((reservationDateTimeUtc - nowUtc).TotalHours, 2);
    }
}
