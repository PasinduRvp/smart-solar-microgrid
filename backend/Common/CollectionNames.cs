/*
 * ---------------------------------------------------------------------------
 * File        : CollectionNames.cs
 * Project     : Smart Solar Microgrid Trading System — SE4040 Assignment 1
 * Author      : <Your Full Name> (<IT Number>)
 * Created     : 2026-09-17
 * Description : Central list of the MongoDB collection names used by the
 *               service. Repositories reference these constants instead of
 *               repeating string literals.
 *
 * Code smell  : Removes "magic strings". A mistyped collection name would
 *               otherwise create a new, empty collection at runtime with no
 *               error at all, which is one of the hardest MongoDB bugs to spot.
 *               Declaring them once means a typo becomes a compile error.
 * ---------------------------------------------------------------------------
 */
//
namespace SolarMicrogrid.Api.Common;

/// <summary>
/// Names of the four collections that make up the application database.
/// </summary>
public static class CollectionNames
{
    /// <summary>Backoffice officers, grid operators and solar prosumers.</summary>
    public const string Users = "Users";

    /// <summary>Solar microgrid nodes, including GPS position and capacity.</summary>
    public const string SolarStationInfo = "SolarStationInfo";

    /// <summary>Bookable energy trading time windows belonging to a station.</summary>
    public const string EnergyBookingSlots = "EnergyBookingSlots";

    /// <summary>Energy slot reservations made by prosumers.</summary>
    public const string EnergyReservation = "EnergyReservation";
}
