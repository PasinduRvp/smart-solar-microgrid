/*
 * ---------------------------------------------------------------------------
 * File        : BackofficeSeeder.cs
 * Project     : Smart Solar Microgrid Trading System — SE4040 Assignment 1
 * Author      : NIMSARA R V P P (IT 23215306)
 * Created     : 2026-09-19
 * Description : Creates the first Backoffice account at startup, but only when
 *               the database holds no Backoffice officer at all.
 *
 * SOLID       : Single Responsibility — bootstrapping the first officer, and
 *               nothing else. It is a separate hosted service from
 *               DatabaseInitializer because index creation and data seeding are
 *               different concerns that change for different reasons.
 * Design note : A hosted service is a singleton, but IUserRepository is
 *               registered as scoped, so it cannot simply be injected here —
 *               doing so would capture a scoped dependency for the lifetime of
 *               the application, which is a well known source of bugs. The
 *               correct approach, used below, is to inject IServiceScopeFactory
 *               and open a scope for the duration of the work.
 * Security    : Runs only when no Backoffice account exists, so it can never
 *               overwrite or reset an existing officer's password. The password
 *               comes from configuration that is excluded from source control,
 *               and a warning is logged reminding the operator to change it.
 * ---------------------------------------------------------------------------
 */

using Microsoft.Extensions.Options;
using SolarMicrogrid.Api.Configuration;
using SolarMicrogrid.Api.Models;
using SolarMicrogrid.Api.Models.Enums;
using SolarMicrogrid.Api.Repositories;
using SolarMicrogrid.Api.Security;

namespace SolarMicrogrid.Api.Data;

/// <summary>
/// Seeds the bootstrap Backoffice account on first run.
/// </summary>
public sealed partial class BackofficeSeeder : IHostedService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly SeedSettings _settings;
    private readonly ILogger<BackofficeSeeder> _logger;

    /// <summary>Receives the scope factory, settings and a logger.</summary>
    public BackofficeSeeder(
        IServiceScopeFactory scopeFactory,
        IOptions<SeedSettings> options,
        ILogger<BackofficeSeeder> logger)
    {
        ArgumentNullException.ThrowIfNull(options);
        _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
        _settings = options.Value;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>Creates the bootstrap account if it is needed.</summary>
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (!_settings.CreateDefaultBackofficeUser)
        {
            return;
        }

        try
        {
            // A scope is opened because IUserRepository is scoped. Resolving it
            // from the root provider instead would keep one instance alive for
            // the whole application.
            using IServiceScope scope = _scopeFactory.CreateScope();

            IUserRepository userRepository = scope.ServiceProvider.GetRequiredService<IUserRepository>();
            IPasswordHasher passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();

            bool backofficeExists = await userRepository
                .ExistsAsync(user => user.Role == UserRole.Backoffice, cancellationToken)
                .ConfigureAwait(false);

            if (backofficeExists)
            {
                // Nothing to do. This is the normal case on every run after the
                // first, and on any deployment with real officer accounts.
                return;
            }

            User backofficeUser = new()
            {
                Nic = _settings.Nic,
                FullName = _settings.FullName,
                Email = _settings.Email,
                Phone = "0000000000",
                Address = "System generated bootstrap account",
                PasswordHash = passwordHasher.Hash(_settings.Password),
                Role = UserRole.Backoffice,
                Status = AccountStatus.Active,
                CreatedBy = null,
            };

            await userRepository.InsertAsync(backofficeUser, cancellationToken).ConfigureAwait(false);
            LogSeededBackofficeUser(_settings.Email);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // As with index creation, a seeding failure must not stop the
            // service from starting: the health endpoint has to stay reachable
            // so the problem can be diagnosed on the deployed machine.
            LogSeedFailed(ex);
        }
    }

    /// <summary>Nothing to release on shutdown.</summary>
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    // -----------------------------------------------------------------------
    // Source generated log methods. See the note in MongoContext.cs.
    // -----------------------------------------------------------------------

    [LoggerMessage(
        EventId = 1201,
        Level = LogLevel.Warning,
        Message = "No Backoffice account existed, so the bootstrap account {Email} was created. " +
                  "Sign in, create a real Backoffice officer, then delete this one.")]
    private partial void LogSeededBackofficeUser(string email);

    [LoggerMessage(
        EventId = 1202,
        Level = LogLevel.Error,
        Message = "Seeding the bootstrap Backoffice account failed. The service will continue to start.")]
    private partial void LogSeedFailed(Exception exception);
}
