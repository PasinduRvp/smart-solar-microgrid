/*
 * ---------------------------------------------------------------------------
 * File        : JwtTokenService.cs
 * Project     : Smart Solar Microgrid Trading System — SE4040 Assignment 1
 * Author      : <Your Full Name> (<IT Number>)
 * Created     : 2026-09-19
 * Description : Builds signed JSON Web Tokens describing an authenticated user.
 *               The same token is used by both the React web application and
 *               the Android application, which is what allows one set of
 *               authorisation rules to protect every client.
 *
 * SOLID       : Single Responsibility — token construction only. It performs no
 *               credential checking; AuthService decides who is allowed a token
 *               and this class only issues it.
 * Security    : The token carries the user's role, which drives every
 *               [Authorize(Roles = ...)] check in the API. It is signed with
 *               HMAC-SHA256, so a client that edits its own role claim breaks
 *               the signature and is rejected. A JWT is signed, not encrypted:
 *               anyone can read its contents, so only identifiers go inside —
 *               never a password hash or anything else confidential.
 * ---------------------------------------------------------------------------
 */

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using SolarMicrogrid.Api.Configuration;
using SolarMicrogrid.Api.Models;

namespace SolarMicrogrid.Api.Security;

/// <inheritdoc cref="ITokenService" />
public sealed class JwtTokenService : ITokenService
{
    /// <summary>Custom claim carrying the prosumer's NIC, used by the mobile client.</summary>
    public const string NicClaimType = "nic";

    private readonly JwtSettings _settings;

    /// <summary>Receives the validated JWT settings from the container.</summary>
    public JwtTokenService(IOptions<JwtSettings> options)
    {
        ArgumentNullException.ThrowIfNull(options);
        _settings = options.Value;
    }

    /// <inheritdoc />
    public (string Token, DateTime ExpiresAtUtc) CreateToken(User user)
    {
        ArgumentNullException.ThrowIfNull(user);

        DateTime expiresAtUtc = DateTime.UtcNow.AddMinutes(_settings.ExpiryMinutes);

        // Only identifiers go into the token. ClaimTypes.Role is used because
        // that is the claim [Authorize(Roles = ...)] reads by default.
        List<Claim> claims =
        [
            new(JwtRegisteredClaimNames.Sub, user.Id ?? string.Empty),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(ClaimTypes.Name, user.FullName),
            new(ClaimTypes.NameIdentifier, user.Id ?? string.Empty),
            new(ClaimTypes.Role, user.Role.ToString()),
            new(NicClaimType, user.Nic),
        ];

        SymmetricSecurityKey signingKey = new(Encoding.UTF8.GetBytes(_settings.SecretKey));
        SigningCredentials credentials = new(signingKey, SecurityAlgorithms.HmacSha256);

        JwtSecurityToken token = new(
            issuer: _settings.Issuer,
            audience: _settings.Audience,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: expiresAtUtc,
            signingCredentials: credentials);

        string encoded = new JwtSecurityTokenHandler().WriteToken(token);
        return (encoded, expiresAtUtc);
    }
}
