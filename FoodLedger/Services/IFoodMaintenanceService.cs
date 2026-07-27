using FoodLedger.DTOs.Foods;

namespace FoodLedger.Services;

/// <summary>
/// 定義管理員建立、讀取、修改與刪除食物的操作。
/// </summary>
public interface IFoodMaintenanceService
{
    Task<AdminFoodResponse> GetAsync(long foodId, CancellationToken cancellationToken = default);
    Task<AdminFoodResponse> CreateAsync(
        UpsertFoodRequest request,
        CancellationToken cancellationToken = default);
    Task<AdminFoodResponse> UpdateAsync(
        long foodId,
        UpsertFoodRequest request,
        CancellationToken cancellationToken = default);
    Task DeleteAsync(long foodId, CancellationToken cancellationToken = default);
}
