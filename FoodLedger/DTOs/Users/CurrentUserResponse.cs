namespace FoodLedger.DTOs.Users;

/// <summary>
/// 目前登入使用者的基本資訊回應。
/// </summary>
/// <remarks>
/// 此 DTO 用於讓前端或手動測試確認目前 request 的登入身分是否已被後端正確解析。
/// 回應內容不包含 email、角色或其他個資，避免在尚未有明確需求前擴大資料揭露範圍。
/// </remarks>
public sealed class CurrentUserResponse
{
    /// <summary>
    /// 目前登入使用者的系統識別碼。
    /// </summary>
    public long UserId { get; init; }

    /// <summary>
    /// 目前登入使用者名稱。
    /// </summary>
    public string? UserName { get; init; }

    /// <summary>
    /// 目前 request 是否已有通過驗證的登入使用者。
    /// </summary>
    public bool IsAuthenticated { get; init; }
}
