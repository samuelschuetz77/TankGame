namespace GameLogic;

// Match rules the creator picks; they apply to every player in the match
public record MatchSettings
{
    public const int DefaultHealth = 3;
    public const int MinReloadTicks = 2;
    public const int MaxReloadTicks = 30;
    public const int MaxBouncesAllowed = 5;
    public const int MinHealth = 1;
    public const int MaxHealth = 10;
    public static readonly double[] SpeedChoices = [0.5, 0.75, 1, 1.25, 1.5, 2];
    // 0 = no time limit
    public static readonly int[] TimeLimitChoices = [0, 1, 2, 3, 5, 10];

    // Game ticks between shots (10 ticks = 1 second)
    public int ReloadTicks { get; init; } = 10;
    public int MaxBounces { get; init; } = 1;
    public int Health { get; init; } = DefaultHealth;
    public double SpeedMultiplier { get; init; } = 1;
    public int TimeLimitMinutes { get; init; } = 0;

    // Clients can send anything; snap every value to an allowed choice
    public static MatchSettings Sanitize(MatchSettings incoming) => new()
    {
        ReloadTicks = Math.Clamp(incoming.ReloadTicks, MinReloadTicks, MaxReloadTicks),
        MaxBounces = Math.Clamp(incoming.MaxBounces, 0, MaxBouncesAllowed),
        Health = Math.Clamp(incoming.Health, MinHealth, MaxHealth),
        SpeedMultiplier = double.IsFinite(incoming.SpeedMultiplier)
            ? SpeedChoices.MinBy(choice => Math.Abs(choice - incoming.SpeedMultiplier))
            : 1,
        // long math: int.MinValue would overflow Math.Abs
        TimeLimitMinutes = TimeLimitChoices.MinBy(choice => Math.Abs((long)choice - incoming.TimeLimitMinutes)),
    };

    // Health and the time limit can't change once the match is running
    public MatchSettings WithLockedFrom(MatchSettings current) =>
        this with { Health = current.Health, TimeLimitMinutes = current.TimeLimitMinutes };

    // Tank speed scales top speed and acceleration together so handling feels the same; turning is unchanged
    public DeveloperGameSettings ScaleMovement(DeveloperGameSettings baseSettings) => baseSettings with
    {
        MaxSpeed = Math.Max(1, (int)Math.Round(baseSettings.MaxSpeed * SpeedMultiplier)),
        ForwardAcceleration = Math.Max(1, (int)Math.Round(baseSettings.ForwardAcceleration * SpeedMultiplier)),
        BrakeAcceleration = (int)Math.Round(baseSettings.BrakeAcceleration * SpeedMultiplier),
    };
}
