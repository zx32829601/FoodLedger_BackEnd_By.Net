using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace FoodLedger.Swagger;

/// <summary>
/// 只替需要授權的 API operation 加上 Swagger Bearer token 驗證需求。
/// </summary>
/// <remarks>
/// Swagger UI 需要 security requirement 才會在單一 API 操作上帶入
/// <c>Authorization</c> header。此 filter 會根據 endpoint metadata 判斷
/// <see cref="AuthorizeAttribute" /> 與 <see cref="AllowAnonymousAttribute" />，
/// 避免登入、註冊等匿名 API 被誤標成必須先帶 token。
/// </remarks>
public sealed class AuthorizeDocumentFilter : IDocumentFilter
{
    /// <summary>
    /// 對需要授權的 API operation 補上 Bearer security requirement。
    /// </summary>
    /// <param name="swaggerDoc">已產生的 OpenAPI 文件。</param>
    /// <param name="context">Swagger 產生文件時提供的 API 描述集合。</param>
    public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
    {
        foreach (var apiDescription in context.ApiDescriptions)
        {
            var endpointMetadata = apiDescription.ActionDescriptor.EndpointMetadata;
            var hasAuthorize = endpointMetadata.OfType<AuthorizeAttribute>().Any();
            var allowsAnonymous = endpointMetadata.OfType<AllowAnonymousAttribute>().Any();

            if (!hasAuthorize || allowsAnonymous)
            {
                continue;
            }

            var path = "/" + apiDescription.RelativePath?.Split('?')[0];

            if (!swaggerDoc.Paths.TryGetValue(path, out var pathItem))
            {
                continue;
            }

            var operationType = GetOperationType(apiDescription.HttpMethod);

            if (operationType is null || pathItem.Operations is null ||
                !pathItem.Operations.TryGetValue(operationType, out var operation))
            {
                continue;
            }

            operation.Security ??= [];
            operation.Security.Add(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecuritySchemeReference("Bearer", swaggerDoc, null),
                    []
                },
            });
        }
    }

    private static HttpMethod? GetOperationType(string? httpMethod)
    {
        return httpMethod?.ToUpperInvariant() switch
        {
            "GET" => HttpMethod.Get,
            "POST" => HttpMethod.Post,
            "PUT" => HttpMethod.Put,
            "PATCH" => HttpMethod.Patch,
            "DELETE" => HttpMethod.Delete,
            "HEAD" => HttpMethod.Head,
            "OPTIONS" => HttpMethod.Options,
            "TRACE" => HttpMethod.Trace,
            _ => null,
        };
    }
}
