using GameLogic;

namespace GameTest;

public class MatchSettingsTests
{
    [Fact]
    public void DefaultsMatchTheSpec()
    {
        var settings = new MatchSettings();

        Assert.Equal(10, settings.ReloadTicks);
        Assert.Equal(1, settings.MaxBounces);
        Assert.Equal(3, settings.Health);
        Assert.Equal(1, settings.SpeedMultiplier);
        Assert.Equal(0, settings.TimeLimitMinutes);
    }

    [Fact]
    public void SanitizeKeepsAllowedValues()
    {
        var settings = new MatchSettings
        {
            ReloadTicks = 25, MaxBounces = 5, Health = 10, SpeedMultiplier = 1.25, TimeLimitMinutes = 5
        };

        Assert.Equal(settings, MatchSettings.Sanitize(settings));
    }

    [Theory]
    [InlineData(0, -1, 0, 2, 0, 1)]
    [InlineData(999, 99, 99, 30, 5, 10)]
    [InlineData(int.MinValue, int.MinValue, int.MinValue, 2, 0, 1)]
    [InlineData(int.MaxValue, int.MaxValue, int.MaxValue, 30, 5, 10)]
    public void SanitizeClampsNumbersIntoRange(int reload, int bounces, int health,
        int expectedReload, int expectedBounces, int expectedHealth)
    {
        var sanitized = MatchSettings.Sanitize(new MatchSettings
        {
            ReloadTicks = reload, MaxBounces = bounces, Health = health
        });

        Assert.Equal(expectedReload, sanitized.ReloadTicks);
        Assert.Equal(expectedBounces, sanitized.MaxBounces);
        Assert.Equal(expectedHealth, sanitized.Health);
    }

    [Theory]
    [InlineData(0.1, 0.5)]
    [InlineData(0.8, 0.75)]
    [InlineData(1.9, 2)]
    [InlineData(100, 2)]
    [InlineData(-3, 0.5)]
    [InlineData(double.NaN, 1)]
    [InlineData(double.PositiveInfinity, 1)]
    public void SanitizeSnapsSpeedToNearestChoice(double speed, double expected)
    {
        var sanitized = MatchSettings.Sanitize(new MatchSettings { SpeedMultiplier = speed });

        Assert.Equal(expected, sanitized.SpeedMultiplier);
    }

    [Theory]
    [InlineData(7, 5)]
    [InlineData(-5, 0)]
    [InlineData(60, 10)]
    [InlineData(int.MaxValue, 10)]
    [InlineData(int.MinValue, 0)]
    public void SanitizeSnapsTimeLimitToNearestChoice(int minutes, int expected)
    {
        var sanitized = MatchSettings.Sanitize(new MatchSettings { TimeLimitMinutes = minutes });

        Assert.Equal(expected, sanitized.TimeLimitMinutes);
    }

    [Fact]
    public void WithLockedFromKeepsHealthAndTimeLimit()
    {
        var current = new MatchSettings { Health = 5, TimeLimitMinutes = 3 };
        var incoming = new MatchSettings { Health = 9, TimeLimitMinutes = 10, SpeedMultiplier = 2, ReloadTicks = 4, MaxBounces = 0 };

        var locked = incoming.WithLockedFrom(current);

        Assert.Equal(5, locked.Health);
        Assert.Equal(3, locked.TimeLimitMinutes);
        Assert.Equal(2, locked.SpeedMultiplier);
        Assert.Equal(4, locked.ReloadTicks);
        Assert.Equal(0, locked.MaxBounces);
    }
}
