using SolarMicrogrid.Api.Common;
using SolarMicrogrid.Api.Dtos.Users;
using SolarMicrogrid.Api.Mappings;
using SolarMicrogrid.Api.Models;
using SolarMicrogrid.Api.Models.Enums;
using SolarMicrogrid.Api.Repositories;
using SolarMicrogrid.Api.Security;

namespace SolarMicrogrid.Api.Services;

public sealed class ProsumerService : IProsumerService
{
    private const string ProsumerNotFoundMessage = "No prosumer found with that NIC.";
    private readonly IUserRepository _userRepository;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<ProsumerService> _logger;

    public ProsumerService(
        IUserRepository userRepository,
        ICurrentUser currentUser,
        ILogger<ProsumerService> logger)
    {
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<ServiceResult<IReadOnlyList<UserResponseDto>>> GetProsumersAsync(
        AccountStatus? status,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<User> prosumers = status is null
            ? await _userRepository.FindAsync(user => user.Role == UserRole.Prosumer, cancellationToken).ConfigureAwait(false)
            : await _userRepository.FindAsync(user => user.Role == UserRole.Prosumer && user.Status == status, cancellationToken).ConfigureAwait(false);

        return ServiceResult.Success(prosumers.OrderByDescending(user => user.CreatedAt).ToResponseDtos());
    }

    public async Task<ServiceResult<IReadOnlyList<UserResponseDto>>> GetPendingActivationsAsync(
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<User> pending = await _userRepository
            .FindAsync(user => user.Role == UserRole.Prosumer && user.Status == AccountStatus.Pending, cancellationToken)
            .ConfigureAwait(false);

        return ServiceResult.Success(pending.OrderBy(user => user.CreatedAt).ToResponseDtos());
    }

    public async Task<ServiceResult<UserResponseDto>> GetByNicAsync(
        string nic,
        CancellationToken cancellationToken = default)
    {
        User? prosumer = await FindProsumerAsync(nic, cancellationToken).ConfigureAwait(false);
        if (prosumer is null)
        {
            return NotFound();
        }

        if (!CallerMayAccess(prosumer.Nic))
        {
            return ServiceResult.Failure<UserResponseDto>(ServiceErrorType.Forbidden, "You may only view your own profile.");
        }

        return ServiceResult.Success(prosumer.ToResponseDto());
    }

    public async Task<ServiceResult<UserResponseDto>> UpdateProfileAsync(
        string nic,
        UpdateUserRequestDto request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        User? prosumer = await FindProsumerAsync(nic, cancellationToken).ConfigureAwait(false);
        if (prosumer is null)
        {
            return NotFound();
        }

        if (!string.Equals(_currentUser.Nic, prosumer.Nic, StringComparison.Ordinal))
        {
            return ServiceResult.Failure<UserResponseDto>(ServiceErrorType.Forbidden, "Only the account holder can change these details.");
        }

        string email = request.Email.Trim();
        User? emailOwner = await _userRepository.GetByEmailAsync(email, cancellationToken).ConfigureAwait(false);
        if (emailOwner is not null && emailOwner.Id != prosumer.Id)
        {
            return ServiceResult.Failure<UserResponseDto>(ServiceErrorType.Conflict, "Another account already uses this email address.");
        }

        prosumer.FullName = request.FullName.Trim();
        prosumer.Email = email;
        prosumer.Phone = request.Phone.Trim();
        prosumer.Address = request.Address.Trim();
        prosumer.SolarCapacityKw = request.SolarCapacityKw;

        if (!await _userRepository.UpdateAsync(prosumer, cancellationToken).ConfigureAwait(false))
        {
            return NotFound();
        }

        _logger.LogInformation("Prosumer {Nic} profile updated by {ActorId}.", prosumer.Nic, _currentUser.UserId ?? "system");
        return ServiceResult.Success(prosumer.ToResponseDto());
    }

    public async Task<ServiceResult<UserResponseDto>> ActivateAsync(
        string nic,
        CancellationToken cancellationToken = default)
    {
        User? prosumer = await FindProsumerAsync(nic, cancellationToken).ConfigureAwait(false);
        if (prosumer is null)
        {
            return NotFound();
        }

        if (!_currentUser.IsBackoffice)
        {
            return ServiceResult.Failure<UserResponseDto>(ServiceErrorType.Forbidden, "Only a Backoffice officer can activate an account.");
        }

        if (prosumer.Status == AccountStatus.Active)
        {
            return ServiceResult.Failure<UserResponseDto>(ServiceErrorType.Conflict, "This account is already active.");
        }

        prosumer.Status = AccountStatus.Active;
        prosumer.DeactivationRequested = false;
        await _userRepository.UpdateAsync(prosumer, cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("Prosumer {Nic} activated by {ActorId}.", prosumer.Nic, _currentUser.UserId ?? "system");
        return ServiceResult.Success(prosumer.ToResponseDto());
    }

    public async Task<ServiceResult<UserResponseDto>> DeactivateAsync(
        string nic,
        CancellationToken cancellationToken = default)
    {
        User? prosumer = await FindProsumerAsync(nic, cancellationToken).ConfigureAwait(false);
        if (prosumer is null)
        {
            return NotFound();
        }

        if (_currentUser.Role is not (UserRole.Backoffice or UserRole.GridOperator))
        {
            return ServiceResult.Failure<UserResponseDto>(ServiceErrorType.Forbidden, "Only a Backoffice officer or Grid Operator can deactivate an account.");
        }

        if (prosumer.Status == AccountStatus.Deactivated)
        {
            return ServiceResult.Failure<UserResponseDto>(ServiceErrorType.Conflict, "This account is already deactivated.");
        }

        prosumer.Status = AccountStatus.Deactivated;
        prosumer.DeactivationRequested = false;
        await _userRepository.UpdateAsync(prosumer, cancellationToken).ConfigureAwait(false);
        _logger.LogWarning("Prosumer {Nic} deactivated by {ActorId}.", prosumer.Nic, _currentUser.UserId ?? "system");
        return ServiceResult.Success(prosumer.ToResponseDto());
    }

    public async Task<ServiceResult<UserResponseDto>> RequestDeactivationAsync(
        string nic,
        CancellationToken cancellationToken = default)
    {
        User? prosumer = await FindProsumerAsync(nic, cancellationToken).ConfigureAwait(false);
        if (prosumer is null)
        {
            return NotFound();
        }

        if (!CallerMayAccess(prosumer.Nic))
        {
            return ServiceResult.Failure<UserResponseDto>(ServiceErrorType.Forbidden, "You may only request deactivation of your own account.");
        }

        if (prosumer.Status == AccountStatus.Deactivated)
        {
            return ServiceResult.Failure<UserResponseDto>(ServiceErrorType.Conflict, "This account is already deactivated.");
        }

        prosumer.DeactivationRequested = true;
        await _userRepository.UpdateAsync(prosumer, cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("Prosumer {Nic} requested account deactivation.", prosumer.Nic);
        return ServiceResult.Success(prosumer.ToResponseDto());
    }

    private async Task<User?> FindProsumerAsync(string nic, CancellationToken cancellationToken)
    {
        User? user = await _userRepository.GetByNicAsync(nic?.Trim() ?? string.Empty, cancellationToken).ConfigureAwait(false);
        return user?.Role == UserRole.Prosumer ? user : null;
    }

    private bool CallerMayAccess(string ownerNic)
    {
        if (_currentUser.Role is null)
        {
            return false;
        }

        return _currentUser.Role != UserRole.Prosumer || string.Equals(_currentUser.Nic, ownerNic, StringComparison.Ordinal);
    }

    private static ServiceResult<UserResponseDto> NotFound() =>
        ServiceResult.Failure<UserResponseDto>(ServiceErrorType.NotFound, ProsumerNotFoundMessage);
}
