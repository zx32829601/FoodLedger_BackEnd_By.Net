using System.ComponentModel.DataAnnotations;
using FoodLedger.DTOs.Errors;

namespace FoodLedger.DTOs.BodyMeasurements;

/// <summary>身體測量歷史的分頁與本地日期篩選條件。</summary>
public sealed class BodyMeasurementQueryRequest : IValidatableObject
{
    public int Page { get; init; } = BodyMeasurementRules.MinimumPage;
    public int PageSize { get; init; } = BodyMeasurementRules.DefaultPageSize;
    public DateOnly? FromDate { get; init; }
    public DateOnly? ToDate { get; init; }

    /// <inheritdoc />
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Page < BodyMeasurementRules.MinimumPage)
        {
            yield return new ValidationResult(
                BodyMeasurementErrorCodes.PageOutOfRange,
                [nameof(Page)]);
        }

        if (PageSize is < BodyMeasurementRules.MinimumPageSize
            or > BodyMeasurementRules.MaximumPageSize)
        {
            yield return new ValidationResult(
                BodyMeasurementErrorCodes.PageSizeOutOfRange,
                [nameof(PageSize)]);
        }

        if (FromDate.HasValue && ToDate.HasValue && FromDate > ToDate)
        {
            yield return new ValidationResult(
                BodyMeasurementErrorCodes.InvalidDateRange,
                [nameof(FromDate), nameof(ToDate)]);
        }

        if (ToDate == DateOnly.MaxValue)
        {
            yield return new ValidationResult(
                BodyMeasurementErrorCodes.InvalidDateRange,
                [nameof(ToDate)]);
        }
    }
}
