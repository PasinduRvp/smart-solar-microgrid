/*
 * ---------------------------------------------------------------------------
 * File        : User.cs
 * Project     : Smart Solar Microgrid Trading System — SE4040 Assignment 1
 * Author      : <Your Full Name> (<IT Number>)
 * Created     : 2026-09-17
 * Description : A user of the system. One collection holds all three roles —
 *               Backoffice officers, Grid Operators and Solar Prosumers —
 *               distinguished by the Role property. The assignment counts
 *               "User's detail" as one of the four required collections, so
 *               they are deliberately not split apart.
 *
 * OOP         : Inherits identity and audit fields from EntityBase.
 * Security    : Only the BCrypt hash of the password is stored. The plain
 *               password is never persisted, never logged and never returned
 *               by any endpoint — DTOs are used to shape responses so this
 *               entity is never serialised directly to a client.
 * ---------------------------------------------------------------------------
 */

using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using SolarMicrogrid.Api.Models.Enums;

namespace SolarMicrogrid.Api.Models;

/// <summary>
/// An account belonging to a Backoffice officer, Grid Operator or Prosumer.
/// </summary>
public sealed class User : EntityBase
{
    /// <summary>
    /// National Identity Card number. This is the business key for prosumers,
    /// as required by the assignment, and is protected by a unique index
    /// created at startup by <see cref="Data.DatabaseInitializer"/>.
    /// </summary>
    [BsonElement("nic")]
    public string Nic { get; set; } = string.Empty;

    /// <summary>Full name of the account holder.</summary>
    [BsonElement("fullName")]
    public string FullName { get; set; } = string.Empty;

    /// <summary>Email address. Used as the sign in identifier for staff accounts.</summary>
    [BsonElement("email")]
    public string Email { get; set; } = string.Empty;

    /// <summary>Contact telephone number.</summary>
    [BsonElement("phone")]
    public string Phone { get; set; } = string.Empty;

    /// <summary>Postal address of the property.</summary>
    [BsonElement("address")]
    public string Address { get; set; } = string.Empty;

    /// <summary>BCrypt hash of the account password. Never the password itself.</summary>
    [BsonElement("passwordHash")]
    public string PasswordHash { get; set; } = string.Empty;

    /// <summary>
    /// Role held by this account, which drives authorisation on every endpoint.
    /// Stored as a string so the documents stay readable in MongoDB Compass.
    /// </summary>
    [BsonElement("role")]
    [BsonRepresentation(BsonType.String)]
    public UserRole Role { get; set; } = UserRole.Prosumer;

    /// <summary>
    /// Current lifecycle state. A prosumer registering from the mobile
    /// application starts as Pending and cannot sign in until approved.
    /// </summary>
    [BsonElement("status")]
    [BsonRepresentation(BsonType.String)]
    public AccountStatus Status { get; set; } = AccountStatus.Pending;

    /// <summary>
    /// True when the prosumer has asked to have their account deactivated from
    /// the mobile application. The request is completed by a Backoffice
    /// officer; a prosumer cannot deactivate their own account directly.
    /// </summary>
    [BsonElement("deactivationRequested")]
    public bool DeactivationRequested { get; set; }

    /// <summary>Rated output of the prosumer's solar panel array, in kilowatts.</summary>
    [BsonElement("solarCapacityKw")]
    public double SolarCapacityKw { get; set; }

    /// <summary>
    /// Identifier of the account that created this record, for audit purposes.
    /// Null for self registered prosumers.
    /// </summary>
    [BsonElement("createdBy")]
    public string? CreatedBy { get; set; }
}
