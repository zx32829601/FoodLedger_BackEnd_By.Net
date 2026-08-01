using FoodLedger.Data.Entities;
using FoodLedger.DTOs.Foods;
using Microsoft.EntityFrameworkCore;

namespace FoodLedger.Services;

/// <summary>
/// 使用單一 aggregate 寫入食物翻譯與每 100 克營養素資料。
/// </summary>
public sealed class FoodMaintenanceService(ApplicationDbContext dbContext)
    : IFoodMaintenanceService
{
    /// <inheritdoc />
    public async Task<AdminFoodResponse> GetAsync(
        long foodId,
        CancellationToken cancellationToken = default)
    {
        return await Query()
            .FirstOrDefaultAsync(food => food.FoodId == foodId, cancellationToken)
            ?? throw new KeyNotFoundException();
    }

    /// <inheritdoc />
    public async Task<AdminFoodResponse> CreateAsync(
        UpsertFoodRequest request,
        CancellationToken cancellationToken = default)
    {
        var foodCode = request.FoodCode.Trim();
        await ValidateAsync(null, foodCode, request, cancellationToken);

        var food = new SimpleFood { FoodCode = foodCode };
        Apply(food, request);
        dbContext.SimpleFoods.Add(food);
        await dbContext.SaveChangesAsync(cancellationToken);
        return await GetAsync(food.FoodId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<AdminFoodResponse> UpdateAsync(
        long foodId,
        UpsertFoodRequest request,
        CancellationToken cancellationToken = default)
    {
        var food = await dbContext.SimpleFoods
            .Include(item => item.Translations)
            .FirstOrDefaultAsync(item => item.FoodId == foodId, cancellationToken)
            ?? throw new KeyNotFoundException();
        var foodCode = request.FoodCode.Trim();
        await ValidateAsync(foodId, foodCode, request, cancellationToken);

        food.FoodCode = foodCode;
        dbContext.SimpleFoodTranslations.RemoveRange(food.Translations);
        dbContext.FoodNutrients.RemoveRange(
            dbContext.FoodNutrients.Where(item => item.FoodId == foodId));
        food.Translations = [];
        Apply(food, request);
        await dbContext.SaveChangesAsync(cancellationToken);
        return await GetAsync(foodId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task DeleteAsync(long foodId, CancellationToken cancellationToken = default)
    {
        var food = await dbContext.SimpleFoods
            .FirstOrDefaultAsync(item => item.FoodId == foodId, cancellationToken)
            ?? throw new KeyNotFoundException();
        var isInUse = await dbContext.DailyRecords
            .AnyAsync(record => record.FoodId == foodId, cancellationToken);
        if (isInUse)
        {
            throw new InvalidOperationException(FoodMaintenanceErrorCodes.InUse);
        }

        dbContext.SimpleFoods.Remove(food);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task ValidateAsync(
        long? foodId,
        string foodCode,
        UpsertFoodRequest request,
        CancellationToken cancellationToken)
    {
        if (await dbContext.SimpleFoods.AnyAsync(
            food => food.FoodCode == foodCode && food.FoodId != foodId,
            cancellationToken))
        {
            throw new FoodMaintenanceValidationException(
                nameof(request.FoodCode),
                FoodMaintenanceErrorCodes.DuplicateFoodCode);
        }

        var requestedCodes = request.Nutrients
            .Select(item => item.NutrientCode.Trim())
            .ToArray();
        var existingCodes = await dbContext.Nutrients
            .Where(item => requestedCodes.Contains(item.NutrientCode))
            .Select(item => item.NutrientCode)
            .ToListAsync(cancellationToken);
        if (requestedCodes.Except(existingCodes).Any())
        {
            throw new FoodMaintenanceValidationException(
                nameof(request.Nutrients),
                FoodMaintenanceErrorCodes.NutrientNotFound);
        }
    }

    private void Apply(SimpleFood food, UpsertFoodRequest request)
    {
        foreach (var translation in request.Translations)
        {
            food.Translations.Add(new SimpleFoodTranslation
            {
                LangCode = translation.LangCode.Trim(),
                FoodName = translation.DisplayName.Trim(),
                Description = translation.Description?.Trim() ?? string.Empty,
            });
        }

        var nutrients = request.Nutrients.ToDictionary(
            item => item.NutrientCode.Trim(),
            StringComparer.Ordinal);
        foreach (var nutrient in dbContext.Nutrients.Where(
            item => nutrients.Keys.Contains(item.NutrientCode)))
        {
            dbContext.FoodNutrients.Add(new FoodNutrient
            {
                Food = food,
                Nutrient = nutrient,
                Amount = nutrients[nutrient.NutrientCode].AmountPer100Grams,
            });
        }
    }

    private IQueryable<AdminFoodResponse> Query()
    {
        return dbContext.SimpleFoods
            .AsNoTracking()
            .Select(food => new AdminFoodResponse
            {
                FoodId = food.FoodId,
                FoodCode = food.FoodCode,
                Translations = food.Translations
                    .OrderBy(item => item.LangCode)
                    .Select(item => new UpsertFoodTranslationRequest
                    {
                        LangCode = item.LangCode,
                        DisplayName = item.FoodName,
                        Description = item.Description,
                    })
                    .ToArray(),
                Nutrients = dbContext.FoodNutrients
                    .Where(item => item.FoodId == food.FoodId)
                    .OrderBy(item => item.Nutrient.DisplayOrder)
                    .ThenBy(item => item.Nutrient.NutrientCode)
                    .Select(item => new AdminFoodNutrientResponse
                    {
                        NutrientCode = item.Nutrient.NutrientCode,
                        AmountPer100Grams = item.Amount,
                        UnitCode = item.Nutrient.UnitCode,
                        DisplayOrder = item.Nutrient.DisplayOrder,
                    })
                    .ToArray(),
            });
    }
}
