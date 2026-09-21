/*
 * ---------------------------------------------------------------------------
 * File        : ServiceErrorType.cs
 * Project     : Smart Solar Microgrid Trading System — SE4040 Assignment 1
 * Author      : NIMSARA R V P P (IT 23215306)
 * Created     : 2026-09-19
 * Description : Categories of failure a service operation can report. The
 *               controller maps each category onto an HTTP status code.
 *
 * Why this    : The service layer must stay free of HTTP concepts — it should
 *               not know what "404" means — while the controller must not
 *               guess why something failed. This enum is the shared vocabulary
 *               between them, which keeps the FAT service pattern intact.
 * ---------------------------------------------------------------------------
 */

namespace SolarMicrogrid.Api.Common;

/// <summary>
/// The kind of failure returned by a service operation.
/// </summary>
public enum ServiceErrorType
{
    /// <summary>The operation succeeded; no error.</summary>
    None = 0,

    /// <summary>Input failed validation. Maps to 400 Bad Request.</summary>
    Validation = 1,

    /// <summary>Credentials were missing or wrong. Maps to 401 Unauthorized.</summary>
    Unauthorized = 2,

    /// <summary>The caller is known but not allowed. Maps to 403 Forbidden.</summary>
    Forbidden = 3,

    /// <summary>The requested record does not exist. Maps to 404 Not Found.</summary>
    NotFound = 4,

    /// <summary>
    /// The request clashes with the current state of the data, for example a
    /// duplicate NIC or a booking outside the permitted window.
    /// Maps to 409 Conflict.
    /// </summary>
    Conflict = 5,
}
