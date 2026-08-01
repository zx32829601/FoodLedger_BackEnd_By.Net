namespace FoodLedger.Data.Entities;

/// <summary>
/// 身體資料可接受的生理性別代碼。
/// </summary>
public static class BiologicalSexCodes
{
    public const string Male = "MALE";
    public const string Female = "FEMALE";

    public static bool IsSupported(string? code) =>
        code is Male or Female;
}
