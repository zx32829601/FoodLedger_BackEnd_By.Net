using FoodLedger.Services;
using Microsoft.AspNetCore.DataProtection;

namespace FoodLedger.Tests.Services;

/// <summary>驗證刪除影響 token 綁定內容、不可竄改且會逾期。</summary>
[Category("BodyMeasurements")]
[Category("Unit")]
public sealed class BodyMeasurementImpactTokenServiceTests
{
    private static readonly DateTimeOffset FixedUtcNow =
        new(2026, 8, 25, 10, 0, 0, TimeSpan.Zero);

    [Test]
    public void IsValid_WithOriginalBoundValues_ReturnsTrue()
    {
        var timeProvider = new AdjustableTimeProvider(FixedUtcNow);
        var service = CreateService(timeProvider);
        var version = Guid.NewGuid();

        var token = service.Create(42, 7, version, 0, false);

        Assert.Multiple(() =>
        {
            Assert.That(service.IsValid(token.Value, 42, 7, version, 0, false), Is.True);
            Assert.That(token.ExpiresAt, Is.EqualTo(FixedUtcNow.AddMinutes(10)));
        });
    }

    [Test]
    public void IsValid_WithDifferentUserOrTamperedToken_ReturnsFalse()
    {
        var service = CreateService(new AdjustableTimeProvider(FixedUtcNow));
        var version = Guid.NewGuid();
        var token = service.Create(42, 7, version, 0, false);

        Assert.Multiple(() =>
        {
            Assert.That(service.IsValid(token.Value, 99, 7, version, 0, false), Is.False);
            Assert.That(service.IsValid(token.Value + "tampered", 42, 7, version, 0, false),
                Is.False);
            Assert.That(service.IsValid(string.Empty, 42, 7, version, 0, false), Is.False);
        });
    }

    [Test]
    public void IsValid_AfterExpiry_ReturnsFalse()
    {
        var timeProvider = new AdjustableTimeProvider(FixedUtcNow);
        var service = CreateService(timeProvider);
        var version = Guid.NewGuid();
        var token = service.Create(42, 7, version, 0, false);
        timeProvider.UtcNow = FixedUtcNow.AddMinutes(10).AddTicks(1);

        Assert.That(service.IsValid(token.Value, 42, 7, version, 0, false), Is.False);
    }

    private static BodyMeasurementImpactTokenService CreateService(TimeProvider timeProvider) =>
        new(new EphemeralDataProtectionProvider(), timeProvider);

    private sealed class AdjustableTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public DateTimeOffset UtcNow { get; set; } = utcNow;
        public override DateTimeOffset GetUtcNow() => UtcNow;
    }
}
