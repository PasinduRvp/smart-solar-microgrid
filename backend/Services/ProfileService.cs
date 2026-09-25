/*
 * ---------------------------------------------------------------------------
 * File        : ProfileService.cs
 * Project     : Smart Solar Microgrid Trading System — SE4040 Assignment 1
 * Author      : <Your Full Name> (<IT Number>)
 * Created     : 2026-09-20
 * Description : Self service account operations. Every method here acts on the
 *               caller's own account and on no other.
 *
 * Security    : The account is identified from ICurrentUser, which reads the
 *               validated token. No method takes an account id, so there is no
 *               value a caller could supply to aim one of these operations at
 *               somebody else's record.
 *
 *               What cannot be changed here is as important as what can:
 *               NIC, role and account status are all absent from the request,
 *               so a user cannot rename their own key, promote themselves, or
 *               reactivate an account an officer deactivated.
 * ---------------------------------------------------------------------------
 */

using SolarMicrogrid.Api.Common;
using SolarMicrogrid.Api.Dtos.Users;
using SolarMicrogrid.Api.Mappings;
using SolarMicrogrid.Api.Models;
using SolarMicrogrid.Api.Models.Enums;
using SolarMicrogrid.Api.Repositories;
using SolarMicrogrid.Api.Security;

namespace SolarMicrogrid.Api.Services;

/// <inheritdoc cref="IProfileService" />
public sealed partial class ProfileService : IProfileService
{
    private const string NotSignedInMessage = "You are not signed in.";

    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<ProfileService> _logger;

    /// <summary>Receives its collaborators from the DI container.</summary>
    public ProfileService(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        ICurrentUser currentUser,
        ILogger<ProfileService> logger)
    {
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _passwordHasher = passwordHasher ?? throw new ArgumentNullException(nameof(passwordHasher));
        _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<ServiceResult<UserResponseDto>> GetMyProfileAsync(
        CancellationToken cancellationToken = default)
    {
        User? user = await LoadCallerAsync(cancellationToken).ConfigureAwait(false);

        return user is null
            ? ServiceResult.Failure<UserResponseDto>(ServiceErrorType.Unauthorized, NotSignedInMessage)
            : ServiceResult.Success(user.ToResponseDto());
    }

    /// <inheritdoc />
    public async Task<ServiceResult<UserResponseDto>> UpdateMyProfileAsync(
        UpdateMyProfileRequestDto request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        User? user = await LoadCallerAsync(cancellationToken).ConfigureAwait(false);
        if (user is null)
        {
            return ServiceResult.Failure<UserResponseDto>(ServiceErrorType.Unauthorized, NotSignedInMessage);
        }

        string email = request.Email.Trim();

        // The unique index would reject a clash anyway, but checking here gives
        // a message that names the problem. The comparison excludes this user,
        // so saving an unchanged email does not conflict with itself.
        User? emailOwner = await _userRepository
            .GetByEmailAsync(email, cancellationToken)
            .ConfigureAwait(false);

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

        await _userRepository.UpdateAsync(user, cancellationToken).ConfigureAwait(false);
        LogProfileUpdated(user.Id ?? string.Empty);

        return ServiceResult.Success(user.ToResponseDto());
    }

    /// <inheritdoc />
    public async Task<ServiceResult<bool>> ChangeMyPasswordAsync(
        ChangePasswordRequestDto request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        User? user = await LoadCallerAsync(cancellationToken).ConfigureAwait(false);
        if (user is null)
        {
            return ServiceResult.Failure<bool>(ServiceErrorType.Unauthorized, NotSignedInMessage);
        }

        // Holding a valid token is not enough. If someone walked up to an
        // unlocked machine they could otherwise lock the real owner out of
        // their own account, so the existing password must be proved.
        if (!_passwordHasher.Verify(request.CurrentPassword, user.PasswordHash))
        {
            LogPasswordChangeRefused(user.Id ?? string.Empty);

            return ServiceResult.Failure<bool>(
                ServiceErrorType.Unauthorized, "Your current password is not correct.");
        }

        if (string.Equals(request.CurrentPassword, request.NewPassword, StringComparison.Ordinal))
        {
            return ServiceResult.Failure<bool>(
                ServiceErrorType.Validation, "The new password must be different from the current one.");
        }

        user.PasswordHash = _passwordHasher.Hash(request.NewPassword);

        await _userRepository.UpdateAsync(user, cancellationToken).ConfigureAwait(false);
        LogPasswordChanged(user.Id ?? string.Empty);

        // Note for the client: tokens already issued stay valid until they
        // expire. Revoking them would need a token blacklist, which this
        // system does not have; the short token lifetime limits the exposure.
        return ServiceResult.Success(true);
    }

    /// <summary>
    /// Loads the account behind the current token, or null when the caller is
    /// not signed in or the account has since been removed.
    /// </summary>
    private async Task<User?> LoadCallerAsync(CancellationToken cancellationToken)
    {
        string? userId = _currentUser.UserId;

        if (string.IsNullOrWhiteSpace(userId))
        {
            return null;
        }

        return await _userRepository.GetByIdAsync(userId, cancellationToken).ConfigureAwait(false);
    }

    // -----------------------------------------------------------------------
    // Source generated log methods. See the note in MongoContext.cs.
    // -----------------------------------------------------------------------

    [LoggerMessage(
        EventId = 4201,
        Level = LogLevel.Information,
        Message = "User {UserId} updated their own profile.")]
    private partial void LogProfileUpdated(string userId);

    [LoggerMessage(
        EventId = 4202,
        Level = LogLevel.Information,
        Message = "User {UserId} changed their own password.")]
    private partial void LogPasswordChanged(string userId);

    /// <remarks>
    /// Logged at Warning: repeated failures here may mean someone is using a
    /// session they should not have.
    /// </remarks>
    [LoggerMessage(
        EventId = 4203,
        Level = LogLevel.Warning,
        Message = "Password change refused for user {UserId}: current password incorrect.")]
    private partial void LogPasswordChangeRefused(string userId);
}
