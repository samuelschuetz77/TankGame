using GameLogic;

namespace GameTest;

// Characterization tests: these record what tank movement actually does today,
// so later changes to the controls show up as differences in behavior.
public class TankMovementCharacterizationTests
{
    // Big empty map so walls and edges never affect a single tick
    private static readonly GameMap openMap =
        new("Test", 1000, 1000, [], [new MapSpawnPoint(0, 0, 0)]);

    private static Tank TankInMiddle() => new() { PositionX = 500, PositionY = 500 };

    [Fact]
    public void DrivingRightForOneTick()
    {
        var tank = TankInMiddle() with { MovingRight = true };

        var moved = Tank.ProcessTankMovement(tank, openMap);

        // Characterized: one tick driving right moves 8 pixels
        Assert.Equal(508, moved.PositionX);

        Assert.Equal(0, moved.Angle);
    }

    [Fact]
    public void DrivingLeftForOneTick()
    {
        var tank = TankInMiddle() with { MovingLeft = true };

        var moved = Tank.ProcessTankMovement(tank, openMap);

        // Characterized: one tick driving left moves 
        Assert.Equal(492, moved.PositionX);

        Assert.Equal(180, moved.Angle);
    }


    [Fact]
    public void DrivingUpForOneTick()
    {
        var tank = TankInMiddle() with { MovingUp = true };

        var moved = Tank.ProcessTankMovement(tank, openMap);

        // Characterized: one tick driving up moves 
        Assert.Equal(492, moved.PositionY);

        Assert.Equal(-90, moved.Angle);
    }

    [Fact]
    public void DrivingDownForOneTick()
    {
        var tank = TankInMiddle() with { MovingDown = true };

        var moved = Tank.ProcessTankMovement(tank, openMap);

        // Characterized: one tick driving down moves 8 pixels
        Assert.Equal(508, moved.PositionY);

        Assert.Equal(90, moved.Angle);
    }
}
