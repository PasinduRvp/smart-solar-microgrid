/*
 * ---------------------------------------------------------------------------
 * File        : CreateStaffUserRequestDto.cs
 * Project     : Smart Solar Microgrid Trading System — SE4040 Assignment 1
 * Author      : <Your Full Name> (<IT Number>)
 * Created     : 2026-09-19
 * Description : Details a Backoffice officer supplies when creating a web
 *               application user, which the assignment defines as either a
 *               Backoffice officer or a Grid Operator.
 *
 * Security    : Unlike prosumer self registration, the role IS accepted here —
 *               but only because the endpoint itself is restricted to callers
 *               who already hold the Backoffice role. The service validates
 *               that the requested role is one of the two staff roles, so this
 *               endpoint cannot be used to mint a Prosumer account that would
 *               bypass the pending activation rule.
 * ---------------------------------------------------------------------------
 */

using System.ComponentModel.DataAnnotations;

namespace SolarMicrogrid.Api.Dtos.Users;

/// <summary>
/// Details for a new Backoffice or Grid Operator account.
/// </summary>
public sealed class CreateStaffUserRequestDto
{
    /// <summary>National Identity Card number. Must be unique across all accounts.</summary>
    [Required(AllowEmptyStrings = false, ErrorMessage = "NIC is required.")]
    [RegularExpression(@"^(\d{9}[VvXx]|\d{12})$", ErrorMessage =
        "Enter a valid NIC: either 9 digits followed by V or X, or 12 digits.")]
    public string Nic { get; init; } = string.Empty;

    /// <summary>Full name of the staff member.</summary>
    [Required(AllowEmptyStrings = false, ErrorMessage = "Full name is required.")]
    [MaxLength(120)]
    public string FullName { get; init; } = string.Empty;

    /// <summary>Email address, used to sign in. Must be unique.</summary>
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
    /// Role to assign. Must be "Backoffice" or "GridOperator"; the service
    /// rejects any other value, including "Prosumer".
    /// </summary>
    [Required(AllowEmptyStrings = false, ErrorMessage = "Role is required.")]
    [RegularExpression("^(Backoffice|GridOperator)$", ErrorMessage =
        "Role must be either Backoffice or GridOperator.")]
    public string Role { get; init; } = string.Empty;

    /// <summary>Initial password for the account.</summary>
    [Required(AllowEmptyStrings = false, ErrorMessage = "Password is required.")]
    [MinLength(8, ErrorMessage = "Password must be at least 8 characters.")]
    [MaxLength(128)]
    public string Password { get; init; } = string.Empty;
}
