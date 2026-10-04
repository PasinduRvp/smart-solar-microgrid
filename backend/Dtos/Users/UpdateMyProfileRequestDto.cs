/*
 * ---------------------------------------------------------------------------
 * File        : UpdateMyProfileRequestDto.cs
 * Project     : Smart Solar Microgrid Trading System — SE4040 Assignment 1
 * Author      : <Your Full Name> (<IT Number>)
 * Created     : 2026-09-20
 * Description : The details a signed in user may change on their own account.
 *
 * Security    : NIC, Role and Status are absent by design. A person editing
 *               their own profile must not be able to rename their key,
 *               promote themselves to Backoffice, or reactivate an account an
 *               officer deactivated. Those are not guarded by a check here —
 *               the request simply has nowhere to put them, which is stronger,
 *               because a check can be forgotten and a missing field cannot.
 * ---------------------------------------------------------------------------
 */

using System.ComponentModel.DataAnnotations;

namespace SolarMicrogrid.Api.Dtos.Users;

/// <summary>
/// Changes a user makes to their own profile.
/// </summary>
public sealed class UpdateMyProfileRequestDto
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
    /// Rated output of the solar array in kilowatts. Applies to prosumers only
    /// and is ignored for staff accounts, which have no panels.
    /// </summary>
    [Range(0, 1000, ErrorMessage = "Solar capacity must be between 0 and 1000 kW.")]
    public double SolarCapacityKw { get; init; }
}
