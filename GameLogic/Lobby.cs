using System.Collections.Concurrent;
using GameLogic;
using GameLogic.Game;
using Microsoft.AspNetCore.SignalR;

public class Lobby
{
  public List<Game> Games { get; set; } = new();
  private readonly IHubContext<LobbyHub> context;
  private readonly IMapSource mapSource;

  //public event Action? OnLobbyUpdate;
  public Lobby(IHubContext<LobbyHub> context, IMapSource? mapSource = null)
  {
    this.context = context;
    this.mapSource = mapSource ?? new FixedMapSource();
  }

  public Game CreateGame(string name, string? mapName = null, string? matchType = null)
  {
    var newGame = new Game(context)
    {
      Name = name,
      MatchType = matchType == GameMatchTypes.DeveloperSimulation
        ? GameMatchTypes.DeveloperSimulation
        : GameMatchTypes.Multiplayer,
      Map = mapSource.GetByName(mapName)
    };

    Games.Add(newGame);
    return newGame;
  }
}
