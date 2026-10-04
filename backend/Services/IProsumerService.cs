using SolarMicrogrid.Api.Common;
using SolarMicrogrid.Api.Dtos.Users;
using SolarMicrogrid.Api.Models.Enums;

namespace SolarMicrogrid.Api.Services;

public interface IProsumerService
{
    Task<ServiceResult<IReadOnlyList<UserResponseDto>>> GetProsumersAsync(
        AccountStatus? status,
        CancellationToken cancellationToken = default);

    Task<ServiceResult<IReadOnlyList<UserResponseDto>>> GetPendingActivationsAsync(
        CancellationToken cancellationToken = default);

    Task<ServiceResult<UserResponseDto>> GetByNicAsync(
        string nic,
        CancellationToken cancellationToken = default);

    Task<ServiceResult<UserResponseDto>> UpdateProfileAsync(
        string nic,
        UpdateUserRequestDto request,
        CancellationToken cancellationToken = default);

    Task<ServiceResult<UserResponseDto>> ActivateAsync(
        string nic,
        CancellationToken cancellationToken = default);

    Task<ServiceResult<UserResponseDto>> DeactivateAsync(
        string nic,
        CancellationToken cancellationToken = default);

    Task<ServiceResult<UserResponseDto>> RequestDeactivationAsync(
        string nic,
        CancellationToken cancellationToken = default);
}
