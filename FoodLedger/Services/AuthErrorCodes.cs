namespace FoodLedger.Services;

/// <summary>
/// 自訂 Auth API 與前端共同使用的穩定錯誤代碼。
/// </summary>
public static class AuthErrorCodes
{
    /// <summary>
    /// 註冊帳號已被其他使用者使用。
    /// </summary>
    public const string UserAccountAlreadyExists = "Auth.UserAccountAlreadyExists";

    /// <summary>
    /// 註冊 Email 已被其他使用者使用。
    /// </summary>
    public const string EmailAlreadyExists = "Auth.EmailAlreadyExists";

    /// <summary>
    /// 登入識別資料或密碼無效。
    /// </summary>
    public const string InvalidCredentials = "Auth.InvalidCredentials";
}
