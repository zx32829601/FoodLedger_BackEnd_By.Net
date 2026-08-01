using FoodLedger.Data.Entities;
using FoodLedger.DTOs.BodyProfiles;
using FoodLedger.DTOs.DefinedCodes;
using FoodLedger.DTOs.Errors;
using FoodLedger.Models;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace FoodLedger.Services;

/// <summary>
/// 實作目前使用者身體資料的查詢、驗證與建立或修改。
/// </summary>
public sealed class BodyProfileService(
    ApplicationDbContext dbContext,
    ICurrentUserService currentUserService,
    TimeProvider timeProvider) : IBodyProfileService
{
    private const int MinimumAge = 18;
    private const int MaximumAge = 120;
    private const decimal MinimumHeight = 100m;
    private const decimal MaximumHeight = 250m;

    public async Task<BodyProfileResponse> GetAsync(
        string langCode,
        CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        var profile = await dbContext.BodyProfiles
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.UserId == userId, cancellationToken)
            ?? throw new KeyNotFoundException();

        var fitnessGoal = await GetStoredCodeAsync(
            DefinedCodeTypes.FitnessGoal,
            profile.FitnessGoalCode,
            langCode,
            cancellationToken);
        var activityLevel = await GetStoredCodeAsync(
            DefinedCodeTypes.ActivityLevel,
            profile.ActivityLevelCode,
            langCode,
            cancellationToken);

        return Map(profile, fitnessGoal, activityLevel);
    }

    public async Task<BodyProfileResponse> UpsertAsync(
        UpsertBodyProfileRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var userId = GetCurrentUserId();
        await ValidateAsync(request, cancellationToken);

        var profile = await dbContext.BodyProfiles
            .SingleOrDefaultAsync(item => item.UserId == userId, cancellationToken);

        var isCreating = profile is null;
        if (profile is null)
        {
            if (request.Version.HasValue)
            {
                throw new BodyProfileConflictException();
            }

            profile = new BodyProfile
            {
                UserId = userId,
                BirthDate = request.BirthDate!.Value,
                BiologicalSexCode = request.BiologicalSexCode,
                HeightInCentimeters = request.HeightInCentimeters,
                FitnessGoalCode = request.FitnessGoalCode,
                ActivityLevelCode = request.ActivityLevelCode,
                TimeZone = request.TimeZone,
                Version = Guid.NewGuid(),
            };
            dbContext.BodyProfiles.Add(profile);
        }
        else
        {
            if (!request.Version.HasValue || request.Version.Value != profile.Version)
            {
                throw new BodyProfileConflictException();
            }

            profile.BirthDate = request.BirthDate!.Value;
            profile.BiologicalSexCode = request.BiologicalSexCode;
            profile.HeightInCentimeters = request.HeightInCentimeters;
            profile.FitnessGoalCode = request.FitnessGoalCode;
            profile.ActivityLevelCode = request.ActivityLevelCode;
            profile.TimeZone = request.TimeZone;
            profile.Version = Guid.NewGuid();
        }

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new BodyProfileConflictException();
        }
        catch (DbUpdateException exception)
            when (isCreating && IsConcurrentCreate(exception))
        {
            // 兩個裝置同時首次建立時，使用者主鍵會阻擋第二筆寫入。
            throw new BodyProfileConflictException();
        }

        return Map(profile);
    }

    private async Task ValidateAsync(
        UpsertBodyProfileRequest request,
        CancellationToken cancellationToken)
    {
        if (!request.BirthDate.HasValue)
        {
            throw Validation(
                nameof(request.BirthDate),
                BodyProfileErrorCodes.BirthDateRequired);
        }

        if (!LocalizationRules.IsValidTimeZone(request.TimeZone))
        {
            throw Validation(
                nameof(request.TimeZone),
                BodyProfileErrorCodes.InvalidTimeZone);
        }

        var currentLocalDate = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(
            timeProvider.GetUtcNow(),
            TimeZoneInfo.FindSystemTimeZoneById(request.TimeZone)).DateTime);
        var age = CalculateAge(request.BirthDate.Value, currentLocalDate);
        if (age is < MinimumAge or > MaximumAge)
        {
            throw Validation(nameof(request.BirthDate), BodyProfileErrorCodes.AgeOutOfRange);
        }

        if (!BiologicalSexCodes.IsSupported(request.BiologicalSexCode))
        {
            throw Validation(
                nameof(request.BiologicalSexCode),
                BodyProfileErrorCodes.InvalidBiologicalSex);
        }

        if (request.HeightInCentimeters is < MinimumHeight or > MaximumHeight)
        {
            throw Validation(
                nameof(request.HeightInCentimeters),
                BodyProfileErrorCodes.HeightOutOfRange);
        }

        if (decimal.Round(request.HeightInCentimeters, 2) != request.HeightInCentimeters)
        {
            throw Validation(
                nameof(request.HeightInCentimeters),
                BodyProfileErrorCodes.HeightPrecisionExceeded);
        }

        if (!await IsActiveCodeAsync(
                DefinedCodeTypes.FitnessGoal,
                request.FitnessGoalCode,
                cancellationToken))
        {
            throw Validation(
                nameof(request.FitnessGoalCode),
                BodyProfileErrorCodes.InvalidFitnessGoal);
        }

        if (!await IsActiveCodeAsync(
                DefinedCodeTypes.ActivityLevel,
                request.ActivityLevelCode,
                cancellationToken))
        {
            throw Validation(
                nameof(request.ActivityLevelCode),
                BodyProfileErrorCodes.InvalidActivityLevel);
        }
    }

    private Task<bool> IsActiveCodeAsync(
        string codeType,
        string code,
        CancellationToken cancellationToken) =>
        dbContext.DefinedCodes.AnyAsync(
            item => item.CodeType == codeType && item.Code == code && item.IsActive,
            cancellationToken);

    private async Task<DefinedCodeResponse?> GetStoredCodeAsync(
        string codeType,
        string code,
        string langCode,
        CancellationToken cancellationToken)
    {
        var requestedLangCode = LocalizationRules.NormalizeLangCode(langCode);
        var fallbackLangCode = LocalizationRules.NormalizeLangCode(
            LocalizationRules.FallbackLangCode);

        var row = await dbContext.DefinedCodes
            .AsNoTracking()
            .Where(item => item.CodeType == codeType && item.Code == code)
            .Select(item => new
            {
                item.Code,
                item.SortOrder,
                Translation = item.Translations
                    .Where(translation =>
                        translation.LangCode.ToLower() == requestedLangCode
                        || translation.LangCode.ToLower() == fallbackLangCode)
                    .OrderBy(translation =>
                        translation.LangCode.ToLower() == requestedLangCode ? 0 : 1)
                    .Select(translation => new
                    {
                        translation.DisplayName,
                        translation.LangCode,
                        translation.Note,
                    })
                    .FirstOrDefault(),
            })
            .SingleOrDefaultAsync(cancellationToken);

        return row is null
            ? null
            : new DefinedCodeResponse
            {
                Code = row.Code,
                SortOrder = row.SortOrder,
                DisplayName = row.Translation?.DisplayName ?? row.Code,
                LangCode = row.Translation?.LangCode,
                Note = row.Translation?.Note,
            };
    }

    private long GetCurrentUserId() =>
        currentUserService.IsAuthenticated && currentUserService.UserId.HasValue
            ? currentUserService.UserId.Value
            : throw new UnauthorizedAccessException();

    private static int CalculateAge(DateOnly birthDate, DateOnly currentDate)
    {
        var age = currentDate.Year - birthDate.Year;
        if (birthDate > currentDate.AddYears(-age))
        {
            age--;
        }

        return age;
    }

    private static BodyProfileValidationException Validation(
        string fieldName,
        string errorCode) => new(fieldName, errorCode);

    private static bool IsConcurrentCreate(DbUpdateException exception) =>
        exception.InnerException is PostgresException
        {
            SqlState: PostgresErrorCodes.UniqueViolation,
            ConstraintName: "PK_body_profile",
        };

    private static BodyProfileResponse Map(
        BodyProfile profile,
        DefinedCodeResponse? fitnessGoal = null,
        DefinedCodeResponse? activityLevel = null) => new()
        {
            BirthDate = profile.BirthDate,
            BiologicalSexCode = profile.BiologicalSexCode,
            HeightInCentimeters = profile.HeightInCentimeters,
            FitnessGoalCode = profile.FitnessGoalCode,
            FitnessGoalDisplayName = fitnessGoal?.DisplayName,
            FitnessGoalLangCode = fitnessGoal?.LangCode,
            FitnessGoalNote = fitnessGoal?.Note,
            ActivityLevelCode = profile.ActivityLevelCode,
            ActivityLevelDisplayName = activityLevel?.DisplayName,
            ActivityLevelLangCode = activityLevel?.LangCode,
            ActivityLevelNote = activityLevel?.Note,
            TimeZone = profile.TimeZone,
            Version = profile.Version,
        };
}
