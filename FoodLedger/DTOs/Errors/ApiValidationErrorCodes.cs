namespace FoodLedger.DTOs.Errors;

/// <summary>
/// Request model validation 與前端共同使用的穩定錯誤代碼。
/// </summary>
public static class ApiValidationErrorCodes
{
    /// <summary>
    /// 驗證失敗回應的最上層錯誤代碼。
    /// </summary>
    public const string ValidationFailed = "Validation.Failed";

    /// <summary>
    /// 使用者帳號格式不符合註冊規則。
    /// </summary>
    public const string UserAccountInvalid = "Auth.UserAccountInvalid";

    /// <summary>
    /// 顯示名稱為空白或長度不符合註冊規則。
    /// </summary>
    public const string DisplayNameInvalid = "Auth.DisplayNameInvalid";

    /// <summary>
    /// Email 為空白或格式無效。
    /// </summary>
    public const string EmailInvalid = "Auth.EmailInvalid";

    /// <summary>
    /// 密碼未符合 Identity 的長度、大小寫英文與數字規則。
    /// </summary>
    public const string PasswordInvalid = "Auth.PasswordInvalid";

    /// <summary>
    /// JSON 或其他 request 欄位無法轉換成預期格式。
    /// </summary>
    public const string InvalidValue = "Validation.InvalidValue";
}
