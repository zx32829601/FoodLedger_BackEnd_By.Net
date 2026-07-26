using System.ComponentModel.DataAnnotations;
using FoodLedger.DTOs.Errors;

namespace FoodLedger.DTOs.Auth;

/// <summary>
/// 建立 FoodLedger 使用者帳號的請求資料。
/// </summary>
public sealed class RegisterRequest : IValidatableObject
{
    /// <summary>
    /// 用於登入的唯一帳號，限 4 到 30 個英文字母、數字、底線或連字號。
    /// </summary>
    [Required(ErrorMessage = ApiValidationErrorCodes.UserAccountInvalid)]
    [StringLength(
        30,
        MinimumLength = 4,
        ErrorMessage = ApiValidationErrorCodes.UserAccountInvalid)]
    [RegularExpression(
        @"^[A-Za-z0-9_-]+$",
        ErrorMessage = ApiValidationErrorCodes.UserAccountInvalid)]
    public string UserAccount { get; init; } = string.Empty;

    /// <summary>
    /// 對外顯示名稱，前後空白會由後端移除。
    /// </summary>
    [Required(ErrorMessage = ApiValidationErrorCodes.DisplayNameInvalid)]
    public string DisplayName { get; init; } = string.Empty;

    /// <summary>
    /// 使用者唯一的有效電子郵件地址。
    /// </summary>
    [Required(ErrorMessage = ApiValidationErrorCodes.EmailInvalid)]
    [EmailAddress(ErrorMessage = ApiValidationErrorCodes.EmailInvalid)]
    public string Email { get; init; } = string.Empty;

    /// <summary>
    /// 至少八個字元，並須符合 Identity 設定的英文大小寫與數字規則。
    /// </summary>
    [Required(ErrorMessage = ApiValidationErrorCodes.PasswordInvalid)]
    [RegularExpression(
        @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).{8,}$",
        ErrorMessage = ApiValidationErrorCodes.PasswordInvalid)]
    public string Password { get; init; } = string.Empty;

    /// <summary>
    /// 以實際會儲存的修剪後顯示名稱驗證長度。
    /// </summary>
    /// <param name="validationContext">目前驗證內容。</param>
    /// <returns>顯示名稱不符合限制時回傳對應的欄位驗證結果。</returns>
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var trimmedDisplayNameLength = DisplayName.Trim().Length;
        if (trimmedDisplayNameLength is < 1 or > 30)
        {
            yield return new ValidationResult(
                ApiValidationErrorCodes.DisplayNameInvalid,
                [nameof(DisplayName)]);
        }
    }
}
