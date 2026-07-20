namespace FoodLedger.Services;

/// <summary>
/// 提供目前請求中的登入使用者資訊，讓 Service 層不需要直接依賴 HTTP 環境。
/// </summary>
/// <remarks>
/// 此介面只暴露業務邏輯需要的目前使用者資訊，避免 Service 層直接使用
/// <c>HttpContext</c>、JWT、Cookie 或其他驗證實作細節。
/// </remarks>
public interface ICurrentUserService
{
    /// <summary>
    /// 取得目前請求是否已有通過驗證的登入使用者。
    /// </summary>
    bool IsAuthenticated { get; }

    /// <summary>
    /// 取得目前登入使用者的系統識別碼。
    /// </summary>
    /// <remarks>
    /// 使用者識別碼來自 ASP.NET Core Identity 產生的
    /// <c>ClaimTypes.NameIdentifier</c>。若目前未登入、缺少 claim，或 claim 無法解析為
    /// <see cref="long" />，則回傳 <c>null</c>，由呼叫端決定是否拒絕操作。
    /// </remarks>
    long? UserId { get; }

    /// <summary>
    /// 取得目前登入使用者名稱。
    /// </summary>
    /// <remarks>
    /// 此值可用於記錄或顯示輔助資訊，不應作為資料授權依據；資料授權應以
    /// <see cref="UserId" /> 為準。
    /// </remarks>
    string? UserName { get; }
}
