using GameLogic;
using GameLogic.Game;

namespace GameTest;

public class MatchEndTests
{
    [Fact]
    public void OnePlayerNeverEndsTheMatch()
    {
        Assert.Equal(MatchResult.Ongoing, Combat.DecideResult([new Tank()]));
        Assert.Equal(MatchResult.Ongoing, Combat.DecideResult([new Tank { Eliminated = true }]));
    }

    [Fact]
    public void LastTankStandingWins()
    {
        var winner = new Tank();
        var loser = new Tank { Eliminated = true };

        Assert.Equal(new MatchResult(true, winner.Id), Combat.DecideResult([winner, loser]));
    }

    [Fact]
    public void EveryoneEliminatedIsADraw()
    {
        Assert.Equal(new MatchResult(true, null),
            Combat.DecideResult([new Tank { Eliminated = true }, new Tank { Eliminated = true }]));
    }

    [Fact]
    public void TwoTanksLeftKeepPlaying()
    {
        Assert.Equal(MatchResult.Ongoing,
            Combat.DecideResult([new Tank(), new Tank(), new Tank { Eliminated = true }]));
    }

    private static async Task<(Game Game, Guid A, Guid B)> FinishedDuel()
    {
        var game = TestGames.NewGame(new MatchSettings { Health = 1 });
        var a = game.JoinGame();
        var b = game.JoinGame();
        TestGames.ShootAt(game, a, b);
        await TestGames.TickUntil(game, () => game.Status == GameStatus.Ended);
        return (game, a, b);
    }

    [Fact]
    public async Task DuelEndsWithTheShooterWinning()
    {
        var (game, a, b) = await FinishedDuel();

        var state = game.GetGameState();
        Assert.Equal(GameStatus.Ended, state.Status);
        Assert.Equal(a, state.WinnerId);
        var loser = state.Tanks!.Single(t => t.Id == b);
        Assert.True(loser.Eliminated);
        Assert.Equal(0, loser.Health);
        Assert.Equal(1, state.Tanks!.Single(t => t.Id == a).HitsLanded);
    }

    [Fact]
    public async Task CannotJoinAnEndedMatch()
    {
        var (game, _, _) = await FinishedDuel();

        Assert.Throws<InvalidOperationException>(() => game.JoinGame());
    }

    [Fact]
    public async Task InputAfterTheMatchEndsIsIgnored()
    {
        var (game, a, b) = await FinishedDuel();
        var bulletsBefore = game.Bullets.Count();

        TestGames.ShootAt(game, a, b);

        Assert.Equal(bulletsBefore, game.Bullets.Count());
    }

    [Fact]
    public async Task EliminatedTankCannotShootOrMove()
    {
        var game = TestGames.NewGame(new MatchSettings { Health = 1 });
        var a = game.JoinGame();
        var b = game.JoinGame();
        var c = game.JoinGame();
        TestGames.ShootAt(game, a, b);
        await TestGames.TickUntil(game, () => game.Tanks.Single(t => t.Id == b).Eliminated);
        Assert.Equal(GameStatus.Playing, game.Status);
        var before = game.Tanks.Single(t => t.Id == b);

        TestGames.ShootAt(game, b, c);
        game.ReceiveUserInput(TestGames.Input(b) with { Right = true });
        await game.loopRunner.ProcessGameTick();

        Assert.DoesNotContain(game.Bullets, bullet => bullet.OwnerId == b);
        var after = game.Tanks.Single(t => t.Id == b);
        Assert.Equal(before.PositionX, after.PositionX);
        Assert.Equal(before.PositionY, after.PositionY);
    }

    [Fact]
    public void NewTanksStartWithTheMatchHealth()
    {
        var game = TestGames.NewGame(new MatchSettings { Health = 7 });

        var id = game.JoinGame();

        Assert.Equal(7, game.Tanks.Single(t => t.Id == id).Health);
    }
}
