using FoodLedger.Data.Entities;
using FoodLedger.Infrastructure.Authentication;
using FoodLedger.Infrastructure.Mvc;
using FoodLedger.Security;
using FoodLedger.Services;
using FoodLedger.Swagger;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;

const string DevelopmentCorsPolicy = "DevelopmentCorsPolicy";

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString));

builder.Services.AddAuthentication(IdentityConstants.BearerScheme)
    .AddBearerToken(IdentityConstants.BearerScheme);

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
builder.Services.AddExceptionHandler<ApiExceptionHandler>();
builder.Services.AddProblemDetails();

builder.Services.AddApplicationAuthorization();
builder.Services.AddCors(options =>
{
    options.AddPolicy(DevelopmentCorsPolicy, policy =>
    {
        policy
            .SetIsOriginAllowed(origin =>
                Uri.TryCreate(origin, UriKind.Absolute, out var uri)
                && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)
                && (uri.Host == "localhost" || uri.Host == "127.0.0.1"))
            .AllowAnyHeader()
            .AllowAnyMethod();
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

app.MapDefaultEndpoints();
app.UseExceptionHandler();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();   // 啟用 Swagger 中介軟體
    app.UseSwaggerUI(); // 啟用 Swagger UI 網頁介面
}

app.UseHttpsRedirection();

if (app.Environment.IsDevelopment())
{
    app.UseCors(DevelopmentCorsPolicy);
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

/// <summary>
/// 讓 API 整合測試可透過 <c>WebApplicationFactory</c> 載入 top-level statements 建立的應用程式進入點。
/// </summary>
public partial class Program;
