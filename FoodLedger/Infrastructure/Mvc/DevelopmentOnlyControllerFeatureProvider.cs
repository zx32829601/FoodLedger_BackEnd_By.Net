using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Controllers;

namespace FoodLedger.Infrastructure.Mvc;

/// <summary>
/// 在非 Development 環境排除標示為 <see cref="DevelopmentOnlyControllerAttribute" /> 的 Controller。
/// </summary>
public sealed class DevelopmentOnlyControllerFeatureProvider : IApplicationFeatureProvider<ControllerFeature>
{
    private readonly bool _isDevelopment;

    /// <summary>
    /// 建立 Development-only Controller 篩選器。
    /// </summary>
    /// <param name="isDevelopment">目前執行環境是否為 Development。</param>
    public DevelopmentOnlyControllerFeatureProvider(bool isDevelopment)
    {
        _isDevelopment = isDevelopment;
    }

    /// <summary>
    /// 依執行環境保留或排除 Development-only Controller。
    /// </summary>
    /// <param name="parts">MVC application parts。</param>
    /// <param name="feature">MVC 已發現的 Controller 清單。</param>
    public void PopulateFeature(IEnumerable<ApplicationPart> parts, ControllerFeature feature)
    {
        if (_isDevelopment)
        {
            return;
        }

        for (var index = feature.Controllers.Count - 1; index >= 0; index--)
        {
            var controllerType = feature.Controllers[index];
            if (controllerType.IsDefined(typeof(DevelopmentOnlyControllerAttribute), inherit: false))
            {
                feature.Controllers.RemoveAt(index);
            }
        }
    }
}
