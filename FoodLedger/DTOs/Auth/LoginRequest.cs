using System.ComponentModel.DataAnnotations;

namespace FoodLedger.DTOs.Auth;

/// <summary>
/// 使用帳號或 Email 登入 FoodLedger 的請求資料。
/// </summary>
public sealed class LoginRequest
{
    /// <summary>
    /// 使用者帳號或 Email；包含 <c>@</c> 時會以 Email 查詢。
    /// </summary>
    [Required]
    public string LoginId { get; init; } = string.Empty;

    /// <summary>
    /// 由 ASP.NET Core Identity 驗證的使用者密碼。
    /// </summary>
    [Required]
    public string Password { get; init; } = string.Empty;
}
