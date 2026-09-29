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
    public async Task PressDuringReloadIsIgnored()
    {
        var game = TestGames.NewGame(new MatchSettings { ReloadTicks = 3 });
        var a = game.JoinGame();

        Press(game, a);
        Press(game, a);
        await game.loopRunner.ProcessGameTick();
        await game.loopRunner.ProcessGameTick();
        Press(game, a);

        Assert.Single(game.Bullets, bullet => bullet.OwnerId == a);
    }

    [Fact]
    public async Task PressAfterReloadFires()
    {
        var game = TestGames.NewGame(new MatchSettings { ReloadTicks = 3 });
        var a = game.JoinGame();

        Press(game, a);
        for (var tick = 0; tick < 3; tick++)
            await game.loopRunner.ProcessGameTick();
        Press(game, a);

        Assert.Equal(2, game.Bullets.Count(bullet => bullet.OwnerId == a));
    }
}
