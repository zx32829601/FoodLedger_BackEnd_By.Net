namespace FoodLedger.Models;

/// <summary>
/// 將 IANA 時區中的本地日曆範圍轉為 UTC 半開區間。
/// </summary>
public static class LocalDateRange
{
    // 最多向後搜尋四十八小時，涵蓋整日被時區規則跳過的極端情境。
    private const int MaximumInvalidMinutes = 48 * 60;

    /// <summary>
    /// 取得從本地起始日到本地排他結束日的 UTC 半開區間。
    /// </summary>
    /// <param name="startDate">包含的本地起始日期。</param>
    /// <param name="endDateExclusive">不包含的本地結束日期。</param>
    /// <param name="timeZone">用來解析本地日界的時區。</param>
    /// <returns>可直接用於資料庫查詢的 UTC 起訖 instant。</returns>
    public static (DateTimeOffset StartAt, DateTimeOffset EndAt) GetUtcRange(
        DateOnly startDate,
        DateOnly endDateExclusive,
        TimeZoneInfo timeZone)
    {
        ArgumentNullException.ThrowIfNull(timeZone);
        if (endDateExclusive < startDate)
        {
            throw new ArgumentOutOfRangeException(
                nameof(endDateExclusive),
                "The exclusive end date cannot be earlier than the start date.");
        }

        return (
            ResolveStartOfDay(startDate, timeZone),
            ResolveStartOfDay(endDateExclusive, timeZone));
    }

    private static DateTimeOffset ResolveStartOfDay(
        DateOnly date,
        TimeZoneInfo timeZone)
    {
        var localTime = DateTime.SpecifyKind(
            date.ToDateTime(TimeOnly.MinValue),
            DateTimeKind.Unspecified);

        var invalidMinutes = 0;
        while (timeZone.IsInvalidTime(localTime)
            && invalidMinutes < MaximumInvalidMinutes)
        {
            localTime = localTime.AddMinutes(1);
            invalidMinutes++;
        }

        if (timeZone.IsInvalidTime(localTime))
        {
            throw new InvalidTimeZoneException(
                $"Cannot resolve the start of {date:yyyy-MM-dd} in {timeZone.Id}.");
        }

        var offset = timeZone.IsAmbiguousTime(localTime)
            ? timeZone.GetAmbiguousTimeOffsets(localTime).Max()
            : timeZone.GetUtcOffset(localTime);
        return new DateTimeOffset(localTime, offset).ToUniversalTime();
    }
}
