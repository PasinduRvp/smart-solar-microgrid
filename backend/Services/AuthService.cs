/*
 * ---------------------------------------------------------------------------
 * File        : AuthService.cs
 * Project     : Smart Solar Microgrid Trading System — SE4040 Assignment 1
 * Author      : <Your Full Name> (<IT Number>)
 * Created     : 2026-09-19
 * Description : All sign in and self registration rules for the system. Both
 *               the React web application and the Android application call the
 *               endpoints backed by this class, so the rules below are applied
 *               identically to every client — which is the point of the FAT
 *               service pattern the assignment requires.
 *
 * Business    : BR-7  A prosumer registering on mobile starts as Pending and
 *                     cannot sign in until a Backoffice officer activates them.
 *               BR-5  A Deactivated account is refused sign in; only a
 *                     Backoffice officer can reactivate it.
 *
 * SOLID       : Single Responsibility — authentication rules only. Hashing is
 *               delegated to IPasswordHasher, token creation to ITokenService
 *               and persistence to IUserRepository. Each of those can be
 *               replaced without touching this class.
 * ---------------------------------------------------------------------------
 */

using MongoDB.Driver;
using SolarMicrogrid.Api.Common;
using SolarMicrogrid.Api.Dtos.Auth;
using SolarMicrogrid.Api.Models;
using SolarMicrogrid.Api.Models.Enums;
using SolarMicrogrid.Api.Repositories;
using SolarMicrogrid.Api.Security;

namespace SolarMicrogrid.Api.Services;

/// <inheritdoc cref="IAuthService" />
public sealed partial class AuthService : IAuthService
{
    /// <summary>
    /// One message for every failed sign in, whatever the real cause.
    /// Saying "no such user" would let an attacker confirm which NICs and email
    /// addresses exist on the system — a user enumeration weakness — so a wrong
    /// identifier and a wrong password are reported identically.
    /// </summary>
    private const string InvalidCredentialsMessage =
        "Invalid credentials. Check your email or NIC and password.";

    /// <summary>
    /// A valid BCrypt hash of a throwaway value. Used only to spend comparable
    /// time when the account does not exist, so that response timing does not
    /// reveal which accounts are real. See the note in LoginAsync.
    /// </summary>
    private const string DummyHash = "$2a$12$N9qo8uLOickgx2ZMRZoMyeIjZAgcfl7p92ldGxad68LJZdL17lhWy";

    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenService _tokenService;
    private readonly ILogger<AuthService> _logger;

