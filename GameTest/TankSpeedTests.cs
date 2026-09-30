using GameLogic;

namespace GameTest;

public class TankSpeedTests
{
    [Fact]
    public void DoubleSpeedScalesTopSpeedAndAccelerationButNotTurning()
    {
        var baseSettings = new DeveloperGameSettings();

        var scaled = new MatchSettings { SpeedMultiplier = 2 }.ScaleMovement(baseSettings);

        Assert.Equal(16, scaled.MaxSpeed);
        Assert.Equal(16, scaled.ForwardAcceleration);
        Assert.Equal(-32, scaled.BrakeAcceleration);
        Assert.Equal(baseSettings.TurnDegrees, scaled.TurnDegrees);
        Assert.Equal(baseSettings.BackwardSpeedMultiplier, scaled.BackwardSpeedMultiplier);
        Assert.Equal(baseSettings.HitboxInset, scaled.HitboxInset);
    }

    [Fact]
    public void SlowSpeedNeverDropsBelowOne()
    {
        var scaled = new MatchSettings { SpeedMultiplier = 0.5 }
            .ScaleMovement(new DeveloperGameSettings { MaxSpeed = 1, ForwardAcceleration = 1 });

        Assert.Equal(1, scaled.MaxSpeed);
        Assert.Equal(1, scaled.ForwardAcceleration);
    }

    [Theory]
    [InlineData(1, 108)]
    [InlineData(2, 116)]
    public async Task MatchSpeedChangesHowFarTanksDrive(double multiplier, int expectedX)
    {
        var game = TestGames.NewGame(new MatchSettings { SpeedMultiplier = multiplier });
        var a = game.JoinGame();

        game.ReceiveUserInput(TestGames.Input(a) with { Right = true });
        await game.loopRunner.ProcessGameTick();

        Assert.Equal(expectedX, game.Tanks.Single().PositionX);
        // The developer base values are never overwritten
        Assert.Equal(new DeveloperGameSettings().MaxSpeed, game.DeveloperSettings.MaxSpeed);
    }
}
