/*
 * ---------------------------------------------------------------------------
 * File        : SeedSettings.cs
 * Project     : Smart Solar Microgrid Trading System — SE4040 Assignment 1
 * Author      : NIMSARA R V P P (IT 23215306)
 * Created     : 2026-09-19
 * Description : Details of the first Backoffice account, created automatically
 *               when the database contains no Backoffice officer at all.
 *
 * Why this    : The system has a bootstrap problem. Only a Backoffice officer
 *               can create users and activate prosumers, but on a brand new
 *               database no such officer exists, so nobody can do anything.
 *               Seeding the first one breaks the deadlock.
 * Security    : The seed password is read from the git-ignored configuration
 *               file, never hard coded in source. Seeding happens only when
 *               there are no Backoffice accounts, so it can never overwrite or
 *               reset a real one, and the account it creates can be deleted
 *               once a proper officer account exists.
 * ---------------------------------------------------------------------------
 */

using System.ComponentModel.DataAnnotations;

namespace SolarMicrogrid.Api.Configuration;

/// <summary>
/// Details of the bootstrap Backoffice account.
/// </summary>
public sealed class SeedSettings
{
    /// <summary>Name of the configuration section these settings are bound from.</summary>
    public const string SectionName = "Seed";

    /// <summary>
    /// When false, no seeding is attempted. Set this to false once real
    /// Backoffice accounts exist and the deployment no longer needs bootstrapping.
    /// </summary>
    public bool CreateDefaultBackofficeUser { get; init; }

    /// <summary>NIC for the bootstrap account.</summary>
    public string Nic { get; init; } = string.Empty;

    /// <summary>Display name for the bootstrap account.</summary>
    public string FullName { get; init; } = string.Empty;

    /// <summary>Email address the bootstrap officer signs in with.</summary>
    [EmailAddress]
    public string Email { get; init; } = string.Empty;

    /// <summary>Password for the bootstrap account.</summary>
    [MinLength(8)]
    public string Password { get; init; } = string.Empty;
}
