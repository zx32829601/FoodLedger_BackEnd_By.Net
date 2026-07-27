using FoodLedger.Data.Entities;
using FoodLedger.DTOs.Auth;
using FoodLedger.DTOs.Users;
using Microsoft.AspNetCore.Identity;

namespace FoodLedger.Services;

/// <summary>
/// 使用 ASP.NET Core Identity 實作 FoodLedger 的身分驗證商業流程。
/// </summary>
public sealed class AuthService : IAuthService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;

    /// <summary>
    /// 初始化身分驗證服務。
    /// </summary>
    /// <param name="userManager">負責 Identity 使用者與密碼安全處理的框架服務。</param>
    /// <param name="signInManager">負責密碼登入驗證與鎖定狀態處理的框架服務。</param>
    public AuthService(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager)
    {
        _userManager = userManager;
        _signInManager = signInManager;
    }

    /// <inheritdoc />
    public async Task<AuthServiceResult> RegisterAsync(RegisterRequest request)
    {
        var user = new ApplicationUser
        {
            UserName = request.UserAccount,
            DisplayName = request.DisplayName.Trim(),
            Email = request.Email.Trim(),
        };
        var result = await _userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            if (result.Errors.Any(error => error.Code == nameof(IdentityErrorDescriber.DuplicateUserName)))
            {
                return new AuthServiceFailure(
                    AuthErrorCodes.UserAccountAlreadyExists,
                    "此使用者帳號已被註冊。",
                    "userAccount");
            }

            if (result.Errors.Any(error => error.Code == nameof(IdentityErrorDescriber.DuplicateEmail)))
            {
                return new AuthServiceFailure(
                    AuthErrorCodes.EmailAlreadyExists,
                    "此電子郵件已被註冊。",
                    "email");
            }

            throw new InvalidOperationException(
                string.Join(", ", result.Errors.Select(error => error.Code)));
        }

        return new AuthServiceSuccess(user);
    }

    /// <inheritdoc />
    public async Task<AuthServiceResult> LoginAsync(LoginRequest request)
    {
        var loginId = request.LoginId.Trim();
        var user = loginId.Contains('@', StringComparison.Ordinal)
            ? await _userManager.FindByEmailAsync(loginId)
            : await _userManager.FindByNameAsync(loginId);

        if (user is null)
        {
            return InvalidCredentials();
        }

        var signInResult = await _signInManager.CheckPasswordSignInAsync(
            user,
            request.Password,
            lockoutOnFailure: true);
        if (!signInResult.Succeeded)
        {
            return InvalidCredentials();
        }

        return new AuthServiceSuccess(user);
    }

    /// <inheritdoc />
    public async Task<CurrentUserResponse?> GetCurrentUserAsync(long userId)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user is null)
        {
            return null;
        }

        var isAdmin = await _userManager.IsInRoleAsync(user, Security.ApplicationRoles.Admin);
        return CurrentUserResponseMapper.Map(user, isAdmin);
    }

    private static AuthServiceResult InvalidCredentials()
    {
        return new AuthServiceFailure(
            AuthErrorCodes.InvalidCredentials,
            "帳號、電子郵件或密碼不正確。");
    }
}
