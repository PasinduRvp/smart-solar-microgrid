/*
 * ---------------------------------------------------------------------------
 * File        : CurrentUser.cs
 * Project     : Smart Solar Microgrid Trading System — SE4040 Assignment 1
 * Author      : <Your Full Name> (<IT Number>)
 * Created     : 2026-09-19
 * Description : Reads the caller's identity from the claims that the JWT
 *               middleware placed on the current request.
 *
 * SOLID       : Single Responsibility — reads claims and nothing more.
 * Security    : Every value here comes from a token whose signature has already
 *               been verified by the authentication middleware. Nothing is read
 *               from a header, query string or request body the caller controls,
 *               so a client cannot claim to be someone else by editing the
 *               request. Tampering with the token itself breaks the signature
 *               and the request never reaches a controller.
 * ---------------------------------------------------------------------------
 */

using System.Security.Claims;
using SolarMicrogrid.Api.Models.Enums;

namespace SolarMicrogrid.Api.Security;

/// <inheritdoc cref="ICurrentUser" />
public sealed class CurrentUser : ICurrentUser
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    /// <summary>Receives the accessor that exposes the current request.</summary>
    public CurrentUser(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor
            ?? throw new ArgumentNullException(nameof(httpContextAccessor));
    }

    /// <summary>The verified claims of the caller, if any.</summary>
    private ClaimsPrincipal? Principal => _httpContextAccessor.HttpContext?.User;

    /// <inheritdoc />
    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated ?? false;

    /// <inheritdoc />
    public string? UserId => Principal?.FindFirstValue(ClaimTypes.NameIdentifier);

    /// <inheritdoc />
    public string? Nic => Principal?.FindFirstValue(JwtTokenService.NicClaimType);

    /// <inheritdoc />
    public UserRole? Role
    {
        get
        {
            string? roleName = Principal?.FindFirstValue(ClaimTypes.Role);

            // A token could in principle carry a role string this version of the
            // application does not recognise. Returning null rather than
            // throwing means such a caller is simply treated as having no role,
            // and is refused by every rule that requires one.
            return Enum.TryParse(roleName, ignoreCase: false, out UserRole parsed)
                ? parsed
                : null;
        }
    }

    /// <inheritdoc />
    public bool IsBackoffice => Role == UserRole.Backoffice;
}
