/*
 * ---------------------------------------------------------------------------
 * File        : UserResponseDto.cs
 * Project     : Smart Solar Microgrid Trading System — SE4040 Assignment 1
 * Author      : <Your Full Name> (<IT Number>)
 * Created     : 2026-09-19
 * Description : The shape of a user account as returned by the API. Used for
 *               staff accounts and for prosumer profiles alike.
 *
 * Security    : This DTO is the boundary that keeps PasswordHash inside the
 *               service. Returning the User entity directly would serialise
 *               every property it has, so a field added to the entity later
 *               would silently start leaking. Listing the safe fields
 *               explicitly means exposure has to be a deliberate act.
 * ---------------------------------------------------------------------------
 */

namespace SolarMicrogrid.Api.Dtos.Users;

/// <summary>
/// A user account as presented to the web and mobile clients.
/// </summary>
/// <param name="Id">Document identifier.</param>
/// <param name="Nic">National Identity Card number, the prosumer business key.</param>
/// <param name="FullName">Full name of the account holder.</param>
/// <param name="Email">Email address.</param>
/// <param name="Phone">Contact telephone number.</param>
/// <param name="Address">Postal address.</param>
/// <param name="Role">Role name: Backoffice, GridOperator or Prosumer.</param>
/// <param name="Status">Lifecycle state: Pending, Active or Deactivated.</param>
/// <param name="DeactivationRequested">True when the prosumer has asked to be deactivated.</param>
/// <param name="SolarCapacityKw">Rated output of the prosumer's array, in kilowatts.</param>
/// <param name="CreatedAt">UTC time the account was created.</param>
/// <param name="UpdatedAt">UTC time the account was last changed.</param>
public sealed record UserResponseDto(
    string Id,
    string Nic,
    string FullName,
    string Email,
    string Phone,
    string Address,
    string Role,
    string Status,
    bool DeactivationRequested,
    double SolarCapacityKw,
    DateTime CreatedAt,
    DateTime UpdatedAt);
