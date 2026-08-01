using FoodLedger.Data.Entities;
using FoodLedger.Infrastructure.Authentication;
using FoodLedger.Infrastructure.Mvc;
using FoodLedger.Security;
using FoodLedger.Services;
using FoodLedger.Swagger;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;

const string FrontendCorsPolicy = "FrontendCorsPolicy";
const string CorsAllowedOriginsConfigurationKey = "Cors:AllowedOrigins";
const string ApplyMigrationsOnStartupConfigurationKey = "Database:ApplyMigrationsOnStartup";
const string InternalTestingEnvironment = "InternalTesting";

var builder = WebApplication.CreateBuilder(args);
var isDevelopmentEnvironment = builder.Environment.IsDevelopment();
var isInternalTestingEnvironment =
    builder.Environment.IsEnvironment(InternalTestingEnvironment);
var cookieSecurePolicy = isInternalTestingEnvironment
    ? CookieSecurePolicy.SameAsRequest
    : CookieSecurePolicy.Always;
var configuredCorsOrigins = builder.Configuration
    .GetSection(CorsAllowedOriginsConfigurationKey)
    .GetChildren()
    .Select(section => section.Value)
    .Where(origin => !string.IsNullOrWhiteSpace(origin))
    .Select(origin => origin!)
    .ToHashSet(StringComparer.OrdinalIgnoreCase);

builder.AddServiceDefaults();

// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString));

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = AuthenticationSchemeNames.Combined;
        options.DefaultChallengeScheme = AuthenticationSchemeNames.Combined;
        options.DefaultSignInScheme = AuthenticationSchemeNames.WebCookie;
    })
    .AddPolicyScheme(
        AuthenticationSchemeNames.Combined,
        displayName: null,
        options =>
        {
            options.ForwardDefaultSelector = context =>
            {
                var authorizationHeader = context.Request.Headers.Authorization.ToString();
                return authorizationHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
                    ? IdentityConstants.BearerScheme
                    : AuthenticationSchemeNames.WebCookie;
            };
        })
    .AddBearerToken(IdentityConstants.BearerScheme)
    .AddCookie(
        AuthenticationSchemeNames.WebCookie,
        options =>
        {
            options.Cookie.Name = isInternalTestingEnvironment
                ? "FoodLedger.Auth"
                : "__Host-FoodLedger.Auth";
            options.Cookie.HttpOnly = true;
            options.Cookie.SecurePolicy = cookieSecurePolicy;
            options.Cookie.SameSite = SameSiteMode.Lax;
            options.Cookie.Path = "/";
            options.SlidingExpiration = true;
            options.Events.OnRedirectToLogin = context =>
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return Task.CompletedTask;
            };
            options.Events.OnRedirectToAccessDenied = context =>
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                return Task.CompletedTask;
            };
        });

builder.Services
    .AddIdentityCore<ApplicationUser>(options =>
    {
        options.User.RequireUniqueEmail = true;
        options.Password.RequiredLength = 8;
        options.Password.RequireDigit = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireNonAlphanumeric = false;
        options.Stores.MaxLengthForKeys = 128;
    })
    .AddRoles<IdentityRole<long>>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddApiEndpoints();

builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddScoped<IdentityBearerTokenResponseFactory>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IDailyRecordService, DailyRecordService>();
builder.Services.AddScoped<IBodyProfileService, BodyProfileService>();
builder.Services.AddScoped<IDefinedCodeService, DefinedCodeService>();
builder.Services.AddScoped<IFoodSearchService, FoodSearchService>();
builder.Services.AddScoped<IFoodMaintenanceService, FoodMaintenanceService>();
builder.Services.AddScoped<INutritionSummaryService, NutritionSummaryService>();
builder.Services.AddScoped<INutrientCatalogService, NutrientCatalogService>();
builder.Services.AddExceptionHandler<ApiExceptionHandler>();
builder.Services.AddProblemDetails();
builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-CSRF-TOKEN";
    options.Cookie.Name = isInternalTestingEnvironment
        ? "FoodLedger.Antiforgery"
        : "__Host-FoodLedger.Antiforgery";
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = cookieSecurePolicy;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.Path = "/";
});

builder.Services.AddApplicationAuthorization();
builder.Services.AddScoped<CookieAntiforgeryFilter>();
builder.Services.AddCors(options =>
{
    options.AddPolicy(FrontendCorsPolicy, policy =>
    {
        policy
            .SetIsOriginAllowed(origin =>
                Uri.TryCreate(origin, UriKind.Absolute, out var uri)
                && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)
                && ((isDevelopmentEnvironment
                        && (uri.Host == "localhost" || uri.Host == "127.0.0.1"))
                    || configuredCorsOrigins.Contains(origin)))
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

builder.Services.AddControllers()
    .ConfigureApiBehaviorOptions(options =>
    {
        options.InvalidModelStateResponseFactory = ApiValidationProblemFactory.Create;
    })
    .ConfigureApplicationPartManager(partManager =>
    {
        partManager.FeatureProviders.Add(
            new DevelopmentOnlyControllerFeatureProvider(builder.Environment.IsDevelopment()));
    });
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer(); // 讓 Swagger 找到你的 API
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "Opaque",
        In = ParameterLocation.Header,
        Description = "請輸入登入後取得的 Bearer token，不需要加上 Bearer 前綴。",
    });

    options.DocumentFilter<AuthorizeDocumentFilter>();
});           // 生成 Swagger 規格
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
var app = builder.Build();

if (isInternalTestingEnvironment)
{
    app.Logger.LogWarning(
        "目前使用 InternalTesting：驗證 Cookie 可透過 HTTP 傳送，僅限隔離的內網測試環境。");
}

if (app.Configuration.GetValue<bool>(ApplyMigrationsOnStartupConfigurationKey))
{
    await using var migrationScope = app.Services.CreateAsyncScope();
    var dbContext = migrationScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await dbContext.Database.MigrateAsync();
}

app.MapDefaultEndpoints();
app.UseExceptionHandler();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();   // 啟用 Swagger 中介軟體
    app.UseSwaggerUI(); // 啟用 Swagger UI 網頁介面
}

if (!isInternalTestingEnvironment)
{
    app.UseHttpsRedirection();
}

app.UseCors(FrontendCorsPolicy);

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

/// <summary>
/// 讓 API 整合測試可透過 <c>WebApplicationFactory</c> 載入 top-level statements 建立的應用程式進入點。
/// </summary>
public partial class Program;
