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
        IEnumerable<Tank> tanks, IEnumerable<Bullet> bullets, DeveloperGameSettings settings)
    {
        var tankList = tanks.ToList();
        var flying = new List<Bullet>();

        foreach (var bullet in bullets)
        {
            var bulletArea = new RectangleArea(bullet.PositionX, bullet.PositionY, Bullet.BulletSize, Bullet.BulletSize);
            var targetIndex = tankList.FindIndex(tank =>
                !tank.Eliminated && Tank.GetCollisionArea(tank, settings).Intersects(bulletArea));
            if (targetIndex < 0)
            {
                flying.Add(bullet);
                continue;
            }

            var target = tankList[targetIndex];
            var health = Math.Max(0, target.Health - 1);
            tankList[targetIndex] = target with { Health = health, Eliminated = health == 0 };

            // Shooting yourself with a bounce doesn't count as a hit landed
            var shooterIndex = tankList.FindIndex(tank => tank.Id == bullet.OwnerId);
            if (shooterIndex >= 0 && bullet.OwnerId != target.Id)
                tankList[shooterIndex] = tankList[shooterIndex] with { HitsLanded = tankList[shooterIndex].HitsLanded + 1 };
        }

        return (tankList.ToArray(), flying.ToArray());
    }

    // A match needs 2 players before it can end, or the creator would win alone
    public static MatchResult DecideResult(IReadOnlyCollection<Tank> tanks)
    {
        if (tanks.Count < 2)
            return MatchResult.Ongoing;

        var alive = tanks.Where(tank => !tank.Eliminated).ToList();
        if (alive.Count == 0)
            return new MatchResult(true, null);
        if (alive.Count == 1)
            return new MatchResult(true, alive[0].Id);
        return MatchResult.Ongoing;
    }
}
