using FoodLedger.DTOs.BodyProfiles;

namespace FoodLedger.Services;

/// <summary>
/// 管理目前登入使用者唯一的身體資料。
/// </summary>
public interface IBodyProfileService
{
    Task<BodyProfileResponse> GetAsync(CancellationToken cancellationToken = default);

    Task<BodyProfileResponse> UpsertAsync(
        UpsertBodyProfileRequest request,
        CancellationToken cancellationToken = default);
}
