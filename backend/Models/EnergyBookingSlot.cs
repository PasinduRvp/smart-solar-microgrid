/*
 * ---------------------------------------------------------------------------
 * File        : EnergyBookingSlot.cs
 * Project     : Smart Solar Microgrid Trading System — SE4040 Assignment 1
 * Author      : <Your Full Name> (<IT Number>)
 * Created     : 2026-09-17
 * Description : A bookable energy trading time window belonging to one station.
 *               Slots are generated for the coming seven days, matching the
 *               seven day booking window the assignment specifies.
 *
 * OOP         : Inherits from EntityBase.
 * Business    : BookedCount must never reach TotalCapacitySlots. That check is
 *               enforced in the reservation service so the rule sits in the
 *               API, never in the web or Android client.
 * ---------------------------------------------------------------------------
 */

using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace SolarMicrogrid.Api.Models;

/// <summary>
/// A single bookable time window at a microgrid node.
/// </summary>
public sealed class EnergyBookingSlot : EntityBase
{
    /// <summary>Identifier of the station this slot belongs to.</summary>
    [BsonElement("stationId")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string StationId { get; set; } = string.Empty;

    /// <summary>Calendar date of the slot, stored in UTC.</summary>
    [BsonElement("slotDate")]
    public DateTime SlotDate { get; set; }

    /// <summary>Start of the window in 24 hour "HH:mm" format.</summary>
    [BsonElement("startTime")]
    public string StartTime { get; set; } = string.Empty;

    /// <summary>End of the window in 24 hour "HH:mm" format.</summary>
    [BsonElement("endTime")]
    public string EndTime { get; set; } = string.Empty;

    /// <summary>How many reservations this window can hold.</summary>
    [BsonElement("totalCapacitySlots")]
    public int TotalCapacitySlots { get; set; }

    /// <summary>How many reservations have already been accepted for it.</summary>
    [BsonElement("bookedCount")]
    public int BookedCount { get; set; }

    /// <summary>Trading price per kilowatt hour for energy moved in this window.</summary>
    [BsonElement("energyRatePerKwh")]
    public double EnergyRatePerKwh { get; set; }

    /// <summary>False when an operator has closed the window manually.</summary>
    [BsonElement("isAvailable")]
    public bool IsAvailable { get; set; } = true;
}
