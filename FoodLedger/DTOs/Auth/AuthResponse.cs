using FoodLedger.DTOs.Users;

namespace FoodLedger.DTOs.Auth;

/// <summary>
/// 註冊或登入成功後回傳的 Token 與使用者基本資料。
/// </summary>
public sealed class AuthResponse
{
    /// <summary>
    /// 呼叫受保護 API 時使用的 Bearer Token。
    /// </summary>
    public required string AccessToken { get; init; }

    /// <summary>
    /// 保留供後續 Refresh Token 流程使用的 Token。
    /// </summary>
    public required string RefreshToken { get; init; }

    /// <summary>
    /// Access Token 的有效秒數。
    /// </summary>
    public required long ExpiresIn { get; init; }

    /// <summary>
    /// 已完成驗證的 FoodLedger 使用者基本資料。
    /// </summary>
    public required CurrentUserResponse User { get; init; }
}
