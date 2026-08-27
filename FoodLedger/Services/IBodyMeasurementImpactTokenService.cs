namespace FoodLedger.Services;

/// <summary>建立並驗證綁定刪除影響內容的短效 opaque token。</summary>
public interface IBodyMeasurementImpactTokenService
{
    /// <summary>建立綁定使用者、測量版本與影響內容的短效 token。</summary>
    BodyMeasurementImpactToken Create(
        long userId,
        long measurementId,
        Guid version,
        int affectedSnapshotCount,
        bool affectsCurrentTarget);

    /// <summary>驗證 token 是否未逾期、未遭竄改且完全符合預期內容。</summary>
    bool IsValid(
        string token,
        long userId,
        long measurementId,
        Guid version,
        int affectedSnapshotCount,
        bool affectsCurrentTarget);
}

/// <summary>包含 opaque token 與其到期時間。</summary>
public sealed record BodyMeasurementImpactToken(string Value, DateTimeOffset ExpiresAt);
