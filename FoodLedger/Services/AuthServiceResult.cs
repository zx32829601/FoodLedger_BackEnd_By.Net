using FoodLedger.Data.Entities;

namespace FoodLedger.Services;

/// <summary>
/// Auth Service 的成功或預期失敗結果基底型別。
/// </summary>
public abstract class AuthServiceResult
{
    private protected AuthServiceResult()
    {
    }
}

/// <summary>
/// 表示 Identity 已成功建立或驗證使用者。
/// </summary>
public sealed class AuthServiceSuccess : AuthServiceResult
{
    /// <summary>
    /// 建立成功的 Auth Service 結果。
    /// </summary>
    /// <param name="user">已由 Identity 建立或驗證的使用者。</param>
    public AuthServiceSuccess(ApplicationUser user)
    {
        User = user;
    }

    /// <summary>
    /// 已由 Identity 建立或驗證的使用者。
    /// </summary>
    public ApplicationUser User { get; }
}

/// <summary>
/// 表示可安全轉換成 API 回應的預期 Auth 失敗。
/// </summary>
public sealed class AuthServiceFailure : AuthServiceResult
{
    /// <summary>
    /// 建立預期失敗的 Auth Service 結果。
    /// </summary>
    /// <param name="errorCode">穩定錯誤代碼。</param>
    /// <param name="errorMessage">可安全對外顯示的繁體中文 fallback 訊息。</param>
    /// <param name="errorField">可選的 request 欄位名稱。</param>
    public AuthServiceFailure(
        string errorCode,
        string errorMessage,
        string? errorField = null)
    {
        ErrorCode = errorCode;
        ErrorMessage = errorMessage;
        ErrorField = errorField;
    }

    /// <summary>
    /// 穩定錯誤代碼。
    /// </summary>
    public string ErrorCode { get; }

    /// <summary>
    /// 可安全對外顯示的繁體中文 fallback 訊息。
    /// </summary>
    public string ErrorMessage { get; }

    /// <summary>
    /// 錯誤對應的 request 欄位；一般錯誤可為 <see langword="null" />。
    /// </summary>
    public string? ErrorField { get; }
}
