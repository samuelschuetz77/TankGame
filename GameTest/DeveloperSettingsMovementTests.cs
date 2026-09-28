using GameLogic;

namespace GameTest;

// Each developer setting has to change how the tank drives under the 8-direction controls
public class DeveloperSettingsMovementTests
{
    private static readonly GameMap openMap =
        new("Test", 1000, 1000, [], [new MapSpawnPoint(0, 0, 0)]);

    private static Tank TankInMiddle() => new() { PositionX = 500, PositionY = 500, Angle = 0 };

    private static Tank Drive(Tank tank, DeveloperGameSettings settings, int ticks)
    {
        for (var i = 0; i < ticks; i++)
            tank = Tank.ProcessTankMovement(tank, openMap, settings);
        return tank;
    }

    [Fact]
    public void MaxSpeedCapsSpeedAndRaisingItSpeedsTankUp()
    {
        var driving = TankInMiddle() with { MovingRight = true };

        var slow = Drive(driving, new() { ForwardAcceleration = 30, MaxSpeed = 3 }, 1);
        var fast = Drive(driving, new() { ForwardAcceleration = 30, MaxSpeed = 20 }, 1);

        Assert.Equal(3, slow.Speed);
        Assert.Equal(503, slow.PositionX);
        Assert.Equal(20, fast.Speed);
        Assert.Equal(520, fast.PositionX);
    }

    [Fact]
    public void AccelerationRampsSpeedUpOverSeveralTicks()
    {
        var driving = TankInMiddle() with { MovingRight = true };

        var moved = Drive(driving, new() { ForwardAcceleration = 2, MaxSpeed = 100 }, 3);

        Assert.Equal(6, moved.Speed);
    }

    [Fact]
    public void BrakeSlowsTheTankGraduallyWhenNoKeyIsHeld()
    {
        var coasting = TankInMiddle() with { Speed = 10 };

        var gentle = Drive(coasting, new() { BrakeAcceleration = -3 }, 1);
        var hard = Drive(coasting, new() { BrakeAcceleration = -30 }, 1);

        Assert.Equal(7, gentle.Speed);
        Assert.Equal(507, gentle.PositionX);
        Assert.Equal(0, hard.Speed);
        Assert.Equal(500, hard.PositionX);
    }

    [Fact]
    public void TurnDegreesLimitsHowFastTheHullSwings()
    {
        var driving = TankInMiddle() with { MovingDown = true };

        var oneTick = Drive(driving, new() { TurnDegrees = 30 }, 1);
        var threeTicks = Drive(driving, new() { TurnDegrees = 30 }, 3);

        Assert.Equal(30, oneTick.Angle);
        Assert.Equal(90, threeTicks.Angle);
    }

    [Fact]
    public void FullTurnDegreesSnapsInstantlyLikeTheDefault()
    {
        var driving = TankInMiddle() with { MovingLeft = true };

        Assert.Equal(180, Drive(driving, new() { TurnDegrees = 180 }, 1).Angle);
        Assert.Equal(180, Drive(driving, new(), 1).Angle);
    }

    [Fact]
    public void SlowTurningTankBacksUpAtTheReverseMultiplierInsteadOfSwingingAround()
    {
        // Facing right, driver presses left: 180 degrees away
        var driving = TankInMiddle() with { MovingLeft = true };

        var normal = Drive(driving, new() { TurnDegrees = 20, BackwardSpeedMultiplier = 1.0 }, 1);
        var slowReverse = Drive(driving, new() { TurnDegrees = 20, BackwardSpeedMultiplier = 0.5 }, 1);

        Assert.True(normal.Reversing);
        Assert.Equal(0, normal.Angle);
        Assert.Equal(492, normal.PositionX);
        Assert.Equal(496, slowReverse.PositionX);
    }

    [Fact]
    public void BackwardMultiplierDoesNotApplyWhenDrivingForward()
    {
        var driving = TankInMiddle() with { MovingRight = true };

        var moved = Drive(driving, new() { TurnDegrees = 20, BackwardSpeedMultiplier = 0.1 }, 1);

        Assert.False(moved.Reversing);
        Assert.Equal(508, moved.PositionX);
    }
}
