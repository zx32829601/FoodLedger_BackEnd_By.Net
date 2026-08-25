using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FoodLedger.Data.Entities;
using FoodLedger.DTOs.Auth;
using FoodLedger.DTOs.Users;
using FoodLedger.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace FoodLedger.Tests.Controllers;

/// <summary>
/// 驗證自訂 Auth API 經過 ASP.NET Core Identity 與 HTTP middleware 後的公開行為。
/// </summary>
[Category("Authentication")]
[Category("Integration")]
public class AuthApiTests
{
    // 以下路徑與密碼皆為 Auth API 整合測試使用的固定值，不代表正式環境憑證。
    private const string RegisterPath = "/api/auth/register";
    private const string LoginPath = "/api/auth/login";
    private const string CookieLoginPath = "/api/auth/login?useCookies=true";
    private const string LogoutPath = "/api/auth/logout";
    private const string AntiforgeryPath = "/api/auth/antiforgery";
    private const string AntiforgeryHeaderName = "X-CSRF-TOKEN";
    private const string CurrentUserPath = "/api/users/me";
    private const string ValidPassword = "Password1";
    private const string InvalidPassword = "WrongPassword1";
    private const string BuiltInRegisterPath = "/register";
    private const string BuiltInLoginPath = "/login";
    private const string AllowedCorsOrigin = "http://192.168.10.50:8180";
    private const string DeniedCorsOrigin = "http://192.168.10.51:8180";
    private const string LoopbackCorsOrigin = "http://localhost:8180";
    private const string DevelopmentEnvironment = "Development";
    private const string InternalTestingEnvironment = "InternalTesting";
    private const string ProductionEnvironment = "Production";
    private const string TestingEnvironment = "Testing";

    /// <summary>
    /// 驗證有效註冊資料會建立使用者、回傳 Token，且該 Token 可取得相同的目前使用者資料。
    /// </summary>
    [Test]
    public async Task Register_WhenRequestIsValid_ReturnsTokenAndCurrentUser()
    {
        // 準備
        await using var factory = new AuthApiFactory();
        using var client = factory.CreateClient();
        var request = new
        {
            UserAccount = "food_user",
            DisplayName = " Food 使用者 ",
            Email = "user@example.com",
            Password = ValidPassword,
        };

        // 執行
        var registerResponse = await client.PostAsJsonAsync(RegisterPath, request);

        // 驗證
        Assert.That(registerResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        using var registerJson = JsonDocument.Parse(
            await registerResponse.Content.ReadAsStringAsync());
        var registerRoot = registerJson.RootElement;
        var accessToken = registerRoot.GetProperty("accessToken").GetString();
        var refreshToken = registerRoot.GetProperty("refreshToken").GetString();
        var expiresIn = registerRoot.GetProperty("expiresIn").GetInt64();
        var registeredUser = registerRoot.GetProperty("user");

        Assert.Multiple(() =>
        {
            Assert.That(accessToken, Is.Not.Null.And.Not.Empty);
            Assert.That(refreshToken, Is.Not.Null.And.Not.Empty);
            Assert.That(expiresIn, Is.GreaterThan(0));
            Assert.That(registeredUser.GetProperty("userAccount").GetString(), Is.EqualTo("food_user"));
            Assert.That(registeredUser.GetProperty("displayName").GetString(), Is.EqualTo("Food 使用者"));
            Assert.That(registeredUser.GetProperty("email").GetString(), Is.EqualTo("user@example.com"));
        });

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", accessToken);
        var currentUserResponse = await client.GetAsync(CurrentUserPath);
        using var currentUserJson = JsonDocument.Parse(
            await currentUserResponse.Content.ReadAsStringAsync());
        var currentUser = currentUserJson.RootElement;

        Assert.Multiple(() =>
        {
            Assert.That(currentUserResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(
                currentUser.GetProperty("userId").GetInt64(),
                Is.EqualTo(registeredUser.GetProperty("userId").GetInt64()));
            Assert.That(currentUser.GetProperty("userAccount").GetString(), Is.EqualTo("food_user"));
            Assert.That(currentUser.GetProperty("displayName").GetString(), Is.EqualTo("Food 使用者"));
            Assert.That(currentUser.GetProperty("email").GetString(), Is.EqualTo("user@example.com"));
        });
    }

    /// <summary>
    /// 驗證顯示名稱會先移除前後空白，再套用長度限制與儲存。
    /// </summary>
    [Test]
    public async Task Register_WhenTrimmedDisplayNameIsWithinLimit_ReturnsTrimmedDisplayName()
    {
        // 準備
        await using var factory = new AuthApiFactory();
        using var client = factory.CreateClient();
        var displayName = $"  {new string('名', 30)}  ";

        // 執行
        var response = await client.PostAsJsonAsync(
            RegisterPath,
            new
            {
                UserAccount = "trimmed_user",
                DisplayName = displayName,
                Email = "trimmed@example.com",
                Password = ValidPassword,
            });

        // 驗證
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        using var responseJson = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.That(
            responseJson.RootElement.GetProperty("user").GetProperty("displayName").GetString(),
            Is.EqualTo(new string('名', 30)));
    }

    /// <summary>
    /// 驗證註冊帳號不分大小寫重複時，API 會回傳可定位至帳號欄位的穩定錯誤碼。
    /// </summary>
    [Test]
    public async Task Register_WhenUserAccountAlreadyExists_ReturnsUserAccountError()
    {
        // 準備
        await using var factory = new AuthApiFactory();
        using var client = factory.CreateClient();
        await client.PostAsJsonAsync(
            RegisterPath,
            new
            {
                UserAccount = "food_user",
                DisplayName = "第一位使用者",
                Email = "first@example.com",
                Password = ValidPassword,
            });

        // 執行
        var response = await client.PostAsJsonAsync(
            RegisterPath,
            new
            {
                UserAccount = "FOOD_USER",
                DisplayName = "第二位使用者",
                Email = "second@example.com",
                Password = ValidPassword,
            });

        // 驗證
        using var responseJson = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = responseJson.RootElement;
        var fieldError = root
            .GetProperty("errors")
            .GetProperty("userAccount")[0];

        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
            Assert.That(root.GetProperty("code").GetString(), Is.EqualTo("Auth.UserAccountAlreadyExists"));
            Assert.That(root.GetProperty("traceId").GetString(), Is.Not.Null.And.Not.Empty);
            Assert.That(fieldError.GetProperty("code").GetString(), Is.EqualTo("Auth.UserAccountAlreadyExists"));
        });
    }

