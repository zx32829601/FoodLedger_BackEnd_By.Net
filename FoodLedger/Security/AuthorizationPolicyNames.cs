namespace FoodLedger.Security;

/// <summary>
/// 定義系統內可重複套用的授權 policy 名稱。
/// </summary>
public static class AuthorizationPolicyNames
{
    /// <summary>
    /// 僅允許 Admin 角色使用的授權 policy。
    /// </summary>
    public const string AdminOnly = "AdminOnly";
}
