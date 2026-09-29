namespace GameLogic;

// WinnerId is null when an ended match is a draw
public record MatchResult(bool Ended, Guid? WinnerId)
{
    public static readonly MatchResult Ongoing = new(false, null);
}

public static class Combat
{
    // Each bullet hits at most one tank (the first it overlaps) and is used up;
    // eliminated tanks can't be hit, so later bullets fly on
    public static (Tank[] Tanks, Bullet[] Bullets) ResolveHits(
        IEnumerable<Tank> tanks, IEnumerable<Bullet> bullets, DeveloperGameSettings settings, MatchSettings? match = null)
    {
        match ??= new MatchSettings();
        var tankList = tanks.ToList();
        var flying = new List<Bullet>();

        foreach (var bullet in bullets)
        {
            var bulletArea = new RectangleArea(bullet.PositionX, bullet.PositionY, Bullet.BulletSize, Bullet.BulletSize);
            var targetIndex = tankList.FindIndex(tank =>
                !tank.Eliminated && !tank.Respawning && Tank.GetCollisionArea(tank, settings).Intersects(bulletArea));
            if (targetIndex < 0)
            {
                flying.Add(bullet);
                continue;
            }

            var target = tankList[targetIndex];
            var health = Math.Max(0, target.Health - 1);
            tankList[targetIndex] = health > 0 ? target with { Health = health } : Destroy(target, match);

            // Shooting yourself with a bounce doesn't count as a hit landed
            var shooterIndex = tankList.FindIndex(tank => tank.Id == bullet.OwnerId);
            if (shooterIndex >= 0 && bullet.OwnerId != target.Id)
                tankList[shooterIndex] = tankList[shooterIndex] with { HitsLanded = tankList[shooterIndex].HitsLanded + 1 };
        }

        return (tankList.ToArray(), flying.ToArray());
    }

    // Health hit 0: that's a death. The last life is permanent; otherwise the tank waits to respawn
    private static Tank Destroy(Tank tank, MatchSettings match)
    {
        var deaths = tank.Deaths + 1;
        var outForGood = deaths >= match.Lives;
        return tank with
        {
            Health = 0,
            Deaths = deaths,
            Eliminated = outForGood,
            RespawnTicksLeft = outForGood ? 0 : match.RespawnSeconds * Game.GameLoopRunner.TicksPerSecond,
            Speed = 0,
            MovingUp = false, MovingDown = false, MovingLeft = false, MovingRight = false,
            Shooting = false,
        };
    }

    // Counts down destroyed tanks; at zero they return with full health at a random spawn point
    // (random on purpose: it can be the spot they just died near, or the same as last time)
    public static Tank[] TickRespawns(IEnumerable<Tank> tanks, GameMap map, MatchSettings match, Random rng) =>
        tanks.Select(tank =>
        {
            if (!tank.Respawning)
                return tank;
            if (tank.RespawnTicksLeft > 1)
                return tank with { RespawnTicksLeft = tank.RespawnTicksLeft - 1 };

            var spawn = map.SpawnPoints[rng.Next(map.SpawnPoints.Count)];
            return tank with
            {
                PositionX = spawn.X,
                PositionY = spawn.Y,
                Angle = spawn.Angle,
                TurretAngle = spawn.Angle,
                Health = match.Health,
                RespawnTicksLeft = 0,
                ReloadTicksLeft = 0,
            };
        }).ToArray();

    // A match needs 2 players before it can end, or the creator would win alone.
    // ticksLeft is null when there's no time limit (or it hasn't started)
    public static MatchResult DecideResult(IReadOnlyCollection<Tank> tanks, int? ticksLeft)
    {
        if (tanks.Count < 2)
            return MatchResult.Ongoing;

        var alive = tanks.Where(tank => !tank.Eliminated).ToList();
        if (alive.Count == 0)
            return new MatchResult(true, null);
        if (alive.Count == 1)
            return new MatchResult(true, alive[0].Id);
        if (ticksLeft is <= 0)
            return ByHealthThenHits(alive);
        return MatchResult.Ongoing;
    }

    // Time ran out: fewest deaths wins, then most health, then most hits landed; a full tie is a draw
    private static MatchResult ByHealthThenHits(List<Tank> alive)
    {
        var ranked = alive.OrderBy(tank => tank.Deaths).ThenByDescending(tank => tank.Health).ThenByDescending(tank => tank.HitsLanded).ToList();
        var (first, second) = (ranked[0], ranked[1]);
        var tied = first.Deaths == second.Deaths && first.Health == second.Health && first.HitsLanded == second.HitsLanded;
        return new MatchResult(true, tied ? null : first.Id);
    }
}
