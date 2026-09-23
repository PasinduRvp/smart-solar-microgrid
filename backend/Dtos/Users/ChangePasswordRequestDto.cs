/*
 * ---------------------------------------------------------------------------
 * File        : ChangePasswordRequestDto.cs
 * Project     : Smart Solar Microgrid Trading System — SE4040 Assignment 1
 * Author      : <Your Full Name> (<IT Number>)
 * Created     : 2026-09-20
 * Description : A request to change one's own password.
 *
 * Security    : The CURRENT password is required as well as the new one. A
 *               valid token alone is not enough: if someone walked up to an
 *               unlocked machine, or a token were stolen, they could otherwise
 *               lock the real owner out of their own account. Asking for the
 *               existing password proves the person at the keyboard is the
 *               account holder and not merely holding their session.
 * ---------------------------------------------------------------------------
 */

using System.ComponentModel.DataAnnotations;

namespace SolarMicrogrid.Api.Dtos.Users;

/// <summary>
/// Details required to change an account password.
/// </summary>
public sealed class ChangePasswordRequestDto
{
    /// <summary>The password currently on the account.</summary>
    [Required(AllowEmptyStrings = false, ErrorMessage = "Enter your current password.")]
    [MaxLength(128)]
    public string CurrentPassword { get; init; } = string.Empty;

    /// <summary>The replacement password.</summary>
    [Required(AllowEmptyStrings = false, ErrorMessage = "Enter a new password.")]
    [MinLength(8, ErrorMessage = "The new password must be at least 8 characters.")]
    [MaxLength(128)]
    public string NewPassword { get; init; } = string.Empty;
}
