using FoodLedger.DTOs.Auth;
using FoodLedger.DTOs.Users;

namespace FoodLedger.Services;

/// <summary>
/// 提供 FoodLedger 使用者註冊、登入與目前使用者查詢的應用程式服務。
/// </summary>
public interface IAuthService
{
    /// <summary>
    /// 使用有效的註冊資料建立 Identity 使用者。
    /// </summary>
    /// <param name="request">已通過 API 基本驗證的註冊資料。</param>
    /// <returns>註冊成功後的 Identity 使用者，或可安全轉換成 API 回應的預期錯誤。</returns>
    Task<AuthServiceResult> RegisterAsync(RegisterRequest request);

    /// <summary>
    /// 使用帳號或 Email 與密碼驗證使用者。
    /// </summary>
    /// <param name="request">登入識別資料與密碼。</param>
    /// <returns>登入成功後的 Identity 使用者，或不透露帳號存在狀態的預期錯誤。</returns>
    Task<AuthServiceResult> LoginAsync(LoginRequest request);

    /// <summary>
    /// 依系統識別碼取得目前使用者的公開基本資料。
    /// </summary>
    /// <param name="userId">已通過授權的目前使用者識別碼。</param>
    /// <returns>找到使用者時回傳公開資料，否則回傳 <see langword="null" />。</returns>
    Task<CurrentUserResponse?> GetCurrentUserAsync(long userId);
}
