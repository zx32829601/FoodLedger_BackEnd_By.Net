using Microsoft.AspNetCore.Identity;

namespace FoodLedger.Data.Entities;

/// <summary>代表使用 ASP.NET Core Identity 驗證的 FoodLedger 使用者。</summary>
public class ApplicationUser : IdentityUser<long>
{
    public string DisplayName { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset ModifiedAt { get; set; }
}
