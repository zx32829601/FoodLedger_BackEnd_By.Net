using Microsoft.AspNetCore.Identity;

namespace FoodLedger.Data.Entities;

public class ApplicationUser : IdentityUser<long>
{
    public string DisplayName { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset ModifiedAt { get; set; }
}
