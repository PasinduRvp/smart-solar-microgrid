/*
 * ---------------------------------------------------------------------------
 * File        : ProsumerService.cs
 * Project     : Smart Solar Microgrid Trading System — SE4040 Assignment 1
 * Author      : Vidvanga W A U (IT 23293694)
 * Created     : 2026-09-19
 * Description : Every rule governing a prosumer account's lifecycle. Both the
 *               React back office and the Android application reach these rules
 *               through the API, so a prosumer editing their profile on the
 *               phone and an officer editing it on the web are held to exactly
 *               the same conditions.
 *
 * Business    : BR-5  Only a Backoffice officer may reactivate an account. A
 *                     Grid Operator calling activate is refused.
 *               BR-6  A prosumer may request deactivation but cannot carry it
 *                     out; the request is completed by the back office.
 *               BR-7  Activation is what moves a self registered account out of
 *                     Pending and allows it to sign in.
 *
 * SOLID       : Single Responsibility — prosumer lifecycle rules only.
 *               Dependency Inversion — every collaborator is an interface.
 * Security    : Ownership is decided from ICurrentUser, which reads the signed
 *               token, never from a value in the request body. A prosumer who
 *               asks to edit another NIC is refused even though the endpoint
 *               allows their role through.
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

/// <inheritdoc cref="IProsumerService" />
public sealed partial class ProsumerService : IProsumerService
{
    private const string ProsumerNotFoundMessage = "No prosumer found with that NIC.";

    private readonly IUserRepository _userRepository;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<ProsumerService> _logger;

    /// <summary>Receives its collaborators from the DI container.</summary>
    public ProsumerService(
        IUserRepository userRepository,
        ICurrentUser currentUser,
        ILogger<ProsumerService> logger)
    {
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<ServiceResult<IReadOnlyList<UserResponseDto>>> GetProsumersAsync(
        AccountStatus? status,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<User> prosumers = status is null
            ? await _userRepository
                .FindAsync(user => user.Role == UserRole.Prosumer, cancellationToken)
                .ConfigureAwait(false)
            : await _userRepository
                .FindAsync(user => user.Role == UserRole.Prosumer && user.Status == status, cancellationToken)
                .ConfigureAwait(false);

        IReadOnlyList<UserResponseDto> dtos = prosumers
            .OrderByDescending(user => user.CreatedAt)
            .ToResponseDtos();

        return ServiceResult.Success(dtos);
    }

    /// <inheritdoc />
    public async Task<ServiceResult<IReadOnlyList<UserResponseDto>>> GetPendingActivationsAsync(
        CancellationToken cancellationToken = default)
    {
        // Oldest first: the account that has been waiting longest is the one an
        // officer should deal with next.
        IReadOnlyList<User> pending = await _userRepository
            .FindAsync(
                user => user.Role == UserRole.Prosumer && user.Status == AccountStatus.Pending,
                cancellationToken)
            .ConfigureAwait(false);

        IReadOnlyList<UserResponseDto> dtos = pending
            .OrderBy(user => user.CreatedAt)
            .ToResponseDtos();

        return ServiceResult.Success(dtos);
    }

    /// <inheritdoc />
    public async Task<ServiceResult<UserResponseDto>> GetByNicAsync(
        string nic,
        CancellationToken cancellationToken = default)
    {
        User? prosumer = await FindProsumerAsync(nic, cancellationToken).ConfigureAwait(false);
        if (prosumer is null)
        {
            return ServiceResult.Failure<UserResponseDto>(ServiceErrorType.NotFound, ProsumerNotFoundMessage);
        }

        // A prosumer may read their own record; staff may read any.
        if (!CallerMayAccess(prosumer.Nic))
        {
            return ServiceResult.Failure<UserResponseDto>(
                ServiceErrorType.Forbidden, "You may only view your own profile.");
        }

        return ServiceResult.Success(prosumer.ToResponseDto());
    }

    /// <inheritdoc />
    public async Task<ServiceResult<UserResponseDto>> UpdateProfileAsync(
        string nic,
        UpdateUserRequestDto request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        User? prosumer = await FindProsumerAsync(nic, cancellationToken).ConfigureAwait(false);
        if (prosumer is null)
        {
            return ServiceResult.Failure<UserResponseDto>(ServiceErrorType.NotFound, ProsumerNotFoundMessage);
        }

        // Only the account holder may edit these details — not a Backoffice
        // officer and not a Grid Operator. Staff administer an account's
        // LIFECYCLE (activate, deactivate) but its personal details belong to
        // the person they describe. The comparison is against the NIC in the
        // signed token, so supplying someone else's NIC in the URL achieves
        // nothing.
        if (!string.Equals(_currentUser.Nic, prosumer.Nic, StringComparison.Ordinal))
        {
            return ServiceResult.Failure<UserResponseDto>(
                ServiceErrorType.Forbidden,
                "Only the account holder can change these details. Staff can activate or deactivate the account.");
        }

        string email = request.Email.Trim();

        User? emailOwner = await _userRepository
            .GetByEmailAsync(email, cancellationToken)
            .ConfigureAwait(false);

        if (emailOwner is not null && emailOwner.Id != prosumer.Id)
        {
            return ServiceResult.Failure<UserResponseDto>(
                ServiceErrorType.Conflict, "Another account already uses this email address.");
        }

        // NIC, Role and Status are not touched: the DTO carries no field for
        // them, so a profile edit can never change who someone is or what they
        // are allowed to do.
        prosumer.FullName = request.FullName.Trim();
        prosumer.Email = email;
        prosumer.Phone = request.Phone.Trim();
        prosumer.Address = request.Address.Trim();
        prosumer.SolarCapacityKw = request.SolarCapacityKw;

        bool updated = await _userRepository.UpdateAsync(prosumer, cancellationToken).ConfigureAwait(false);
        if (!updated)
        {
            return ServiceResult.Failure<UserResponseDto>(ServiceErrorType.NotFound, ProsumerNotFoundMessage);
        }

        LogProfileUpdated(prosumer.Nic, _currentUser.UserId ?? "system");
        return ServiceResult.Success(prosumer.ToResponseDto());
    }

    /// <inheritdoc />
    public async Task<ServiceResult<UserResponseDto>> ActivateAsync(
        string nic,
        CancellationToken cancellationToken = default)
    {
        User? prosumer = await FindProsumerAsync(nic, cancellationToken).ConfigureAwait(false);
        if (prosumer is null)
        {
            return ServiceResult.Failure<UserResponseDto>(ServiceErrorType.NotFound, ProsumerNotFoundMessage);
        }

        // BR-5. The controller already restricts this endpoint to Backoffice,
        // but the rule is stated here as well so it holds wherever the service
        // is called from — including any future endpoint or background job.
        if (!_currentUser.IsBackoffice)
        {
            return ServiceResult.Failure<UserResponseDto>(
                ServiceErrorType.Forbidden,
                "Only a Backoffice officer can activate an account.");
        }

        if (prosumer.Status == AccountStatus.Active)
        {
            return ServiceResult.Failure<UserResponseDto>(
                ServiceErrorType.Conflict, "This account is already active.");
        }

        prosumer.Status = AccountStatus.Active;

        // Activating settles any outstanding deactivation request, so the
        // account does not reappear in the back office queue afterwards.
        prosumer.DeactivationRequested = false;

        await _userRepository.UpdateAsync(prosumer, cancellationToken).ConfigureAwait(false);
        LogAccountActivated(prosumer.Nic, _currentUser.UserId ?? "system");

        return ServiceResult.Success(prosumer.ToResponseDto());
    }

    /// <inheritdoc />
    public async Task<ServiceResult<UserResponseDto>> DeactivateAsync(
        string nic,
        CancellationToken cancellationToken = default)
    {
        User? prosumer = await FindProsumerAsync(nic, cancellationToken).ConfigureAwait(false);
        if (prosumer is null)
        {
            return ServiceResult.Failure<UserResponseDto>(ServiceErrorType.NotFound, ProsumerNotFoundMessage);
        }

        if (prosumer.Status == AccountStatus.Deactivated)
        {
            return ServiceResult.Failure<UserResponseDto>(
                ServiceErrorType.Conflict, "This account is already deactivated.");
        }

        prosumer.Status = AccountStatus.Deactivated;
        prosumer.DeactivationRequested = false;

        await _userRepository.UpdateAsync(prosumer, cancellationToken).ConfigureAwait(false);
        LogAccountDeactivated(prosumer.Nic, _currentUser.UserId ?? "system");

        return ServiceResult.Success(prosumer.ToResponseDto());
    }

    /// <inheritdoc />
    public async Task<ServiceResult<UserResponseDto>> RequestDeactivationAsync(
        string nic,
        CancellationToken cancellationToken = default)
    {
        User? prosumer = await FindProsumerAsync(nic, cancellationToken).ConfigureAwait(false);
        if (prosumer is null)
        {
            return ServiceResult.Failure<UserResponseDto>(ServiceErrorType.NotFound, ProsumerNotFoundMessage);
        }

        // A prosumer may only raise this request for their own account.
        if (!CallerMayAccess(prosumer.Nic))
        {
            return ServiceResult.Failure<UserResponseDto>(
                ServiceErrorType.Forbidden, "You may only request deactivation of your own account.");
        }

        if (prosumer.Status == AccountStatus.Deactivated)
        {
            return ServiceResult.Failure<UserResponseDto>(
                ServiceErrorType.Conflict, "This account is already deactivated.");
        }

        // BR-6. Note what does NOT happen here: Status is untouched. The
        // prosumer is raising a request, and the account stays usable until a
        // Backoffice officer acts on it. Setting Status directly would let a
        // prosumer deactivate themselves, which the assignment forbids.
        prosumer.DeactivationRequested = true;

        await _userRepository.UpdateAsync(prosumer, cancellationToken).ConfigureAwait(false);
        LogDeactivationRequested(prosumer.Nic);

        return ServiceResult.Success(prosumer.ToResponseDto());
    }

    /// <summary>
    /// Loads a prosumer by NIC, ignoring staff accounts so that a staff NIC
    /// cannot be administered through the prosumer endpoints.
    /// </summary>
    private async Task<User?> FindProsumerAsync(string nic, CancellationToken cancellationToken)
    {
        User? user = await _userRepository
            .GetByNicAsync(nic?.Trim() ?? string.Empty, cancellationToken)
            .ConfigureAwait(false);

        return user?.Role == UserRole.Prosumer ? user : null;
    }

    /// <summary>
    /// True when the caller is staff, or is the prosumer who owns the record.
    /// </summary>
    private bool CallerMayAccess(string ownerNic)
    {
        if (_currentUser.Role is null)
        {
            return false;
        }

        // Staff administer every prosumer; a prosumer reaches only their own.
        if (_currentUser.Role != UserRole.Prosumer)
        {
            return true;
        }

        return string.Equals(_currentUser.Nic, ownerNic, StringComparison.Ordinal);
    }

    // -----------------------------------------------------------------------
    // Source generated log methods. See the note in MongoContext.cs.
    // -----------------------------------------------------------------------

    [LoggerMessage(
        EventId = 4101,
        Level = LogLevel.Information,
        Message = "Prosumer {Nic} profile updated by {ActorId}.")]
    private partial void LogProfileUpdated(string nic, string actorId);

    [LoggerMessage(
        EventId = 4102,
        Level = LogLevel.Information,
        Message = "Prosumer {Nic} activated by Backoffice officer {ActorId}.")]
    private partial void LogAccountActivated(string nic, string actorId);

    [LoggerMessage(
        EventId = 4103,
        Level = LogLevel.Warning,
        Message = "Prosumer {Nic} deactivated by {ActorId}.")]
    private partial void LogAccountDeactivated(string nic, string actorId);

    [LoggerMessage(
        EventId = 4104,
        Level = LogLevel.Information,
        Message = "Prosumer {Nic} requested account deactivation.")]
    private partial void LogDeactivationRequested(string nic);
}
