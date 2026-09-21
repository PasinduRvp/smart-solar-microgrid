/*
 * ---------------------------------------------------------------------------
 * File        : ScheduleEntry.cs
 * Project     : Smart Solar Microgrid Trading System — SE4040 Assignment 1
 * Author      : NIMSARA R V P P (IT 23215306)
 * Created     : 2026-09-17
 * Description : Opening hours for a microgrid node on one day of the week.
 *               A station holds a collection of these, which Backoffice
 *               officers maintain through the web application.
 *
 * OOP         : A value object composed into SolarStationInfo.
 * Design note : Times are held as "HH:mm" strings rather than DateTime because
 *               they represent a recurring time of day with no date attached.
 *               Storing them as DateTime would invent a meaningless date and
 *               invite time zone bugs.
 * ---------------------------------------------------------------------------
 */

using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace SolarMicrogrid.Api.Models;

/// <summary>
/// Operating hours for a single day of the week.
/// </summary>
public sealed class ScheduleEntry
{
    /// <summary>Day of the week these hours apply to.</summary>
    [BsonElement("dayOfWeek")]
    [BsonRepresentation(BsonType.String)]
    public DayOfWeek DayOfWeek { get; set; }

    /// <summary>Opening time in 24 hour "HH:mm" format, e.g. "06:00".</summary>
    [BsonElement("openTime")]
    public string OpenTime { get; set; } = string.Empty;

    /// <summary>Closing time in 24 hour "HH:mm" format, e.g. "18:30".</summary>
    [BsonElement("closeTime")]
    public string CloseTime { get; set; } = string.Empty;
}
