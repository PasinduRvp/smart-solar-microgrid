/*
 * ---------------------------------------------------------------------------
 * File        : UpdateUserRequestDto.cs
 * Project     : Smart Solar Microgrid Trading System — SE4040 Assignment 1
 * Author      : <Your Full Name> (<IT Number>)
 * Created     : 2026-09-19
 * Description : Editable details of an existing account. Used both by a
 *               Backoffice officer updating any account, and by a prosumer
 *               editing their own profile from the mobile application.
 *
 * Security    : Notice what is absent. NIC, Role and Status are NOT here, so no
 *               caller can rename their own key, promote themselves to
 *               Backoffice, or activate their own pending account by editing
 *               their profile. Status changes go through the dedicated
 *               activate and deactivate endpoints, which carry their own
 *               authorisation rules. This is mass assignment protection: the
 *               DTO simply has no field for the dangerous values.
 * ---------------------------------------------------------------------------
 */

using System.ComponentModel.DataAnnotations;

namespace SolarMicrogrid.Api.Dtos.Users;

/// <summary>
/// Details that may be changed on an existing account.
/// </summary>
public sealed class UpdateUserRequestDto
{
    /// <summary>Full name of the account holder.</summary>
    [Required(AllowEmptyStrings = false, ErrorMessage = "Full name is required.")]
    [MaxLength(120)]
    public string FullName { get; init; } = string.Empty;

    /// <summary>Email address. Must remain unique across all accounts.</summary>
    [Required(AllowEmptyStrings = false, ErrorMessage = "Email address is required.")]
    [EmailAddress(ErrorMessage = "Enter a valid email address.")]
    [MaxLength(100)]
    public string Email { get; init; } = string.Empty;

    /// <summary>Contact telephone number, 10 digits.</summary>
    [Required(AllowEmptyStrings = false, ErrorMessage = "Phone number is required.")]
    [RegularExpression(@"^0\d{9}$", ErrorMessage = "Enter a valid 10 digit phone number, e.g. 0771234567.")]
    public string Phone { get; init; } = string.Empty;

    /// <summary>Postal address.</summary>
    [MaxLength(200)]
    public string Address { get; init; } = string.Empty;

    /// <summary>
    /// Rated output of the solar array in kilowatts. Ignored for staff accounts,
    /// which have no panels.
    /// </summary>
    [Range(0, 1000, ErrorMessage = "Solar capacity must be between 0 and 1000 kW.")]
    public double SolarCapacityKw { get; init; }
}
