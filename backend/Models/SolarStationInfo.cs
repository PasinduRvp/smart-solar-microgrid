/*
 * ---------------------------------------------------------------------------
 * File        : SolarStationInfo.cs
 * Project     : Smart Solar Microgrid Trading System — SE4040 Assignment 1
 * Author      : <Your Full Name> (<IT Number>)
 * Created     : 2026-09-17
 * Description : A solar microgrid node: its GPS position, generating capacity,
 *               battery storage slots and weekly operating schedule. Created
 *               and maintained by Backoffice officers.
 *
 * OOP         : Inherits from EntityBase, and composes GeoLocation and
 *               ScheduleEntry rather than flattening their fields into itself.
 * Business    : A node cannot be deactivated while it still has active
 *               reservations. That rule lives in the station service, not
 *               here — an entity holds state, not policy.
 * ---------------------------------------------------------------------------
 */

using MongoDB.Bson.Serialization.Attributes;

namespace SolarMicrogrid.Api.Models;

/// <summary>
/// A solar microgrid node that prosumers can trade energy with.
/// </summary>
public sealed class SolarStationInfo : EntityBase
{
    /// <summary>Human readable reference code, for example "MG-COL-004".</summary>
    [BsonElement("stationCode")]
    public string StationCode { get; set; } = string.Empty;

    /// <summary>Display name of the node.</summary>
    [BsonElement("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>GPS position and street address, used by the Android map view.</summary>
    [BsonElement("location")]
    public GeoLocation Location { get; set; } = new();

    /// <summary>Rated capacity of the node in kilowatt hours.</summary>
    [BsonElement("capacityKwh")]
    public double CapacityKwh { get; set; }

    /// <summary>Total number of battery storage slots physically installed.</summary>
    [BsonElement("totalBatterySlots")]
    public int TotalBatterySlots { get; set; }

    /// <summary>
    /// Battery slots currently free. Maintained by grid operators and checked
    /// before a reservation is accepted.
    /// </summary>
    [BsonElement("availableBatterySlots")]
    public int AvailableBatterySlots { get; set; }

    /// <summary>
    /// Weekly operating hours. Declared as IList so the MongoDB driver can
    /// populate it during deserialisation.
    /// </summary>
    [BsonElement("schedule")]
    public IList<ScheduleEntry> Schedule { get; set; } = new List<ScheduleEntry>();

    /// <summary>
    /// False once the node has been deactivated. Deactivated nodes are retained
    /// rather than deleted so that historical reservations still resolve.
    /// </summary>
    [BsonElement("isActive")]
    public bool IsActive { get; set; } = true;
}
