using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SolarMicrogrid.Api.Common;
using SolarMicrogrid.Api.Dtos.Users;
using SolarMicrogrid.Api.Models.Enums;
using SolarMicrogrid.Api.Services;

namespace SolarMicrogrid.Api.Controllers;

[Route("api/[controller]")]
[Authorize]
public sealed class ProsumersController : ApiControllerBase
{
    private readonly IProsumerService _prosumerService;

    public ProsumersController(IProsumerService prosumerService)
    {
        _prosumerService = prosumerService ?? throw new ArgumentNullException(nameof(prosumerService));
    }

    [HttpGet]
    [Authorize(Roles = Roles.BackofficeOrGridOperator)]
    public async Task<ActionResult<IReadOnlyList<UserResponseDto>>> GetAllAsync(
        [FromQuery] AccountStatus? status,
        CancellationToken cancellationToken)
    {
        ServiceResult<IReadOnlyList<UserResponseDto>> result =
            await _prosumerService.GetProsumersAsync(status, cancellationToken).ConfigureAwait(false);
        return ToActionResult(result);
    }

    [HttpGet("pending")]
    [Authorize(Roles = Roles.Backoffice)]
    public async Task<ActionResult<IReadOnlyList<UserResponseDto>>> GetPendingAsync(
        CancellationToken cancellationToken)
    {
        ServiceResult<IReadOnlyList<UserResponseDto>> result =
            await _prosumerService.GetPendingActivationsAsync(cancellationToken).ConfigureAwait(false);
        return ToActionResult(result);
    }

    [HttpGet("{nic}")]
    public async Task<ActionResult<UserResponseDto>> GetByNicAsync(
        string nic,
        CancellationToken cancellationToken)
    {
        ServiceResult<UserResponseDto> result =
            await _prosumerService.GetByNicAsync(nic, cancellationToken).ConfigureAwait(false);
        return ToActionResult(result);
    }

    [HttpPut("{nic}")]
    public async Task<ActionResult<UserResponseDto>> UpdateAsync(
        string nic,
        [FromBody] UpdateUserRequestDto request,
        CancellationToken cancellationToken)
    {
        ServiceResult<UserResponseDto> result =
            await _prosumerService.UpdateProfileAsync(nic, request, cancellationToken).ConfigureAwait(false);
        return ToActionResult(result);
    }

    [HttpPatch("{nic}/activate")]
    [Authorize(Roles = Roles.Backoffice)]
    public async Task<ActionResult<UserResponseDto>> ActivateAsync(
        string nic,
        CancellationToken cancellationToken)
    {
        ServiceResult<UserResponseDto> result =
            await _prosumerService.ActivateAsync(nic, cancellationToken).ConfigureAwait(false);
        return ToActionResult(result);
    }

    [HttpPatch("{nic}/deactivate")]
    [Authorize(Roles = Roles.BackofficeOrGridOperator)]
    public async Task<ActionResult<UserResponseDto>> DeactivateAsync(
        string nic,
        CancellationToken cancellationToken)
    {
        ServiceResult<UserResponseDto> result =
            await _prosumerService.DeactivateAsync(nic, cancellationToken).ConfigureAwait(false);
        return ToActionResult(result);
    }

    [HttpPatch("{nic}/request-deactivation")]
    [Authorize(Roles = Roles.Prosumer)]
    public async Task<ActionResult<UserResponseDto>> RequestDeactivationAsync(
        string nic,
        CancellationToken cancellationToken)
    {
        ServiceResult<UserResponseDto> result =
            await _prosumerService.RequestDeactivationAsync(nic, cancellationToken).ConfigureAwait(false);
        return ToActionResult(result);
    }
}
