/*
 * ---------------------------------------------------------------------------
 * File        : AuthResponseDto.cs
 * Project     : Smart Solar Microgrid Trading System — SE4040 Assignment 1
 * Author      : <Your Full Name> (<IT Number>)
 * Created     : 2026-09-19
 * Description : Returned after a successful sign in. The clients store the
 *               token and send it as a bearer header on every later request;
 *               the Android client also persists it in SQLite.
 *
 * Security    : This is why the User entity is never serialised straight to a
 *               client. A DTO states exactly which fields leave the service, so
 *               PasswordHash cannot be exposed by accident when a new field is
 *               added to the entity later.
 * ---------------------------------------------------------------------------
 */

namespace SolarMicrogrid.Api.Dtos.Auth;

/// <summary>
/// Result of a successful authentication.
/// </summary>
/// <param name="Token">Signed JWT to send as "Authorization: Bearer {token}".</param>
/// <param name="ExpiresAtUtc">When the token stops being accepted.</param>
/// <param name="UserId">Identifier of the signed in account.</param>
/// <param name="Nic">National Identity Card number of the account holder.</param>
/// <param name="FullName">Display name for the client's home screen.</param>
/// <param name="Email">Email address of the account.</param>
/// <param name="Role">Role name, used by the clients to choose the home screen.</param>
public sealed record AuthResponseDto(
    string Token,
    DateTime ExpiresAtUtc,
    string UserId,
    string Nic,
    string FullName,
    string Email,
    string Role);
