using FoodLedger.DTOs.BodyMeasurements;

namespace FoodLedger.Services;

/// <summary>管理目前登入使用者的身體測量歷史與安全刪除流程。</summary>
public interface IBodyMeasurementService
{
    /// <summary>取得目前使用者的身體測量歷史。</summary>
    Task<BodyMeasurementPageResponse> GetHistoryAsync(
        BodyMeasurementQueryRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>以伺服器目前時間建立一筆身體測量。</summary>
    Task<BodyMeasurementResponse> CreateAsync(
        CreateBodyMeasurementRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>以樂觀並行版本修正既有測量值，但保留原測量時間。</summary>
    Task<BodyMeasurementResponse> UpdateAsync(
        long measurementId,
        UpdateBodyMeasurementRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>計算刪除影響並簽發綁定目前資料版本的短效 token。</summary>
    Task<BodyMeasurementDeletionImpactResponse> GetDeletionImpactAsync(
        long measurementId,
        CancellationToken cancellationToken = default);

    /// <summary>驗證影響 token 與版本後永久刪除指定測量。</summary>
    Task DeleteAsync(
        long measurementId,
        DeleteBodyMeasurementRequest request,
        CancellationToken cancellationToken = default);
}
