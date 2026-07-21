namespace FoodLedger.Infrastructure.Mvc;

/// <summary>
/// 標示 Controller 僅能在 Development 環境註冊，用於本機診斷或開發輔助端點。
/// </summary>
/// <remarks>
/// 套用此屬性的 Controller 不應承載正式業務流程，也不應作為 Production 環境的對外 API。
/// </remarks>
[AttributeUsage(AttributeTargets.Class)]
public sealed class DevelopmentOnlyControllerAttribute : Attribute
{
}
