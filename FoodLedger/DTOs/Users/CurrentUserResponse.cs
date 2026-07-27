namespace FoodLedger.DTOs.Users;

/// <summary>
/// 目前登入使用者的基本資訊回應。
/// </summary>
/// <remarks>
/// 此 DTO 與註冊、登入成功回應內的使用者資料共用相同契約，且不暴露 Identity
/// 的 PasswordHash、SecurityStamp 等內部安全欄位。
/// </remarks>
public sealed class CurrentUserResponse
{
    /// <summary>
    /// 目前登入使用者的系統識別碼。
    /// </summary>
    public long UserId { get; init; }

    /// <summary>
    /// 目前登入使用者的唯一帳號。
    /// </summary>
    public required string UserAccount { get; init; }

    /// <summary>
    /// 目前登入使用者的公開顯示名稱。
    /// </summary>
    public required string DisplayName { get; init; }

    /// <summary>
    /// 目前登入使用者的電子郵件地址。
    /// </summary>
    public required string Email { get; init; }

    /// <summary>
    /// 指出目前使用者是否具備管理員角色。
    /// </summary>
    public bool IsAdmin { get; init; }
}
