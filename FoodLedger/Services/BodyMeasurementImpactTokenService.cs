using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;

namespace FoodLedger.Services;

/// <summary>使用 ASP.NET Core Data Protection 保護刪除影響內容與到期時間。</summary>
public sealed class BodyMeasurementImpactTokenService : IBodyMeasurementImpactTokenService
{
    private static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(10);
    private readonly IDataProtector _protector;
    private readonly TimeProvider _timeProvider;

    public BodyMeasurementImpactTokenService(
        IDataProtectionProvider dataProtectionProvider,
        TimeProvider timeProvider)
    {
        _protector = dataProtectionProvider.CreateProtector(
            "FoodLedger.BodyMeasurementDeletionImpact.v1");
        _timeProvider = timeProvider;
    }

    /// <inheritdoc />
    public BodyMeasurementImpactToken Create(
        long userId,
        long measurementId,
        Guid version,
        int affectedSnapshotCount,
        bool affectsCurrentTarget)
    {
        var expiresAt = _timeProvider.GetUtcNow().Add(Lifetime);
        var payload = new Payload(
            userId,
            measurementId,
            version,
            affectedSnapshotCount,
            affectsCurrentTarget,
            expiresAt);
        return new BodyMeasurementImpactToken(
            _protector.Protect(JsonSerializer.Serialize(payload)),
            expiresAt);
    }

    /// <inheritdoc />
    public bool IsValid(
        string token,
        long userId,
        long measurementId,
        Guid version,
        int affectedSnapshotCount,
        bool affectsCurrentTarget)
    {
        try
        {
            var payload = JsonSerializer.Deserialize<Payload>(_protector.Unprotect(token));
            return payload is not null
                && payload.UserId == userId
                && payload.MeasurementId == measurementId
                && payload.Version == version
                && payload.AffectedSnapshotCount == affectedSnapshotCount
                && payload.AffectsCurrentTarget == affectsCurrentTarget
                && payload.ExpiresAt >= _timeProvider.GetUtcNow();
        }
        catch (Exception exception) when (
            exception is ArgumentException or CryptographicException or JsonException)
        {
            return false;
        }
    }

    private sealed record Payload(
        long UserId,
        long MeasurementId,
        Guid Version,
        int AffectedSnapshotCount,
        bool AffectsCurrentTarget,
        DateTimeOffset ExpiresAt);
}