    /// <summary>Receives its collaborators from the DI container.</summary>
    public AuthService(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        ITokenService tokenService,
        ILogger<AuthService> logger)
    {
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _passwordHasher = passwordHasher ?? throw new ArgumentNullException(nameof(passwordHasher));
        _tokenService = tokenService ?? throw new ArgumentNullException(nameof(tokenService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<ServiceResult<AuthResponseDto>> LoginAsync(
        LoginRequestDto request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        string identifier = request.Identifier.Trim();

        // Staff sign in with their email address, prosumers with their NIC.
        // Trying email first and falling back to NIC lets one endpoint serve
        // all three roles, so the sign in rules exist in exactly one place.
        bool looksLikeEmail = identifier.Contains('@', StringComparison.Ordinal);

        User? user = looksLikeEmail
            ? await _userRepository.GetByEmailAsync(identifier, cancellationToken).ConfigureAwait(false)
            : await _userRepository.GetByNicAsync(identifier, cancellationToken).ConfigureAwait(false);

        if (user is null)
        {
            // Verify against a dummy hash anyway so a missing account costs
            // about as much time as a real one. Returning immediately here
            // would let an attacker distinguish existing accounts from absent
            // ones purely by how quickly the request comes back.
            _passwordHasher.Verify(request.Password, DummyHash);
            LogFailedLogin(identifier);

            return ServiceResult.Failure<AuthResponseDto>(
                ServiceErrorType.Unauthorized, InvalidCredentialsMessage);
        }

        if (!_passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            LogFailedLogin(identifier);

            return ServiceResult.Failure<AuthResponseDto>(
                ServiceErrorType.Unauthorized, InvalidCredentialsMessage);
        }

        // The account exists and the password is correct, so the messages below
        // may be specific. They tell a legitimate user what to do next without
        // revealing anything to someone who does not know the password.
        if (user.Status == AccountStatus.Pending)
        {
            // BR-7: registered on mobile, still awaiting Backoffice approval.
            return ServiceResult.Failure<AuthResponseDto>(
                ServiceErrorType.Forbidden,
                "Your account is awaiting activation by a Backoffice officer.");
        }

        if (user.Status == AccountStatus.Deactivated)
        {
            // BR-5: only a Backoffice officer can bring this account back.
            return ServiceResult.Failure<AuthResponseDto>(
                ServiceErrorType.Forbidden,
                "This account has been deactivated. Contact a Backoffice officer to reactivate it.");
        }

        (string token, DateTime expiresAtUtc) = _tokenService.CreateToken(user);
        LogSuccessfulLogin(user.Id ?? string.Empty, user.Role.ToString());

        return ServiceResult.Success(new AuthResponseDto(
            Token: token,
            ExpiresAtUtc: expiresAtUtc,
            UserId: user.Id ?? string.Empty,
            Nic: user.Nic,
            FullName: user.FullName,
            Email: user.Email,
            Role: user.Role.ToString()));
    }

    /// <inheritdoc />
    public async Task<ServiceResult<string>> RegisterProsumerAsync(
        RegisterProsumerRequestDto request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        string nic = request.Nic.Trim();
        string email = request.Email.Trim();

        // Checked up front so the ordinary case produces a helpful message
        // rather than a raw duplicate key error. The unique indexes remain the
        // real guarantee: two simultaneous requests can both pass this check,
        // which is why the insert below still handles a duplicate key error.
        User? existingByNic = await _userRepository
            .GetByNicAsync(nic, cancellationToken)
            .ConfigureAwait(false);

        if (existingByNic is not null)
        {
            return ServiceResult.Failure<string>(
                ServiceErrorType.Conflict, "An account with this NIC already exists.");
        }

        User? existingByEmail = await _userRepository
            .GetByEmailAsync(email, cancellationToken)
            .ConfigureAwait(false);

        if (existingByEmail is not null)
        {
            return ServiceResult.Failure<string>(
                ServiceErrorType.Conflict, "An account with this email address already exists.");
        }

        User user = new()
        {
            Nic = nic,
            FullName = request.FullName.Trim(),
            Email = email,
            Phone = request.Phone.Trim(),
            Address = request.Address.Trim(),
            SolarCapacityKw = request.SolarCapacityKw,
            PasswordHash = _passwordHasher.Hash(request.Password),

            // Role and Status are assigned by the server and are never taken
            // from the request body. This is what stops a caller registering
            // themselves as a Backoffice officer, and it implements BR-7.
            Role = UserRole.Prosumer,
            Status = AccountStatus.Pending,
        };

        try
        {
            User created = await _userRepository
                .InsertAsync(user, cancellationToken)
                .ConfigureAwait(false);

            LogProsumerRegistered(created.Id ?? string.Empty);
            return ServiceResult.Success(created.Id ?? string.Empty);
        }
        catch (MongoWriteException ex) when (ex.WriteError?.Category == ServerErrorCategory.DuplicateKey)
        {
            // Two registrations for the same NIC arriving at the same moment:
            // the unique index rejects the second. Reported as a conflict so it
            // never surfaces to the user as a 500.
            return ServiceResult.Failure<string>(
                ServiceErrorType.Conflict,
                "An account with this NIC or email address already exists.");
        }
    }

    // -----------------------------------------------------------------------
    // Source generated log methods. See the note in MongoContext.cs.
    // -----------------------------------------------------------------------

    [LoggerMessage(
        EventId = 3001,
        Level = LogLevel.Information,
        Message = "User {UserId} signed in with role {Role}.")]
    private partial void LogSuccessfulLogin(string userId, string role);

    /// <remarks>
    /// The identifier is recorded but the password never is. Failed attempts
    /// are logged at Warning so repeated failures stand out in the log.
    /// </remarks>
    [LoggerMessage(
        EventId = 3002,
        Level = LogLevel.Warning,
        Message = "Failed sign in attempt for identifier {Identifier}.")]
    private partial void LogFailedLogin(string identifier);

    [LoggerMessage(
        EventId = 3003,
        Level = LogLevel.Information,
        Message = "New prosumer {UserId} registered and is pending activation.")]
    private partial void LogProsumerRegistered(string userId);
}
