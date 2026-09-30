using GameLogic;

namespace GameTest;

public class InMatchSettingsTests
{
    [Fact]
    public void CreatorCanChangeSpeedReloadAndBounces()
    {
        var game = TestGames.NewGame();
        var creator = game.JoinGame();
        game.JoinGame();

        game.UpdateMatchSettings(creator, game.Settings with { SpeedMultiplier = 2, ReloadMs = 250, MaxBounces = 3 });

        Assert.Equal(2, game.Settings.SpeedMultiplier);
        Assert.Equal(250, game.Settings.ReloadMs);
        Assert.Equal(3, game.Settings.MaxBounces);
    }

    [Fact]
    public void OtherPlayersCannotChangeSettings()
    {
        var game = TestGames.NewGame();
        game.JoinGame();
        var other = game.JoinGame();
        var before = game.Settings;

        game.UpdateMatchSettings(other, before with { SpeedMultiplier = 2 });

        Assert.Equal(before, game.Settings);
    }

    [Fact]
    public void HealthAndTimeLimitStayLocked()
    {
        var game = TestGames.NewGame(new MatchSettings { Health = 3, TimeLimitMinutes = 0 });
        var creator = game.JoinGame();

        game.UpdateMatchSettings(creator, game.Settings with { Health = 9, TimeLimitMinutes = 5, SpeedMultiplier = 1.5 });

        Assert.Equal(3, game.Settings.Health);
        Assert.Equal(0, game.Settings.TimeLimitMinutes);
        Assert.Equal(1.5, game.Settings.SpeedMultiplier);
    }

    [Fact]
    public void ChangesAreSanitized()
    {
        var game = TestGames.NewGame();
        var creator = game.JoinGame();

        game.UpdateMatchSettings(creator, game.Settings with { SpeedMultiplier = 7, ReloadMs = -4 });

        Assert.Equal(2, game.Settings.SpeedMultiplier);
        Assert.Equal(MatchSettings.MinReloadMs, game.Settings.ReloadMs);
    }

    [Fact]
    public void BulletsInFlightKeepTheirBounces()
    {
        var game = TestGames.NewGame(new MatchSettings { MaxBounces = 4 });
        var creator = game.JoinGame();
        game.ReceiveUserInput(TestGames.Input(creator, shoot: true));

        game.UpdateMatchSettings(creator, game.Settings with { MaxBounces = 0 });

        Assert.Equal(4, game.Bullets.Single().BouncesLeft);
        Assert.Equal(0, game.Settings.MaxBounces);
    }
}
