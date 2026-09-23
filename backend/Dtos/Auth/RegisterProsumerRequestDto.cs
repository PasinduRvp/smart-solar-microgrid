/*
 * ---------------------------------------------------------------------------
 * File        : RegisterProsumerRequestDto.cs
 * Project     : Smart Solar Microgrid Trading System — SE4040 Assignment 1
 * Author      : <Your Full Name> (<IT Number>)
 * Created     : 2026-09-19
 * Description : Self registration details posted from the Android application.
 *               Only prosumers may register themselves; Backoffice and Grid
 *               Operator accounts are created by a Backoffice officer through
 *               the web application.
 *
 * Security    : The Role is deliberately NOT accepted from the request. If it
 *               were, anyone could register themselves as a Backoffice officer
 *               and take over the system. The service always assigns the
 *               Prosumer role, and the account starts as Pending so a real
 *               officer must approve it before it can be used.
 * ---------------------------------------------------------------------------
 */

using System.ComponentModel.DataAnnotations;

namespace SolarMicrogrid.Api.Dtos.Auth;

/// <summary>
/// Details supplied by a prosumer registering on the mobile application.
/// </summary>
public sealed class RegisterProsumerRequestDto
{
    /// <summary>
    /// National Identity Card number. This is the prosumer's primary key, so it
    /// must be unique across all accounts.
    /// Accepts the old 9 digit plus letter format and the current 12 digit one.
    /// </summary>
    [Required(AllowEmptyStrings = false, ErrorMessage = "NIC is required.")]
    [RegularExpression(@"^(\d{9}[VvXx]|\d{12})$", ErrorMessage =
        "Enter a valid NIC: either 9 digits followed by V or X, or 12 digits.")]
    public string Nic { get; init; } = string.Empty;

    /// <summary>Full name of the prosumer.</summary>
    [Required(AllowEmptyStrings = false, ErrorMessage = "Full name is required.")]
    [MaxLength(120)]
    public string FullName { get; init; } = string.Empty;

    /// <summary>Email address. Must be unique across all accounts.</summary>
    [Required(AllowEmptyStrings = false, ErrorMessage = "Email address is required.")]
    [EmailAddress(ErrorMessage = "Enter a valid email address.")]
    [MaxLength(100)]
    public string Email { get; init; } = string.Empty;

    /// <summary>Contact telephone number, 10 digits.</summary>
    [Required(AllowEmptyStrings = false, ErrorMessage = "Phone number is required.")]
    [RegularExpression(@"^0\d{9}$", ErrorMessage = "Enter a valid 10 digit phone number, e.g. 0771234567.")]
    public string Phone { get; init; } = string.Empty;

    /// <summary>Address of the property carrying the solar array.</summary>
    [Required(AllowEmptyStrings = false, ErrorMessage = "Address is required.")]
    [MaxLength(200)]
    public string Address { get; init; } = string.Empty;

    /// <summary>Rated output of the solar panel array, in kilowatts.</summary>
    [Range(0.1, 1000, ErrorMessage = "Solar capacity must be between 0.1 and 1000 kW.")]
    public double SolarCapacityKw { get; init; }

    /// <summary>
    /// Chosen password. The minimum length is enforced here so a weak password
    /// is rejected before it is ever hashed and stored.
    /// </summary>
    [Required(AllowEmptyStrings = false, ErrorMessage = "Password is required.")]
    [MinLength(8, ErrorMessage = "Password must be at least 8 characters.")]
    [MaxLength(128)]
    public string Password { get; init; } = string.Empty;
}
