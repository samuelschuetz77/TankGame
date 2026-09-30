using GameLogic;

namespace GameTest;

public class CombatTests
{
    // Old rule these tests were written for: the first death is permanent
    private static readonly MatchSettings OneLife = new() { Lives = 1 };

    private static readonly DeveloperGameSettings settings = new();

    private static Tank TankAt(int x, int health = 3) => new() { PositionX = x, PositionY = 200, Health = health };

    // A bullet sitting right on the tank's center
    private static Bullet BulletOn(Tank target, Guid ownerId)
    {
        var (centerX, centerY) = Tank.GetCenter(target, settings);
        return new Bullet
        {
            PositionX = centerX - Bullet.BulletSize / 2,
            PositionY = centerY - Bullet.BulletSize / 2,
            OwnerId = ownerId,
        };
    }

    [Fact]
    public void HitTakesOneHealthAndRemovesTheBullet()
    {
        var shooter = TankAt(100);
        var target = TankAt(400);

        var (tanks, bullets) = Combat.ResolveHits([shooter, target], [BulletOn(target, shooter.Id)], settings, OneLife);

        var hit = tanks.Single(t => t.Id == target.Id);
        Assert.Equal(2, hit.Health);
        Assert.False(hit.Eliminated);
        Assert.Empty(bullets);
    }

    [Fact]
    public void HitCountsForTheShooter()
    {
        var shooter = TankAt(100);
        var target = TankAt(400);

        var (tanks, _) = Combat.ResolveHits([shooter, target], [BulletOn(target, shooter.Id)], settings, OneLife);

        Assert.Equal(1, tanks.Single(t => t.Id == shooter.Id).HitsLanded);
        Assert.Equal(0, tanks.Single(t => t.Id == target.Id).HitsLanded);
    }

    [Fact]
    public void SelfHitCostsHealthButIsNotAHitLanded()
    {
        var tank = TankAt(100);

        var (tanks, _) = Combat.ResolveHits([tank], [BulletOn(tank, tank.Id)], settings, OneLife);

        Assert.Equal(2, tanks.Single().Health);
        Assert.Equal(0, tanks.Single().HitsLanded);
    }

    [Fact]
    public void LosingLastHealthEliminates()
    {
        var shooter = TankAt(100);
        var target = TankAt(400, health: 1);

        var (tanks, _) = Combat.ResolveHits([shooter, target], [BulletOn(target, shooter.Id)], settings, OneLife);

        var hit = tanks.Single(t => t.Id == target.Id);
        Assert.Equal(0, hit.Health);
        Assert.True(hit.Eliminated);
    }

    [Fact]
    public void EliminatedTankCannotBeHit()
    {
        var shooter = TankAt(100);
        var target = TankAt(400, health: 0) with { Eliminated = true };

        var (tanks, bullets) = Combat.ResolveHits([shooter, target], [BulletOn(target, shooter.Id)], settings, OneLife);

        Assert.Equal(0, tanks.Single(t => t.Id == target.Id).Health);
        Assert.Single(bullets);
    }

    [Fact]
    public void BulletHitsOnlyTheFirstOverlappingTank()
    {
        var shooter = TankAt(100);
        var first = TankAt(400);
        var second = TankAt(400);

        var (tanks, _) = Combat.ResolveHits([shooter, first, second], [BulletOn(first, shooter.Id)], settings, OneLife);

        Assert.Equal(2, tanks.Single(t => t.Id == first.Id).Health);
        Assert.Equal(3, tanks.Single(t => t.Id == second.Id).Health);
    }

    [Fact]
    public void TwoHitsOnLastHealthStopAtZeroAndTheSecondBulletFliesOn()
    {
        var shooter = TankAt(100);
        var target = TankAt(400, health: 1);

        var (tanks, bullets) = Combat.ResolveHits([shooter, target],
            [BulletOn(target, shooter.Id), BulletOn(target, shooter.Id)], settings, OneLife);

        var hit = tanks.Single(t => t.Id == target.Id);
        Assert.Equal(0, hit.Health);
        Assert.True(hit.Eliminated);
        Assert.Single(bullets);
        Assert.Equal(1, tanks.Single(t => t.Id == shooter.Id).HitsLanded);
    }

    [Fact]
    public void MissChangesNothing()
    {
        var shooter = TankAt(100);
        var target = TankAt(400);
        var miss = new Bullet { PositionX = 700, PositionY = 20, OwnerId = shooter.Id };

        var (tanks, bullets) = Combat.ResolveHits([shooter, target], [miss], settings, OneLife);

        Assert.All(tanks, t => Assert.Equal(3, t.Health));
        Assert.Single(bullets);
    }

    [Fact]
    public void FiredBulletRemembersItsShooter()
    {
        var tank = TankAt(100);

        Assert.Equal(tank.Id, Tank.FireBullet(tank, settings).OwnerId);
    }
}
