/*
 * ---------------------------------------------------------------------------
 * File        : EnergyReservation.cs
 * Project     : Smart Solar Microgrid Trading System — SE4040 Assignment 1
 * Author      : <Your Full Name> (<IT Number>)
 * Created     : 2026-09-17
 * Description : A prosumer's booking of an energy trading slot at a microgrid
 *               node, from initial request through to a completed transfer.
 *
 * OOP         : Inherits from EntityBase.
 * Business    : Three assignment rules are measured against ReservationDateTime
 *               and are enforced in the reservation service, not here:
 *                 - a booking must fall within 7 days of being created;
 *                 - it may only be updated at least 12 hours beforehand;
 *                 - it may only be cancelled at least 12 hours beforehand.
 * Security    : QrToken is a server generated secret. It is written only when
 *               the reservation is approved, and a scanned code is always
 *               verified against this stored value by the API. The operator's
 *               device never decides on its own whether a code is valid.
 * ---------------------------------------------------------------------------
 */

using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using SolarMicrogrid.Api.Models.Enums;

namespace SolarMicrogrid.Api.Models;

/// <summary>
/// A booked energy trading slot belonging to one prosumer.
/// </summary>
public sealed class EnergyReservation : EntityBase
{
    /// <summary>Readable booking reference shown on the confirmation screen.</summary>
    [BsonElement("reservationNo")]
    public string ReservationNo { get; set; } = string.Empty;

    /// <summary>
    /// NIC of the prosumer who owns the booking. NIC is used rather than the
    /// document id because the assignment defines it as the prosumer key.
    /// </summary>
    [BsonElement("prosumerNic")]
    public string ProsumerNic { get; set; } = string.Empty;

    /// <summary>Identifier of the station where the transfer takes place.</summary>
    [BsonElement("stationId")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string StationId { get; set; } = string.Empty;

    /// <summary>Identifier of the booked time window.</summary>
    [BsonElement("slotId")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string SlotId { get; set; } = string.Empty;

    /// <summary>
    /// The exact instant the transfer is scheduled for, in UTC. Every booking
    /// rule in the system is measured from this value.
    /// </summary>
    [BsonElement("reservationDateTime")]
    public DateTime ReservationDateTime { get; set; }

    /// <summary>Amount of energy to be traded, in kilowatt hours.</summary>
    [BsonElement("energyKwh")]
    public double EnergyKwh { get; set; }

    /// <summary>Whether the prosumer is delivering energy or drawing it.</summary>
    [BsonElement("direction")]
    [BsonRepresentation(BsonType.String)]
    public EnergyDirection Direction { get; set; } = EnergyDirection.Deliver;

    /// <summary>Current state of the booking.</summary>
    [BsonElement("status")]
    [BsonRepresentation(BsonType.String)]
    public ReservationStatus Status { get; set; } = ReservationStatus.Pending;

    /// <summary>
    /// Server generated token encoded into the prosumer's QR code.
    /// Null until the booking is approved.
    /// </summary>
    [BsonElement("qrToken")]
    public string? QrToken { get; set; }

    /// <summary>UTC time the QR token was issued. Null until approval.</summary>
    [BsonElement("qrIssuedAt")]
    public DateTime? QrIssuedAt { get; set; }

    /// <summary>Identifier of the user who approved the booking.</summary>
    [BsonElement("approvedBy")]
    public string? ApprovedBy { get; set; }

    /// <summary>Identifier of the operator who finalised the energy transfer.</summary>
    [BsonElement("completedBy")]
    public string? CompletedBy { get; set; }

    /// <summary>UTC time the transfer was finalised.</summary>
    [BsonElement("completedAt")]
    public DateTime? CompletedAt { get; set; }

    /// <summary>Reason recorded when the booking was cancelled.</summary>
    [BsonElement("cancelReason")]
    public string? CancelReason { get; set; }
}
