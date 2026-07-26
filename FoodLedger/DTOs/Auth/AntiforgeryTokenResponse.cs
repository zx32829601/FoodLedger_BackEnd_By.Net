namespace FoodLedger.DTOs.Auth;

/// <summary>
/// 提供 Web Cookie 狀態變更 request 使用的 Antiforgery Token。
/// </summary>
public sealed class AntiforgeryTokenResponse
{
    /// <summary>
    /// Web client 必須放入 <c>X-CSRF-TOKEN</c> Header 的 request token。
    /// </summary>
    public required string RequestToken { get; init; }
}
