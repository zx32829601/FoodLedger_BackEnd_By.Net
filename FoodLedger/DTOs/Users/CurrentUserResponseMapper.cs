using FoodLedger.Data.Entities;

namespace FoodLedger.DTOs.Users;

/// <summary>
/// 將 Identity 使用者集中轉換為可安全公開的使用者 DTO。
/// </summary>
internal static class CurrentUserResponseMapper
{
    /// <summary>
    /// 建立不含 Identity 安全欄位的使用者回應。
    /// </summary>
    /// <param name="user">已由 Identity 建立或驗證的使用者。</param>
    /// <returns>可回傳給 API caller 的使用者基本資料。</returns>
    public static CurrentUserResponse Map(ApplicationUser user, bool isAdmin = false)
    {
        return new CurrentUserResponse
        {
            UserId = user.Id,
            UserAccount = user.UserName ?? string.Empty,
            DisplayName = user.DisplayName,
            Email = user.Email ?? string.Empty,
            IsAdmin = isAdmin,
        };
    }
}
