using FoodLedger.Models;

namespace FoodLedger.Tests.Models;

/// <summary>
/// 驗證本地日曆日期轉換為 UTC 半開區間的共用規則。
/// </summary>
public sealed class LocalDateRangeTests
{
    /// <summary>
    /// 驗證 DST 開始日依當地午夜換算，區間長度為二十三小時。
    /// </summary>
    [Test]
    public void GetUtcRange_WhenDaylightSavingTimeStarts_ReturnsLocalDayBoundaries()
    {
        // Arrange
        var timeZone = TimeZoneInfo.FindSystemTimeZoneById("America/New_York");

        // Act
        var (startAt, endAt) = LocalDateRange.GetUtcRange(
            new DateOnly(2026, 3, 8),
            new DateOnly(2026, 3, 9),
            timeZone);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(
                startAt,
                Is.EqualTo(new DateTimeOffset(2026, 3, 8, 5, 0, 0, TimeSpan.Zero)));
            Assert.That(
                endAt,
                Is.EqualTo(new DateTimeOffset(2026, 3, 9, 4, 0, 0, TimeSpan.Zero)));
        });
    }

    /// <summary>
    /// 驗證午夜進入 DST 時會使用當天第一個有效本地時間，不拋出例外。
    /// </summary>
    [Test]
    public void GetUtcRange_WhenMidnightIsInvalid_UsesFirstValidLocalTime()
    {
        // Arrange
        var timeZone = TimeZoneInfo.FindSystemTimeZoneById("America/Havana");

        // Act
        var (startAt, endAt) = LocalDateRange.GetUtcRange(
            new DateOnly(2026, 3, 8),
            new DateOnly(2026, 3, 9),
            timeZone);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(
                startAt,
                Is.EqualTo(new DateTimeOffset(2026, 3, 8, 5, 0, 0, TimeSpan.Zero)));
            Assert.That(
                endAt,
                Is.EqualTo(new DateTimeOffset(2026, 3, 9, 4, 0, 0, TimeSpan.Zero)));
        });
    }

    /// <summary>
    /// 驗證午夜為重複時間時選擇較早的 UTC instant，完整涵蓋兩次午夜。
    /// </summary>
    [Test]
    public void GetUtcRange_WhenMidnightIsAmbiguous_UsesEarlierUtcInstant()
    {
        // Arrange
        var timeZone = TimeZoneInfo.FindSystemTimeZoneById("America/Havana");

        // Act
        var (startAt, endAt) = LocalDateRange.GetUtcRange(
            new DateOnly(2026, 11, 1),
            new DateOnly(2026, 11, 2),
            timeZone);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(
                startAt,
                Is.EqualTo(new DateTimeOffset(2026, 11, 1, 4, 0, 0, TimeSpan.Zero)));
            Assert.That(
                endAt,
                Is.EqualTo(new DateTimeOffset(2026, 11, 2, 5, 0, 0, TimeSpan.Zero)));
        });
    }
}
