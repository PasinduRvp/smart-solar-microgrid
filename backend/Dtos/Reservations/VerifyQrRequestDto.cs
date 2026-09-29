/*
 * ---------------------------------------------------------------------------
 * File        : VerifyQrRequestDto.cs
 * Project     : Smart Solar Microgrid Trading System — SE4040 Assignment 1
 * Author      : <Your Full Name> (<IT Number>)
 * Created     : 2026-09-19
 * Description : The raw string a grid operator's scanner read from a prosumer's
 *               transaction QR code, sent to the API to be verified.
 *
 * Security    : The operator device sends what it scanned and is TOLD whether
 *               it is valid. It never decides for itself. An application that
 *               decoded the code and accepted it locally could be modified to
 *               accept anything, and would also accept a code that had already
 *               been used.
 * ---------------------------------------------------------------------------
 */

using System.ComponentModel.DataAnnotations;

namespace SolarMicrogrid.Api.Dtos.Reservations;

/// <summary>
/// A scanned QR payload awaiting verification.
/// </summary>
public sealed class VerifyQrRequestDto
{
    /// <summary>The exact string decoded from the QR code.</summary>
    [Required(AllowEmptyStrings = false, ErrorMessage = "The scanned QR token is required.")]
    [MaxLength(200)]
    public string QrToken { get; init; } = string.Empty;
}
