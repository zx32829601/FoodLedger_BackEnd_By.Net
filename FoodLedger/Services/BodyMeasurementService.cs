using FoodLedger.Data.Entities;
using FoodLedger.DTOs.BodyMeasurements;
using FoodLedger.DTOs.Errors;
using FoodLedger.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace FoodLedger.Services;

/// <summary>實作目前登入使用者的身體測量歷史、修正與安全刪除。</summary>
public sealed class BodyMeasurementService(
    ApplicationDbContext dbContext,
    ICurrentUserService currentUserService,
    IBodyMeasurementImpactTokenService impactTokenService,
    TimeProvider timeProvider) : IBodyMeasurementService
{
    private const int EmptyAffectedSnapshotCount = 0;
    private const bool EmptyCurrentTargetImpact = false;

    /// <inheritdoc />
    public async Task<BodyMeasurementPageResponse> GetHistoryAsync(
        BodyMeasurementQueryRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var userId = GetCurrentUserId();
        var query = dbContext.BodyMeasurements
            .AsNoTracking()
            .Where(measurement => measurement.UserId == userId);

        if (request.FromDate.HasValue || request.ToDate.HasValue)
        {
            var profile = await dbContext.BodyProfiles
                .AsNoTracking()
                .SingleOrDefaultAsync(item => item.UserId == userId, cancellationToken)
                ?? throw new BodyMeasurementProfileRequiredException();
            var timeZone = TimeZoneInfo.FindSystemTimeZoneById(profile.TimeZone);

            if (request.FromDate is { } fromDate)
            {
                var startAt = LocalDateRange.GetUtcRange(fromDate, fromDate, timeZone).StartAt;
                query = query.Where(measurement => measurement.MeasuredAt >= startAt);
            }

            if (request.ToDate is { } toDate)
            {
                var endAt = LocalDateRange.GetUtcRange(
                    toDate,
                    toDate.AddDays(1),
                    timeZone).EndAt;
                query = query.Where(measurement => measurement.MeasuredAt < endAt);
            }
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var skip = (long)(request.Page - BodyMeasurementRules.MinimumPage)
            * request.PageSize;
        BodyMeasurementResponse[] items;
        if (skip > int.MaxValue)
        {
            items = [];
        }
        else
        {
            items = await query
                .OrderByDescending(measurement => measurement.MeasuredAt)
                .ThenByDescending(measurement => measurement.CreatedAt)
                .ThenByDescending(measurement => measurement.MeasurementId)
                .Skip((int)skip)
                .Take(request.PageSize)
                .Select(measurement => Map(measurement))
                .ToArrayAsync(cancellationToken);
        }

        return new BodyMeasurementPageResponse
        {
            Items = items,
            Page = request.Page,
            PageSize = request.PageSize,
            TotalCount = totalCount,
        };
    }

    /// <inheritdoc />
    public async Task<BodyMeasurementResponse> CreateAsync(
        CreateBodyMeasurementRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var userId = GetCurrentUserId();
        ValidateValues(
            request.WeightInKilograms,
            request.BodyFatPercentage,
            request.MuscleMassInKilograms);

        var measurement = new BodyMeasurement
        {
            UserId = userId,
            WeightInKilograms = request.WeightInKilograms,
            BodyFatPercentage = request.BodyFatPercentage,
            MuscleMassInKilograms = request.MuscleMassInKilograms,
            MeasuredAt = timeProvider.GetUtcNow(),
            Version = Guid.NewGuid(),
        };
        dbContext.BodyMeasurements.Add(measurement);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Map(measurement);
    }

    /// <inheritdoc />
    public async Task<BodyMeasurementResponse> UpdateAsync(
        long measurementId,
        UpdateBodyMeasurementRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var userId = GetCurrentUserId();
        ValidateValues(
            request.WeightInKilograms,
            request.BodyFatPercentage,
            request.MuscleMassInKilograms);
        var measurement = await GetOwnedAsync(measurementId, userId, cancellationToken);
        if (!request.Version.HasValue
            || request.Version == Guid.Empty
            || measurement.Version != request.Version)
        {
            throw new BodyMeasurementConflictException();
        }

        measurement.WeightInKilograms = request.WeightInKilograms;
        measurement.BodyFatPercentage = request.BodyFatPercentage;
        measurement.MuscleMassInKilograms = request.MuscleMassInKilograms;
        measurement.Version = Guid.NewGuid();

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new BodyMeasurementConflictException();
        }

        return Map(measurement);
    }

    /// <inheritdoc />
    public async Task<BodyMeasurementDeletionImpactResponse> GetDeletionImpactAsync(
        long measurementId,
        CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        var measurement = await dbContext.BodyMeasurements
            .AsNoTracking()
            .SingleOrDefaultAsync(
                item => item.MeasurementId == measurementId && item.UserId == userId,
                cancellationToken)
            ?? throw new KeyNotFoundException();
        var token = impactTokenService.Create(
            userId,
            measurementId,
            measurement.Version,
            EmptyAffectedSnapshotCount,
            EmptyCurrentTargetImpact);

        return new BodyMeasurementDeletionImpactResponse
        {
            MeasurementId = measurementId,
            Version = measurement.Version,
            AffectedSnapshotCount = EmptyAffectedSnapshotCount,
            AffectsCurrentTarget = EmptyCurrentTargetImpact,
            ExpiresAt = token.ExpiresAt,
            ImpactToken = token.Value,
        };
    }

    /// <inheritdoc />
    public async Task DeleteAsync(
        long measurementId,
        DeleteBodyMeasurementRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var userId = GetCurrentUserId();
        var measurement = await GetOwnedAsync(measurementId, userId, cancellationToken);
        if (!request.Version.HasValue
            || request.Version == Guid.Empty
            || measurement.Version != request.Version
            || !impactTokenService.IsValid(
                request.ImpactToken,
                userId,
                measurementId,
                measurement.Version,
                EmptyAffectedSnapshotCount,
                EmptyCurrentTargetImpact))
        {
            throw new BodyMeasurementConflictException();
        }

        await using IDbContextTransaction? transaction = dbContext.Database.IsRelational()
            ? await dbContext.Database.BeginTransactionAsync(cancellationToken)
            : null;
        dbContext.BodyMeasurements.Remove(measurement);
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            if (transaction is not null)
            {
                await transaction.CommitAsync(cancellationToken);
            }
        }
        catch (DbUpdateConcurrencyException)
        {
            if (transaction is not null)
            {
                await transaction.RollbackAsync(cancellationToken);
            }

            throw new BodyMeasurementConflictException();
        }
    }

    private async Task<BodyMeasurement> GetOwnedAsync(
        long measurementId,
        long userId,
        CancellationToken cancellationToken) =>
        await dbContext.BodyMeasurements.SingleOrDefaultAsync(
            item => item.MeasurementId == measurementId && item.UserId == userId,
            cancellationToken)
        ?? throw new KeyNotFoundException();

    private long GetCurrentUserId() =>
        currentUserService.IsAuthenticated && currentUserService.UserId.HasValue
            ? currentUserService.UserId.Value
            : throw new UnauthorizedAccessException();

    private static void ValidateValues(
        decimal weight,
        decimal? bodyFatPercentage,
        decimal? muscleMass)
    {
        if (weight is < BodyMeasurementRules.MinimumWeight
            or > BodyMeasurementRules.MaximumWeight)
        {
            throw Validation(
                nameof(CreateBodyMeasurementRequest.WeightInKilograms),
                BodyMeasurementErrorCodes.WeightOutOfRange);
        }

        if (bodyFatPercentage is { } bodyFat
            && bodyFat is < BodyMeasurementRules.MinimumBodyFatPercentage
                or > BodyMeasurementRules.MaximumBodyFatPercentage)
        {
            throw Validation(
                nameof(CreateBodyMeasurementRequest.BodyFatPercentage),
                BodyMeasurementErrorCodes.BodyFatOutOfRange);
        }

        if (muscleMass is { } muscle && (muscle <= 0 || muscle > weight))
        {
            throw Validation(
                nameof(CreateBodyMeasurementRequest.MuscleMassInKilograms),
                BodyMeasurementErrorCodes.MuscleMassOutOfRange);
        }

        if (!HasSupportedPrecision(weight))
        {
            throw Validation(
                nameof(CreateBodyMeasurementRequest.WeightInKilograms),
                BodyMeasurementErrorCodes.PrecisionExceeded);
        }

        if (bodyFatPercentage.HasValue && !HasSupportedPrecision(bodyFatPercentage.Value))
        {
            throw Validation(
                nameof(CreateBodyMeasurementRequest.BodyFatPercentage),
                BodyMeasurementErrorCodes.PrecisionExceeded);
        }

        if (muscleMass.HasValue && !HasSupportedPrecision(muscleMass.Value))
        {
            throw Validation(
                nameof(CreateBodyMeasurementRequest.MuscleMassInKilograms),
                BodyMeasurementErrorCodes.PrecisionExceeded);
        }
    }

    private static bool HasSupportedPrecision(decimal value) =>
        decimal.Round(value, BodyMeasurementRules.MaximumDecimalPlaces) == value;

    private static BodyMeasurementValidationException Validation(
        string fieldName,
        string errorCode) => new(fieldName, errorCode);

    private static BodyMeasurementResponse Map(BodyMeasurement measurement) => new()
    {
        MeasurementId = measurement.MeasurementId,
        WeightInKilograms = measurement.WeightInKilograms,
        BodyFatPercentage = measurement.BodyFatPercentage,
        MuscleMassInKilograms = measurement.MuscleMassInKilograms,
        MeasuredAt = measurement.MeasuredAt,
        Version = measurement.Version,
    };
}
