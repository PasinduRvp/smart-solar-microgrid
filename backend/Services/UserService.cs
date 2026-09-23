/*
 * ---------------------------------------------------------------------------
 * File        : UserService.cs
 * Project     : Smart Solar Microgrid Trading System — SE4040 Assignment 1
 * Author      : <Your Full Name> (<IT Number>)
 * Created     : 2026-09-19
 * Description : Rules for creating and maintaining Backoffice and Grid Operator
 *               accounts. Called from the web application's user management
 *               screens, which are open to Backoffice officers only.
 *
 * SOLID       : Single Responsibility — staff account rules only.
 *               Dependency Inversion — depends on IUserRepository,
 *               IPasswordHasher and ICurrentUser, all abstractions.
 * Security    : The role supplied by the caller is re-checked here rather than
 *               trusted from the request. The endpoint is already restricted to
 *               Backoffice callers, but validating again in the service means
 *               the rule holds even if a future controller forgets the
 *               attribute — defence in depth.
 * ---------------------------------------------------------------------------
 */

using MongoDB.Driver;
using SolarMicrogrid.Api.Common;
using SolarMicrogrid.Api.Dtos.Users;
using SolarMicrogrid.Api.Mappings;
using SolarMicrogrid.Api.Models;
using SolarMicrogrid.Api.Models.Enums;
using SolarMicrogrid.Api.Repositories;
using SolarMicrogrid.Api.Security;

namespace SolarMicrogrid.Api.Services;

