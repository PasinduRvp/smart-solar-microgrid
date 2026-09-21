/*
 * ---------------------------------------------------------------------------
 * File        : JwtSettings.cs
 * Project     : Smart Solar Microgrid Trading System — SE4040 Assignment 1
 * Author      : <Your Full Name> (<IT Number>)
 * Created     : 2026-09-19
 * Description : Strongly typed configuration for issuing and validating JSON
 *               Web Tokens. Validated at startup alongside the other settings.
 *
 * SOLID       : Single Responsibility — configuration values only.
 * Security    : SecretKey signs every token. Anyone holding it can mint a token
 *               claiming any role, so it is supplied from the git-ignored
 *               appsettings.Development.json (or an environment variable on the
 *               IIS server) and never committed. The minimum length is enforced
 *               because HMAC-SHA256 keys shorter than 256 bits weaken the
 *               signature, and the .NET token handler refuses them outright.
 * ---------------------------------------------------------------------------
 */

using System.ComponentModel.DataAnnotations;

namespace SolarMicrogrid.Api.Configuration;

/// <summary>
/// Settings controlling JWT creation and validation.
/// </summary>
public sealed class JwtSettings
{
    /// <summary>Name of the configuration section these settings are bound from.</summary>
    public const string SectionName = "Jwt";

    /// <summary>Minimum key length in characters, equal to 256 bits of ASCII.</summary>
    public const int MinimumSecretKeyLength = 32;

    /// <summary>
    /// Symmetric signing key. Must be at least 32 characters and must be kept
    /// secret; it is the only thing preventing forged tokens.
    /// </summary>
    [Required(AllowEmptyStrings = false, ErrorMessage =
        "Jwt:SecretKey is missing. Add it to appsettings.Development.json.")]
    [MinLength(MinimumSecretKeyLength, ErrorMessage =
        "Jwt:SecretKey must be at least 32 characters so the HMAC-SHA256 key is a full 256 bits.")]
    public string SecretKey { get; init; } = string.Empty;

    /// <summary>Value placed in the "iss" claim and required when validating.</summary>
    [Required(AllowEmptyStrings = false)]
    public string Issuer { get; init; } = string.Empty;

    /// <summary>Value placed in the "aud" claim and required when validating.</summary>
    [Required(AllowEmptyStrings = false)]
    public string Audience { get; init; } = string.Empty;

    /// <summary>
    /// Token lifetime in minutes. Kept short because this project has no
    /// refresh token flow: a stolen token stays usable until it expires.
    /// </summary>
    [Range(5, 1440, ErrorMessage = "Jwt:ExpiryMinutes must be between 5 and 1440.")]
    public int ExpiryMinutes { get; init; } = 120;
}
