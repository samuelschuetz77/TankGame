using GameLogic;
using GameLogic.Game;

namespace GameTest;

public class MatchSetupTests
{
    [Fact]
    public void LobbyStoresSanitizedSettings()
    {
        var lobby = new Lobby(new FakeHubContext());

        var game = lobby.CreateGame("custom", null, null,
            new MatchSettings { Health = 99, SpeedMultiplier = 1.5, TimeLimitMinutes = 5 });

        Assert.Equal(10, game.Settings.Health);
        Assert.Equal(1.5, game.Settings.SpeedMultiplier);
        Assert.Equal(5, game.Settings.TimeLimitMinutes);
        Assert.Equal(game.Settings, game.GetGameState().Settings);
    }

    [Fact]
    public void LobbyUsesDefaultSettingsWhenNoneAreSent()
    {
        var lobby = new Lobby(new FakeHubContext());

        var game = lobby.CreateGame("plain");

        Assert.Equal(new MatchSettings(), game.Settings);
    }

    [Fact]
    public void FirstPlayerToJoinIsTheCreator()
    {
        var game = TestGames.NewGame();

        var creator = game.JoinGame();
        game.JoinGame();

        Assert.Equal(creator, game.CreatorId);
        Assert.Equal(creator, game.GetGameState().CreatorId);
    }
}
