using GameLogic;
using GameLogic.Game;

namespace GameTest;

public class TimeLimitTests
{
    [Fact]
    public void TimeOutMostHealthWins()
    {
        var healthy = new Tank { Health = 3 };
        var hurt = new Tank { Health = 1 };

        Assert.Equal(new MatchResult(true, healthy.Id), Combat.DecideResult([hurt, healthy], 0));
    }

    [Fact]
    public void TimeOutHealthTieGoesToMostHitsLanded()
    {
        var sharpshooter = new Tank { Health = 2, HitsLanded = 1 };
        var other = new Tank { Health = 2, HitsLanded = 0 };

        Assert.Equal(new MatchResult(true, sharpshooter.Id), Combat.DecideResult([other, sharpshooter], 0));
    }

    [Fact]
    public void TimeOutFullTieIsADraw()
    {
        Assert.Equal(new MatchResult(true, null),
            Combat.DecideResult([new Tank { Health = 2, HitsLanded = 1 }, new Tank { Health = 2, HitsLanded = 1 }], 0));
    }

    [Fact]
    public void TimeLeftOrNoTimeLimitKeepsPlaying()
    {
        Assert.Equal(MatchResult.Ongoing, Combat.DecideResult([new Tank(), new Tank()], 5));
        Assert.Equal(MatchResult.Ongoing, Combat.DecideResult([new Tank(), new Tank()], null));
    }

    [Fact]
    public async Task TimerStartsWhenTheSecondPlayerJoins()
    {
        var game = TestGames.NewGame(new MatchSettings { TimeLimitMinutes = 1 });
        game.JoinGame();
        for (var tick = 0; tick < 5; tick++)
            await game.loopRunner.ProcessGameTick();
        Assert.Null(game.GetGameState().SecondsLeft);

        game.JoinGame();
        Assert.Equal(60, game.GetGameState().SecondsLeft);

        for (var tick = 0; tick < 10; tick++)
            await game.loopRunner.ProcessGameTick();
        Assert.Equal(59, game.GetGameState().SecondsLeft);
    }

    [Fact]
    public async Task MatchEndsWhenTimeRunsOut()
    {
        var game = TestGames.NewGame(new MatchSettings { TimeLimitMinutes = 1 });
        game.JoinGame();
        game.JoinGame();

        for (var tick = 0; tick < 599; tick++)
            await game.loopRunner.ProcessGameTick();
        Assert.Equal(GameStatus.Playing, game.Status);

        await game.loopRunner.ProcessGameTick();
        Assert.Equal(GameStatus.Ended, game.Status);
        // Nobody was hit: equal health and hits landed
        Assert.Null(game.WinnerId);
    }

    [Fact]
    public void NoTimeLimitMeansNoTimer()
    {
        var game = TestGames.NewGame();
        game.JoinGame();
        game.JoinGame();

        Assert.Null(game.GetGameState().SecondsLeft);
    }
}
