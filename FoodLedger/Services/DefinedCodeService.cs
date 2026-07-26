using FoodLedger.Data.Entities;
using FoodLedger.DTOs.DefinedCodes;
using Microsoft.EntityFrameworkCore;

namespace FoodLedger.Services;

/// <summary>
/// 使用資料庫通用代碼提供唯讀選項。
/// </summary>
public sealed class DefinedCodeService(ApplicationDbContext dbContext) : IDefinedCodeService
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<DefinedCodeResponse>> GetActiveMealTypesAsync(
        CancellationToken cancellationToken = default)
    {
        return await dbContext.DefinedCodes
            .AsNoTracking()
            .Where(code => code.CodeType == DefinedCodeTypes.MealType && code.IsActive)
            .OrderBy(code => code.SortOrder)
            .ThenBy(code => code.Code)
            .Select(code => new DefinedCodeResponse
            {
                Code = code.Code,
                DisplayName = code.DisplayName,
                SortOrder = code.SortOrder,
            })
            .ToListAsync(cancellationToken);
    }
}
