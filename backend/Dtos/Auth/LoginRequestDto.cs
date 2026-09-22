/*
 * ---------------------------------------------------------------------------
 * File        : LoginRequestDto.cs
 * Project     : Smart Solar Microgrid Trading System — SE4040 Assignment 1
 * Author      : <Your Full Name> (<IT Number>)
 * Created     : 2026-09-19
 * Description : Credentials posted to POST /api/auth/login by both the web and
 *               Android clients.
 *
 * Design note : Staff sign in with their email address and prosumers with their
 *               NIC, so a single Identifier field accepts either. The service
 *               decides which it is. One endpoint for all three roles keeps the
 *               clients simple and means the sign in rules live in one place.
 * Security    : [Required] produces a 400 before any database work happens, so
 *               empty requests cannot be used to probe the login endpoint.
 * ---------------------------------------------------------------------------
 */

using System.ComponentModel.DataAnnotations;

namespace SolarMicrogrid.Api.Dtos.Auth;

/// <summary>
/// Sign in credentials.
/// </summary>
public sealed class LoginRequestDto
{
    /// <summary>Email address for staff accounts, or NIC for prosumers.</summary>
    [Required(AllowEmptyStrings = false, ErrorMessage = "Enter your email address or NIC.")]
    [MaxLength(100)]
    public string Identifier { get; init; } = string.Empty;

    /// <summary>The account password in plain text, sent over the request body.</summary>
    [Required(AllowEmptyStrings = false, ErrorMessage = "Enter your password.")]
    [MaxLength(128)]
    public string Password { get; init; } = string.Empty;
}
