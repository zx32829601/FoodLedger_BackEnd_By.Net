using FoodLedger.Models;

namespace FoodLedger.Tests.Models;

/// <summary>
/// 驗證 API 共用語系與時區規則。
/// </summary>
public sealed class LocalizationRulesTests
{
    /// <summary>
    /// 驗證支援的一般與 private-use BCP 47 語系代碼可通過。
    /// </summary>
    [TestCase("zh-TW")]
    [TestCase("en-US")]
    [TestCase("x-food-ledger")]
    public void IsValidLangCode_WhenCodeIsSupported_ReturnsTrue(string langCode)
    {
        Assert.That(LocalizationRules.IsValidLangCode(langCode), Is.True);
    }

    /// <summary>
    /// 驗證空白、底線格式與超長語系代碼會被拒絕。
    /// </summary>
    [Test]
    public void IsValidLangCode_WhenCodeIsInvalid_ReturnsFalse()
    {
        Assert.Multiple(() =>
        {
            Assert.That(LocalizationRules.IsValidLangCode(""), Is.False);
            Assert.That(LocalizationRules.IsValidLangCode("zh_TW"), Is.False);
            Assert.That(
                LocalizationRules.IsValidLangCode(
                    new string('a', LocalizationRules.MaximumLangCodeLength + 1)),
                Is.False);
        });
    }

    /// <summary>
    /// 驗證可解析的 IANA 時區可通過。
    /// </summary>
    [Test]
    public void IsValidTimeZone_WhenTimeZoneIsIana_ReturnsTrue()
    {
        Assert.That(LocalizationRules.IsValidTimeZone("Asia/Taipei"), Is.True);
    }

    /// <summary>
    /// 驗證 Windows ID、空白與不存在的時區不符合 API 契約。
    /// </summary>
    [Test]
    public void IsValidTimeZone_WhenTimeZoneIsNotIana_ReturnsFalse()
    {
        Assert.Multiple(() =>
        {
            Assert.That(
                LocalizationRules.IsValidTimeZone("Taipei Standard Time"),
                Is.False);
            Assert.That(LocalizationRules.IsValidTimeZone(""), Is.False);
            Assert.That(
                LocalizationRules.IsValidTimeZone("Not/A-TimeZone"),
                Is.False);
        });
    }

    /// <summary>
    /// 驗證語系查詢比較會使用 invariant 小寫格式。
    /// </summary>
    [Test]
    public void NormalizeLangCode_WhenCodeIsValid_ReturnsInvariantLowerCase()
    {
        Assert.That(
            LocalizationRules.NormalizeLangCode("ZH-tW"),
            Is.EqualTo("zh-tw"));
    }
}
