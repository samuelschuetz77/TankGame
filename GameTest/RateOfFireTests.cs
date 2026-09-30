using GameLogic;
using GameLogic.Game;

namespace GameTest;

public class RateOfFireTests
{
    private static void Press(Game game, Guid id)
    {
        game.ReceiveUserInput(TestGames.Input(id, shoot: false));
        game.ReceiveUserInput(TestGames.Input(id, shoot: true));
    }

    [Fact]
    public void PressDuringReloadIsIgnored()
    {
        var clock = new FakeClock();
        var game = TestGames.NewGame(new MatchSettings { ReloadMs = 300 }, clock);
        var a = game.JoinGame();

        Press(game, a);
        Press(game, a);
        clock.Advance(200);
        Press(game, a);

        Assert.Single(game.Bullets, bullet => bullet.OwnerId == a);
    }

    [Fact]
    public void PressAfterReloadFires()
    {
        var clock = new FakeClock();
        var game = TestGames.NewGame(new MatchSettings { ReloadMs = 300 }, clock);
        var a = game.JoinGame();

        Press(game, a);
        clock.Advance(300);
        Press(game, a);

        Assert.Equal(2, game.Bullets.Count(bullet => bullet.OwnerId == a));
    }

    [Fact]
    public void ReloadIsPreciseToTheMillisecond()
    {
        var clock = new FakeClock();
        var game = TestGames.NewGame(new MatchSettings { ReloadMs = 250 }, clock);
        var a = game.JoinGame();

        Press(game, a);
        clock.Advance(249);
        Press(game, a);
        Assert.Single(game.Bullets);

        clock.Advance(1);
        Press(game, a);
        Assert.Equal(2, game.Bullets.Count());
    }

    [Fact]
    public void ReloadAppliesToInstantShotsToo()
    {
        var clock = new FakeClock();
        var game = TestGames.NewGame(new MatchSettings { Projectile = ProjectileType.Realistic, ReloadMs = 500 }, clock);
        var a = game.JoinGame();
        game.JoinGame();

        Press(game, a);
        Press(game, a);
        Assert.Single(game.Explosions);

        clock.Advance(500);
        Press(game, a);
        Assert.Equal(2, game.Explosions.Count());
    }
}
