namespace FoodLedger.Infrastructure.Authentication;

/// <summary>
/// 集中定義 FoodLedger Web Cookie 與行動端 Bearer 的驗證 Scheme。
/// </summary>
public static class AuthenticationSchemeNames
{
    /// <summary>
    /// 依 request 是否帶有 Bearer Authorization Header 選擇實際驗證方式的 Scheme。
    /// </summary>
    public const string Combined = "FoodLedger";

    /// <summary>
    /// Flutter Web 使用的 Identity Cookie Scheme。
    /// </summary>
    public const string WebCookie = "FoodLedger.WebCookie";
}
