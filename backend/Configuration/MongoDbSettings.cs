/*
 * ---------------------------------------------------------------------------
 * File        : MongoDbSettings.cs
 * Project     : Smart Solar Microgrid Trading System — SE4040 Assignment 1
 * Author      : <Your Full Name> (<IT Number>)
 * Created     : 2026-09-16
 * Description : Strongly typed configuration for the MongoDB connection. Bound
 *               from the "MongoDb" configuration section at startup and
 *               validated before the application is allowed to serve requests,
 *               so a misconfigured deployment fails immediately and loudly
 *               instead of throwing on the first database call.
 *
 * SOLID       : Single Responsibility — this type only carries configuration
 *               values. It performs no I/O and knows nothing about MongoDB
 *               itself, which keeps configuration concerns separate from data
 *               access concerns.
 * Security    : The connection string contains credentials and is therefore
 *               supplied from appsettings.Development.json (git-ignored) or an
 *               environment variable — never from source control.
 * ---------------------------------------------------------------------------
 */

using System.ComponentModel.DataAnnotations;

namespace SolarMicrogrid.Api.Configuration;

/// <summary>
/// Settings required to reach the MongoDB server that backs the service.
/// </summary>
public sealed class MongoDbSettings
{
    /// <summary>Name of the configuration section these settings are bound from.</summary>
    public const string SectionName = "MongoDb";

    /// <summary>
    /// Full MongoDB connection string, including credentials.
    /// Supplied at runtime; never committed to the repository.
    /// </summary>
    [Required(AllowEmptyStrings = false, ErrorMessage =
        "MongoDb:ConnectionString is missing. Copy appsettings.Example.json to " +
        "appsettings.Development.json and supply your Atlas connection string.")]
    [RegularExpression(@"^mongodb(\+srv)?:\/\/\S+$", ErrorMessage =
        "MongoDb:ConnectionString is not a valid MongoDB URI. It must begin with " +
        "'mongodb://' or 'mongodb+srv://'. Did you leave the placeholder in " +
        "appsettings.Development.json?")]
    public string ConnectionString { get; init; } = string.Empty;

    /// <summary>Name of the database inside the cluster that holds our collections.</summary>
    [Required(AllowEmptyStrings = false, ErrorMessage = "MongoDb:DatabaseName is missing.")]
    public string DatabaseName { get; init; } = string.Empty;

    /// <summary>
    /// How long a server selection attempt may take before the driver gives up.
    /// Keeps the health endpoint responsive when the cluster is unreachable
    /// instead of letting the request hang for the driver's 30 second default.
    /// </summary>
    [Range(1, 60, ErrorMessage = "MongoDb:ServerSelectionTimeoutSeconds must be between 1 and 60.")]
    public int ServerSelectionTimeoutSeconds { get; init; } = 15;
}
