using FoodLedger.DTOs.Users;

namespace FoodLedger.DTOs.Auth;

/// <summary>
/// 註冊或登入成功後回傳的認證資料與使用者基本資料。
/// </summary>
public sealed class AuthResponse
{
    /// <summary>
    /// Bearer 模式呼叫受保護 API 時使用的 Token；Cookie 模式不回傳此欄位。
    /// </summary>
    public string? AccessToken { get; init; }

    /// <summary>
    /// Bearer 模式供後續更新流程使用的 Token；Cookie 模式不回傳此欄位。
    /// </summary>
    public string? RefreshToken { get; init; }

    /// <summary>
    /// Bearer Access Token 的有效秒數；Cookie 模式不回傳此欄位。
    /// </summary>
    public long? ExpiresIn { get; init; }

    /// <summary>
    /// 已完成驗證的 FoodLedger 使用者基本資料。
    /// </summary>
    public required CurrentUserResponse User { get; init; }
}