    /// <summary>
    /// 驗證註冊 Email 不分大小寫重複時，API 會回傳可定位至 Email 欄位的穩定錯誤碼。
    /// </summary>
    [Test]
    public async Task Register_WhenEmailAlreadyExists_ReturnsEmailError()
    {
        // 準備
        await using var factory = new AuthApiFactory();
        using var client = factory.CreateClient();
        await client.PostAsJsonAsync(
            RegisterPath,
            new
            {
                UserAccount = "first_user",
                DisplayName = "第一位使用者",
                Email = "member@example.com",
                Password = ValidPassword,
            });

        // 執行
        var response = await client.PostAsJsonAsync(
            RegisterPath,
            new
            {
                UserAccount = "second_user",
                DisplayName = "第二位使用者",
                Email = "MEMBER@example.com",
                Password = ValidPassword,
            });

        // 驗證
        using var responseJson = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = responseJson.RootElement;
        var fieldError = root
            .GetProperty("errors")
            .GetProperty("email")[0];

        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
            Assert.That(root.GetProperty("code").GetString(), Is.EqualTo("Auth.EmailAlreadyExists"));
            Assert.That(fieldError.GetProperty("code").GetString(), Is.EqualTo("Auth.EmailAlreadyExists"));
        });
    }

    /// <summary>
    /// 驗證使用者可用帳號或 Email 與相同密碼登入，並取得固定 Auth response。
    /// </summary>
    /// <param name="loginId">測試用帳號或 Email 登入識別資料。</param>
    [TestCase("food_user")]
    [TestCase("user@example.com")]
    public async Task Login_WhenCredentialsAreValid_ReturnsTokenAndUser(string loginId)
    {
        // 準備
        await using var factory = new AuthApiFactory();
        using var client = factory.CreateClient();
        await client.PostAsJsonAsync(
            RegisterPath,
            new
            {
                UserAccount = "food_user",
                DisplayName = "Food 使用者",
                Email = "user@example.com",
                Password = ValidPassword,
            });

        // 執行
        var response = await client.PostAsJsonAsync(
            LoginPath,
            new
            {
                LoginId = loginId,
                Password = ValidPassword,
            });

        // 驗證
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        using var responseJson = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = responseJson.RootElement;
        Assert.Multiple(() =>
        {
            Assert.That(root.GetProperty("accessToken").GetString(), Is.Not.Null.And.Not.Empty);
            Assert.That(root.GetProperty("refreshToken").GetString(), Is.Not.Null.And.Not.Empty);
            Assert.That(
                root.GetProperty("user").GetProperty("userAccount").GetString(),
                Is.EqualTo("food_user"));
        });
    }

    /// <summary>
    /// 驗證註冊欄位不符合規則時，API 會回傳 lower camel case 欄位與 code-first 驗證錯誤。
    /// </summary>
    [Test]
    public async Task Register_WhenFieldsAreInvalid_ReturnsValidationErrors()
    {
        // 準備
        await using var factory = new AuthApiFactory();
        using var client = factory.CreateClient();
        var request = new
        {
            UserAccount = "bad account",
            DisplayName = "   ",
            Email = "invalid-email",
            Password = "password",
        };

        // 執行
        var response = await client.PostAsJsonAsync(RegisterPath, request);

        // 驗證
        using var responseJson = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = responseJson.RootElement;
        var errors = root.GetProperty("errors");

        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
            Assert.That(root.GetProperty("code").GetString(), Is.EqualTo("Validation.Failed"));
            Assert.That(root.GetProperty("traceId").GetString(), Is.Not.Null.And.Not.Empty);
            Assert.That(
                errors.GetProperty("userAccount")[0].GetProperty("code").GetString(),
                Is.EqualTo("Auth.UserAccountInvalid"));
            Assert.That(
                errors.GetProperty("displayName")[0].GetProperty("code").GetString(),
                Is.EqualTo("Auth.DisplayNameInvalid"));
            Assert.That(
                errors.GetProperty("email")[0].GetProperty("code").GetString(),
                Is.EqualTo("Auth.EmailInvalid"));
            Assert.That(
                errors.GetProperty("password")[0].GetProperty("code").GetString(),
                Is.EqualTo("Auth.PasswordInvalid"));
        });
    }

    /// <summary>
    /// 驗證帳號不存在與密碼錯誤皆回傳相同的未授權錯誤，避免洩漏帳號存在狀態。
    /// </summary>
    /// <param name="loginId">測試用登入識別資料。</param>
    /// <param name="password">測試用密碼。</param>
    [TestCase("missing_user", ValidPassword)]
    [TestCase("food_user", InvalidPassword)]
    public async Task Login_WhenCredentialsAreInvalid_ReturnsUniformUnauthorizedError(
        string loginId,
        string password)
    {
        // 準備
        await using var factory = new AuthApiFactory();
        using var client = factory.CreateClient();
        await client.PostAsJsonAsync(
            RegisterPath,
            new
            {
                UserAccount = "food_user",
                DisplayName = "Food 使用者",
                Email = "user@example.com",
                Password = ValidPassword,
            });

        // 執行
        var response = await client.PostAsJsonAsync(
            LoginPath,
            new
            {
                LoginId = loginId,
                Password = password,
            });

        // 驗證
        using var responseJson = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = responseJson.RootElement;
        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
            Assert.That(root.GetProperty("code").GetString(), Is.EqualTo("Auth.InvalidCredentials"));
            Assert.That(root.GetProperty("traceId").GetString(), Is.Not.Null.And.Not.Empty);
            Assert.That(root.GetProperty("message").GetString(), Does.Not.Contain("不存在"));
        });
    }

    /// <summary>
    /// 驗證 Auth Service 發生非預期例外時，API 只回傳安全錯誤代碼與 traceId。
    /// </summary>
    [Test]
    public async Task Register_WhenUnexpectedExceptionOccurs_ReturnsSafeSystemError()
    {
        // 準備
        await using var factory = new AuthApiFactory(new ThrowingAuthService());
        using var client = factory.CreateClient();

        // 執行
        var response = await client.PostAsJsonAsync(
            RegisterPath,
            new
            {
                UserAccount = "food_user",
                DisplayName = "Food 使用者",
                Email = "user@example.com",
                Password = ValidPassword,
            });

        // 驗證
        var responseBody = await response.Content.ReadAsStringAsync();
        using var responseJson = JsonDocument.Parse(responseBody);
        var root = responseJson.RootElement;
        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.InternalServerError));
            Assert.That(root.GetProperty("code").GetString(), Is.EqualTo("System.UnexpectedError"));
            Assert.That(root.GetProperty("traceId").GetString(), Is.Not.Null.And.Not.Empty);
            Assert.That(responseBody, Does.Not.Contain("Sensitive database detail"));
            Assert.That(responseBody, Does.Not.Contain(nameof(InvalidOperationException)));
        });
    }

    /// <summary>
    /// 驗證 Identity 內建註冊與登入端點未公開，caller 只能使用 FoodLedger 自訂 Auth 契約。
    /// </summary>
    /// <param name="path">不應公開的 Identity 內建端點。</param>
    [TestCase(BuiltInRegisterPath)]
    [TestCase(BuiltInLoginPath)]
    public async Task BuiltInIdentityEndpoint_WhenCalled_ReturnsNotFound(string path)
    {
        // 準備
        await using var factory = new AuthApiFactory();
        using var client = factory.CreateClient();

        // 執行
        var response = await client.PostAsJsonAsync(path, new { });

        // 驗證
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    /// <summary>
    /// 驗證 Development 環境已設定的前端來源可通過註冊 API 的 CORS 預檢。
    /// </summary>
    [Test]
    public async Task RegisterPreflight_WhenOriginIsConfigured_ReturnsAllowedOrigin()
    {
        // 準備
        await using var factory = new AuthApiFactory(
            environment: DevelopmentEnvironment,
            allowedCorsOrigin: AllowedCorsOrigin);
        using var client = factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Options, RegisterPath);
        request.Headers.Add("Origin", AllowedCorsOrigin);
        request.Headers.Add("Access-Control-Request-Method", HttpMethod.Post.Method);
        request.Headers.Add("Access-Control-Request-Headers", "content-type");

        // 執行
        var response = await client.SendAsync(request);

        // 驗證
        var containsAllowedOrigin = response.Headers.TryGetValues(
            "Access-Control-Allow-Origin",
            out var allowedOrigins);

        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
            Assert.That(containsAllowedOrigin, Is.True);
            Assert.That(allowedOrigins ?? [], Does.Contain(AllowedCorsOrigin));
        });
    }

    /// <summary>
    /// 驗證 Web 登入選擇 Cookie 模式後，後續 request 不需 Bearer Token 即可取得目前使用者。
    /// </summary>
    [Test]
    public async Task Login_WhenCookieModeIsRequested_AuthenticatesSubsequentRequestWithCookie()
    {
        // 準備
        await using var factory = new AuthApiFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"),
            HandleCookies = true,
        });
        await client.PostAsJsonAsync(
            RegisterPath,
            new
            {
                UserAccount = "cookie_user",
                DisplayName = "Cookie 使用者",
                Email = "cookie@example.com",
                Password = ValidPassword,
            });
        await AddAntiforgeryTokenAsync(client);

        // 執行
        var loginResponse = await client.PostAsJsonAsync(
            CookieLoginPath,
            new
            {
                LoginId = "cookie_user",
                Password = ValidPassword,
            });
        var currentUserResponse = await client.GetAsync(CurrentUserPath);

        // 驗證
        var currentUser = await currentUserResponse.Content
            .ReadFromJsonAsync<CurrentUserResponse>();
        Assert.Multiple(() =>
        {
            Assert.That(loginResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(
                loginResponse.Headers.TryGetValues("Set-Cookie", out var cookies),
                Is.True);
            Assert.That(
                (cookies ?? []).Select(cookie => cookie.ToLowerInvariant()),
                Has.Some.Contains("httponly").And.Contains("secure").And.Contains("samesite=lax"));
            Assert.That(currentUserResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(currentUser?.UserAccount, Is.EqualTo("cookie_user"));
        });
    }

    /// <summary>
    /// 驗證 Web 使用者登出後，原本的 Identity Cookie 不再通過授權。
    /// </summary>
    [Test]
    public async Task Logout_WhenCookieUserIsAuthenticated_InvalidatesCookieSession()
    {
        // 準備
        await using var factory = new AuthApiFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"),
            HandleCookies = true,
        });
        await client.PostAsJsonAsync(
            RegisterPath,
            new
            {
                UserAccount = "logout_user",
                DisplayName = "登出使用者",
                Email = "logout@example.com",
                Password = ValidPassword,
            });
        await AddAntiforgeryTokenAsync(client);
        await client.PostAsJsonAsync(
            CookieLoginPath,
            new
            {
                LoginId = "logout_user",
                Password = ValidPassword,
            });
        await AddAntiforgeryTokenAsync(client);

        // 執行
        var logoutResponse = await client.PostAsync(LogoutPath, content: null);
        var currentUserResponse = await client.GetAsync(CurrentUserPath);

        // 驗證
        Assert.Multiple(() =>
        {
            Assert.That(logoutResponse.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
            Assert.That(currentUserResponse.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
        });
    }

    /// <summary>
    /// 驗證 Cookie 模式登入缺少 Antiforgery Token 時，後端拒絕建立登入 Session。
    /// </summary>
    [Test]
    public async Task Login_WhenCookieModeHasNoAntiforgeryToken_ReturnsBadRequest()
    {
        // 準備
        await using var factory = new AuthApiFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"),
            HandleCookies = true,
        });
        await client.PostAsJsonAsync(
            RegisterPath,
            new
            {
                UserAccount = "csrf_user",
                DisplayName = "CSRF 使用者",
                Email = "csrf@example.com",
                Password = ValidPassword,
            });

        // 執行
        var response = await client.PostAsJsonAsync(
            CookieLoginPath,
            new
            {
                LoginId = "csrf_user",
                Password = ValidPassword,
            });

        // 驗證
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    /// <summary>
    /// 驗證 InternalTesting 環境可透過 HTTP 取得 Cookie 登入所需的 Antiforgery Token。
    /// </summary>
    [Test]
    public async Task Antiforgery_WhenInternalTestingUsesHttp_ReturnsToken()
    {
        // 準備
        await using var factory = new AuthApiFactory(environment: InternalTestingEnvironment);
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("http://localhost"),
            HandleCookies = true,
        });

        // 執行
        var response = await client.GetAsync(AntiforgeryPath);

        // 驗證
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        using var responseJson = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.That(
            responseJson.RootElement.GetProperty("requestToken").GetString(),
            Is.Not.Null.And.Not.Empty);
    }

    /// <summary>
    /// 驗證 InternalTesting 使用 HTTP Cookie 登入後，後續 request 仍可取得目前使用者。
    /// </summary>
    [Test]
    public async Task Login_WhenInternalTestingUsesHttpCookie_AuthenticatesSubsequentRequest()
    {
        // 準備
        await using var factory = new AuthApiFactory(environment: InternalTestingEnvironment);
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("http://localhost"),
            HandleCookies = true,
        });
        await client.PostAsJsonAsync(
            RegisterPath,
            new
            {
                UserAccount = "internal_user",
                DisplayName = "內部測試使用者",
                Email = "internal@example.com",
                Password = ValidPassword,
            });
        await AddAntiforgeryTokenAsync(client);

        // 執行
        var loginResponse = await client.PostAsJsonAsync(
            CookieLoginPath,
            new
            {
                LoginId = "internal_user",
                Password = ValidPassword,
            });
        var currentUserResponse = await client.GetAsync(CurrentUserPath);

        // 驗證
        var currentUser = await currentUserResponse.Content
            .ReadFromJsonAsync<CurrentUserResponse>();
        Assert.Multiple(() =>
        {
            Assert.That(loginResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(currentUserResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(currentUser?.UserAccount, Is.EqualTo("internal_user"));
        });
    }

    /// <summary>
    /// 驗證 InternalTesting 的 HTTP Cookie 不包含 Secure，也不使用要求 HTTPS 的 __Host- 前綴。
    /// </summary>
    [Test]
    public async Task Login_WhenInternalTestingUsesHttp_SetsHttpCompatibleCookies()
    {
        // 準備
        await using var factory = new AuthApiFactory(environment: InternalTestingEnvironment);
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("http://localhost"),
            HandleCookies = true,
        });

        // 執行
        var (antiforgeryCookies, loginCookies) = await RegisterAndLoginWithCookieAsync(
            client,
            "cookie_attributes_user",
            "cookie-attributes@example.com");

        // 驗證
        Assert.Multiple(() =>
        {
            Assert.That(
                antiforgeryCookies,
                Has.Some.StartsWith("foodledger.antiforgery=").And.Not.Contains("; secure"));
            Assert.That(
                loginCookies,
                Has.Some.StartsWith("foodledger.auth=").And.Not.Contains("; secure"));
            Assert.That(antiforgeryCookies, Has.None.Contains("__host-"));
            Assert.That(loginCookies, Has.None.Contains("__host-"));
        });
    }

    /// <summary>
    /// 驗證 Production 的 HTTPS Cookie 保留 Secure、HttpOnly、SameSite=Lax 與 __Host- 前綴。
    /// </summary>
    [Test]
    public async Task Login_WhenProductionUsesHttps_PreservesSecureCookieAttributes()
    {
        // 準備
        await using var factory = new AuthApiFactory(environment: ProductionEnvironment);
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"),
            HandleCookies = true,
        });

        // 執行
        var (antiforgeryCookies, loginCookies) = await RegisterAndLoginWithCookieAsync(
            client,
            "production_cookie_user",
            "production-cookie@example.com");

        // 驗證
        Assert.Multiple(() =>
        {
            Assert.That(
                antiforgeryCookies,
                Has.Some.StartsWith("__host-foodledger.antiforgery=")
                    .And.Contains("; secure")
                    .And.Contains("httponly")
                    .And.Contains("samesite=lax"));
            Assert.That(
                loginCookies,
                Has.Some.StartsWith("__host-foodledger.auth=")
                    .And.Contains("; secure")
                    .And.Contains("httponly")
                    .And.Contains("samesite=lax"));
        });
    }

    /// <summary>
    /// 驗證 Production 環境已設定的前端來源可通過註冊 API 的 CORS 預檢。
    /// </summary>
    [Test]
    public async Task RegisterPreflight_WhenProductionOriginIsConfigured_ReturnsAllowedOrigin()
    {
        // 準備
        await using var factory = new AuthApiFactory(
            environment: ProductionEnvironment,
            allowedCorsOrigin: AllowedCorsOrigin);
        using var client = factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Options, RegisterPath);
        request.Headers.Add("Origin", AllowedCorsOrigin);
        request.Headers.Add("Access-Control-Request-Method", HttpMethod.Post.Method);
        request.Headers.Add("Access-Control-Request-Headers", "content-type");

        // 執行
        var response = await client.SendAsync(request);

        // 驗證
        var containsAllowedOrigin = response.Headers.TryGetValues(
            "Access-Control-Allow-Origin",
            out var allowedOrigins);

        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
            Assert.That(containsAllowedOrigin, Is.True);
            Assert.That(allowedOrigins ?? [], Does.Contain(AllowedCorsOrigin));
        });
    }

    /// <summary>
    /// 驗證 Production 環境未設定的前端來源無法取得 CORS 允許標頭。
    /// </summary>
    [Test]
    public async Task RegisterPreflight_WhenProductionOriginIsNotConfigured_DoesNotReturnAllowedOrigin()
    {
        // 準備
        await using var factory = new AuthApiFactory(
            environment: ProductionEnvironment,
            allowedCorsOrigin: AllowedCorsOrigin);
        using var client = factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Options, RegisterPath);
        request.Headers.Add("Origin", DeniedCorsOrigin);
        request.Headers.Add("Access-Control-Request-Method", HttpMethod.Post.Method);
        request.Headers.Add("Access-Control-Request-Headers", "content-type");

        // 執行
        var response = await client.SendAsync(request);

        // 驗證
        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
            Assert.That(response.Headers.Contains("Access-Control-Allow-Origin"), Is.False);
        });
    }

    /// <summary>
    /// 驗證 Production 環境不會套用 Development 的 loopback 自動放行規則。
    /// </summary>
    [Test]
    public async Task RegisterPreflight_WhenProductionLoopbackOriginIsNotConfigured_DoesNotReturnAllowedOrigin()
    {
        // 準備
        await using var factory = new AuthApiFactory(
            environment: ProductionEnvironment,
            allowedCorsOrigin: AllowedCorsOrigin);
        using var client = factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Options, RegisterPath);
        request.Headers.Add("Origin", LoopbackCorsOrigin);
        request.Headers.Add("Access-Control-Request-Method", HttpMethod.Post.Method);
        request.Headers.Add("Access-Control-Request-Headers", "content-type");

        // 執行
        var response = await client.SendAsync(request);

        // 驗證
        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
            Assert.That(response.Headers.Contains("Access-Control-Allow-Origin"), Is.False);
        });
    }

    /// <summary>
    /// 驗證 Development 環境未設定的前端來源無法取得 CORS 允許標頭。
    /// </summary>
    [Test]
    public async Task RegisterPreflight_WhenOriginIsNotConfigured_DoesNotReturnAllowedOrigin()
    {
        // 準備
        await using var factory = new AuthApiFactory(
            environment: DevelopmentEnvironment,
            allowedCorsOrigin: AllowedCorsOrigin);
        using var client = factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Options, RegisterPath);
        request.Headers.Add("Origin", DeniedCorsOrigin);
        request.Headers.Add("Access-Control-Request-Method", HttpMethod.Post.Method);
        request.Headers.Add("Access-Control-Request-Headers", "content-type");

        // 執行
        var response = await client.SendAsync(request);

        // 驗證
        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
            Assert.That(response.Headers.Contains("Access-Control-Allow-Origin"), Is.False);
        });
    }

    private sealed class AuthApiFactory : WebApplicationFactory<Program>
    {
        private readonly string _databaseName = $"AuthApiTests-{Guid.NewGuid()}";
        private readonly IAuthService? _authService;
        private readonly string _environment;
        private readonly string? _allowedCorsOrigin;

        public AuthApiFactory(
            IAuthService? authService = null,
            string environment = TestingEnvironment,
            string? allowedCorsOrigin = null)
        {
            _authService = authService;
            _environment = environment;
            _allowedCorsOrigin = allowedCorsOrigin;
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment(_environment);

            if (_allowedCorsOrigin is not null)
            {
                builder.UseSetting("Cors:AllowedOrigins:0", _allowedCorsOrigin);
            }

            builder.ConfigureServices(services =>
            {
                services.RemoveAll<DbContextOptions<ApplicationDbContext>>();
                services.RemoveAll<IDbContextOptionsConfiguration<ApplicationDbContext>>();
                services.AddDbContext<ApplicationDbContext>(options =>
                    options.UseInMemoryDatabase(_databaseName));

                if (_authService is not null)
                {
                    services.RemoveAll<IAuthService>();
                    services.AddSingleton(_authService);
                }
            });
        }
    }

    private static async Task AddAntiforgeryTokenAsync(HttpClient client)
    {
        using var response = await client.GetAsync(AntiforgeryPath);
        response.EnsureSuccessStatusCode();
        using var responseJson = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        client.DefaultRequestHeaders.Remove(AntiforgeryHeaderName);
        client.DefaultRequestHeaders.Add(
            AntiforgeryHeaderName,
            responseJson.RootElement.GetProperty("requestToken").GetString());
    }

    private static async Task<(string[] AntiforgeryCookies, string[] LoginCookies)>
        RegisterAndLoginWithCookieAsync(
            HttpClient client,
            string userAccount,
            string email)
    {
        using var registerResponse = await client.PostAsJsonAsync(
            RegisterPath,
            new
            {
                UserAccount = userAccount,
                DisplayName = $"{userAccount} 使用者",
                Email = email,
                Password = ValidPassword,
            });
        registerResponse.EnsureSuccessStatusCode();

        using var antiforgeryResponse = await client.GetAsync(AntiforgeryPath);
        antiforgeryResponse.EnsureSuccessStatusCode();
        using var antiforgeryJson = JsonDocument.Parse(
            await antiforgeryResponse.Content.ReadAsStringAsync());
        client.DefaultRequestHeaders.Remove(AntiforgeryHeaderName);
        client.DefaultRequestHeaders.Add(
            AntiforgeryHeaderName,
            antiforgeryJson.RootElement.GetProperty("requestToken").GetString());

        using var loginResponse = await client.PostAsJsonAsync(
            CookieLoginPath,
            new
            {
                LoginId = userAccount,
                Password = ValidPassword,
            });
        loginResponse.EnsureSuccessStatusCode();

        return (
            antiforgeryResponse.Headers
                .GetValues("Set-Cookie")
                .Select(cookie => cookie.ToLowerInvariant())
                .ToArray(),
            loginResponse.Headers
                .GetValues("Set-Cookie")
                .Select(cookie => cookie.ToLowerInvariant())
                .ToArray());
    }

    private sealed class ThrowingAuthService : IAuthService
    {
        public Task<AuthServiceResult> RegisterAsync(RegisterRequest request)
        {
            throw new InvalidOperationException("Sensitive database detail");
        }

        public Task<AuthServiceResult> LoginAsync(LoginRequest request)
        {
            throw new NotSupportedException();
        }

        public Task<CurrentUserResponse?> GetCurrentUserAsync(long userId)
        {
            throw new NotSupportedException();
        }
    }
}
