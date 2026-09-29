namespace GameLogic;

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
}
