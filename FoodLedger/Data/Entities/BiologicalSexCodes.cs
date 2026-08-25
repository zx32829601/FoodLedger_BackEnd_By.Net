namespace FoodLedger.Data.Entities;

/// <summary>
/// 身體資料可接受的生理性別代碼。
/// </summary>
public static class BiologicalSexCodes
{
    public const string Male = "MALE";
    public const string Female = "FEMALE";

    /// <summary>判斷代碼是否為系統支援的生理性別。</summary>
    /// <param name="code">要檢查的代碼。</param>
    /// <returns>代碼受支援時為 <see langword="true" />；否則為 <see langword="false" />。</returns>
    public static bool IsSupported(string? code) =>
        code is Male or Female;
}