/// <inheritdoc cref="IUserService" />
public sealed partial class UserService : IUserService
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<UserService> _logger;

    /// <summary>Receives its collaborators from the DI container.</summary>
    public UserService(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        ICurrentUser currentUser,
        ILogger<UserService> logger)
    {
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _passwordHasher = passwordHasher ?? throw new ArgumentNullException(nameof(passwordHasher));
        _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<ServiceResult<IReadOnlyList<UserResponseDto>>> GetStaffUsersAsync(
        CancellationToken cancellationToken = default)
    {
        // Prosumers are excluded: this screen administers staff only, and there
        // may eventually be far more prosumers than the page could show.
        IReadOnlyList<User> staff = await _userRepository
            .FindAsync(user => user.Role != UserRole.Prosumer, cancellationToken)
            .ConfigureAwait(false);

        IReadOnlyList<UserResponseDto> dtos = staff
            .OrderByDescending(user => user.CreatedAt)
            .ToResponseDtos();

        return ServiceResult.Success(dtos);
    }

    /// <inheritdoc />
    public async Task<ServiceResult<UserResponseDto>> GetByIdAsync(
        string id,
        CancellationToken cancellationToken = default)
    {
        User? user = await _userRepository.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);

        return user is null
            ? ServiceResult.Failure<UserResponseDto>(ServiceErrorType.NotFound, "User not found.")
            : ServiceResult.Success(user.ToResponseDto());
    }

    /// <inheritdoc />
    public async Task<ServiceResult<UserResponseDto>> CreateStaffUserAsync(
        CreateStaffUserRequestDto request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        // Re-validated here even though the DTO's regular expression already
        // limits the value, because the service must be correct on its own.
        if (!Enum.TryParse(request.Role, ignoreCase: false, out UserRole role)
            || role == UserRole.Prosumer)
        {
            return ServiceResult.Failure<UserResponseDto>(
                ServiceErrorType.Validation,
                "Role must be either Backoffice or GridOperator. Prosumers register through the mobile application.");
        }

        string nic = request.Nic.Trim();
        string email = request.Email.Trim();

        if (await _userRepository.GetByNicAsync(nic, cancellationToken).ConfigureAwait(false) is not null)
        {
            return ServiceResult.Failure<UserResponseDto>(
                ServiceErrorType.Conflict, "An account with this NIC already exists.");
        }

        if (await _userRepository.GetByEmailAsync(email, cancellationToken).ConfigureAwait(false) is not null)
        {
            return ServiceResult.Failure<UserResponseDto>(
                ServiceErrorType.Conflict, "An account with this email address already exists.");
        }

        User user = new()
        {
            Nic = nic,
            FullName = request.FullName.Trim(),
            Email = email,
            Phone = request.Phone.Trim(),
            Address = request.Address.Trim(),
            PasswordHash = _passwordHasher.Hash(request.Password),
            Role = role,

            // Staff accounts are usable immediately. The Pending state exists
            // for prosumers who register themselves and must be vetted; a
            // Backoffice officer creating this account has already done that.
            Status = AccountStatus.Active,
            CreatedBy = _currentUser.UserId,
        };

        try
        {
            User created = await _userRepository.InsertAsync(user, cancellationToken).ConfigureAwait(false);
            LogStaffUserCreated(created.Id ?? string.Empty, role.ToString(), _currentUser.UserId ?? "system");

            return ServiceResult.Success(created.ToResponseDto());
        }
        catch (MongoWriteException ex) when (ex.WriteError?.Category == ServerErrorCategory.DuplicateKey)
        {
            return ServiceResult.Failure<UserResponseDto>(
                ServiceErrorType.Conflict, "An account with this NIC or email address already exists.");
        }
    }

    /// <inheritdoc />
    public async Task<ServiceResult<UserResponseDto>> UpdateAsync(
        string id,
        UpdateUserRequestDto request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        User? user = await _userRepository.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
        if (user is null)
        {
            return ServiceResult.Failure<UserResponseDto>(ServiceErrorType.NotFound, "User not found.");
        }

        // An account's personal details belong to the person they describe, so
        // even a Backoffice officer may only edit their own. Administering
        // somebody else's account means creating it, or removing it — not
        // rewriting their name and contact details on their behalf.
        // Everybody edits themselves through /api/profile.
        if (!string.Equals(user.Id, _currentUser.UserId, StringComparison.Ordinal))
        {
            return ServiceResult.Failure<UserResponseDto>(
                ServiceErrorType.Forbidden,
                "Each user edits their own details from their profile page.");
        }

        string email = request.Email.Trim();

        // The email unique index would reject a clash anyway, but checking here
        // produces a message that names the problem instead of a duplicate key
        // error. The comparison excludes this user so saving an unchanged email
        // does not report a conflict with itself.
        User? emailOwner = await _userRepository.GetByEmailAsync(email, cancellationToken).ConfigureAwait(false);
        if (emailOwner is not null && emailOwner.Id != user.Id)
        {
            return ServiceResult.Failure<UserResponseDto>(
                ServiceErrorType.Conflict, "Another account already uses this email address.");
        }

        user.FullName = request.FullName.Trim();
        user.Email = email;
        user.Phone = request.Phone.Trim();
        user.Address = request.Address.Trim();

        // Only meaningful for prosumers; staff accounts have no solar array.
        if (user.Role == UserRole.Prosumer)
        {
            user.SolarCapacityKw = request.SolarCapacityKw;
        }

        bool updated = await _userRepository.UpdateAsync(user, cancellationToken).ConfigureAwait(false);
        if (!updated)
        {
            return ServiceResult.Failure<UserResponseDto>(ServiceErrorType.NotFound, "User not found.");
        }

        LogUserUpdated(user.Id ?? string.Empty, _currentUser.UserId ?? "system");
        return ServiceResult.Success(user.ToResponseDto());
    }

    /// <inheritdoc />
    public async Task<ServiceResult<bool>> DeleteStaffUserAsync(
        string id,
        CancellationToken cancellationToken = default)
    {
        User? user = await _userRepository.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
        if (user is null)
        {
            return ServiceResult.Failure<bool>(ServiceErrorType.NotFound, "User not found.");
        }

        // Prosumers own reservations. Deleting one would leave bookings
        // pointing at a NIC that no longer resolves, so they are deactivated
        // instead. This endpoint refuses rather than silently doing the wrong
        // thing.
        if (user.Role == UserRole.Prosumer)
        {
            return ServiceResult.Failure<bool>(
                ServiceErrorType.Conflict,
                "Prosumer accounts cannot be deleted. Deactivate the account instead.");
        }

        // An officer removing their own account would lock themselves out, and
        // if they were the last Backoffice officer nobody could administer the
        // system at all.
        if (string.Equals(user.Id, _currentUser.UserId, StringComparison.Ordinal))
        {
            return ServiceResult.Failure<bool>(
                ServiceErrorType.Conflict, "You cannot delete your own account.");
        }

        if (user.Role == UserRole.Backoffice)
        {
            long remainingBackoffice = await _userRepository
                .CountAsync(candidate => candidate.Role == UserRole.Backoffice, cancellationToken)
                .ConfigureAwait(false);

            // Removing the final officer would leave the system with nobody
            // able to create users, activate prosumers or manage stations.
            if (remainingBackoffice <= 1)
            {
                return ServiceResult.Failure<bool>(
                    ServiceErrorType.Conflict,
                    "This is the only Backoffice account. Create another before deleting this one.");
            }
        }

        bool deleted = await _userRepository.DeleteAsync(id, cancellationToken).ConfigureAwait(false);
        if (deleted)
        {
            LogStaffUserDeleted(id, _currentUser.UserId ?? "system");
        }

        return ServiceResult.Success(deleted);
    }

    // -----------------------------------------------------------------------
    // Source generated log methods. See the note in MongoContext.cs.
    // -----------------------------------------------------------------------

    [LoggerMessage(
        EventId = 4001,
        Level = LogLevel.Information,
        Message = "Staff user {UserId} created with role {Role} by {ActorId}.")]
    private partial void LogStaffUserCreated(string userId, string role, string actorId);

    [LoggerMessage(
        EventId = 4002,
        Level = LogLevel.Information,
        Message = "User {UserId} updated by {ActorId}.")]
    private partial void LogUserUpdated(string userId, string actorId);

    [LoggerMessage(
        EventId = 4003,
        Level = LogLevel.Warning,
        Message = "Staff user {UserId} deleted by {ActorId}.")]
    private partial void LogStaffUserDeleted(string userId, string actorId);
}
