/*
 * ---------------------------------------------------------------------------
 * File        : UserMappings.cs
 * Project     : Smart Solar Microgrid Trading System — SE4040 Assignment 1
 * Author      : <Your Full Name> (<IT Number>)
 * Created     : 2026-09-19
 * Description : Converts User entities into the DTOs the API returns.
 *
 * Code smell  : Written once and reused by both UserService and
 *               ProsumerService. Mapping the same eleven fields by hand in
 *               several services would be duplicated code, and the copy that
 *               someone forgot to update is exactly where a password hash
 *               would eventually leak.
 * Design note : Mapping is done explicitly rather than with a library such as
 *               AutoMapper. For a model this size the hand written version is
 *               shorter overall, has no configuration to get wrong, fails at
 *               compile time rather than at run time, and can be read and
 *               explained line by line.
 * SOLID       : Single Responsibility — translation between layers only.
 * ---------------------------------------------------------------------------
 */

using SolarMicrogrid.Api.Dtos.Users;
using SolarMicrogrid.Api.Models;

namespace SolarMicrogrid.Api.Mappings;

/// <summary>
/// Extension methods that project user entities onto response DTOs.
/// </summary>
public static class UserMappings
{
    /// <summary>
    /// Builds the response DTO for a single account.
    /// PasswordHash is deliberately not copied.
    /// </summary>
    public static UserResponseDto ToResponseDto(this User user)
    {
        ArgumentNullException.ThrowIfNull(user);

        return new UserResponseDto(
            Id: user.Id ?? string.Empty,
            Nic: user.Nic,
            FullName: user.FullName,
            Email: user.Email,
            Phone: user.Phone,
            Address: user.Address,
            Role: user.Role.ToString(),
            Status: user.Status.ToString(),
            DeactivationRequested: user.DeactivationRequested,
            SolarCapacityKw: user.SolarCapacityKw,
            CreatedAt: user.CreatedAt,
            UpdatedAt: user.UpdatedAt);
    }

    /// <summary>Builds response DTOs for a collection of accounts.</summary>
    public static IReadOnlyList<UserResponseDto> ToResponseDtos(this IEnumerable<User> users)
    {
        ArgumentNullException.ThrowIfNull(users);
        return users.Select(user => user.ToResponseDto()).ToList();
    }
}
