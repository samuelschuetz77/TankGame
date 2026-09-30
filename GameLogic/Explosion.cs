namespace GameLogic;

// A short-lived blast where an instant shot landed; clients animate it from its age
public record Explosion(Guid Id, int X, int Y, int TicksLeft)
{
    // 6 ticks = 0.6 s at 10 ticks per second
    public const int Ticks = 6;

    public static Explosion At(int x, int y) => new(Guid.NewGuid(), x, y, Ticks);
}

public record ExplosionState
{
    public Guid Id { get; init; }
    public int X { get; init; }
    public int Y { get; init; }
    // Ticks since it went off: 0 at the start, up to Explosion.Ticks - 1
    public int Age { get; init; }
}

// Where an instant shot landed, and which tank (index in the tank list) it hit, if any
public record InstantShot(int X, int Y, int? HitIndex);
