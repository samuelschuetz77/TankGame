using System.Text.Json;
using GameLogic;
using GameLogic.Game;
using Microsoft.AspNetCore.SignalR;


public class LobbyHub : Hub
{
  private readonly Lobby lobby;
  public LobbyHub(Lobby lobby)
  {
    this.lobby = lobby;
  }
  public async Task SendMessage(string user, string message)
  {
    await Clients.All.SendAsync("ReceiveMessage", user, message);
  }

  // SignalR doesn't fill optional parameters, so clients must send all four arguments
  public async Task CreateGame(string name, string? mapName = null, string? matchType = null, MatchSettings? settings = null)
  {
    var nameTaken = lobby.Games.FirstOrDefault(g => g.Name == name) != null;
    if(nameTaken)
    {
      throw new Exception($"cannot create game, name already taken: {name}");
    }

    var game = lobby.CreateGame(name, mapName, matchType, settings);
    Console.WriteLine($"created game: {name}");

    var playerId = game.JoinGame();

    await Clients.Client(Context.ConnectionId).SendAsync(Messages.CreatedGame, game.Name, playerId);
    game.loopRunner.RunGameLoop();

    var games = lobby.Games.Select(g => g.GetGameState()).ToArray();
    await Clients.All.SendAsync(Messages.GameList, games);
  }

  public async Task JoinGame(string gameName)
  {
    var game = lobby.Games.First(g => g.Name == gameName);
    var playerId = game.JoinGame();
    SubscribeToGame(gameName);
    await Clients.Client(Context.ConnectionId).SendAsync(Messages.JoinedGame, game.Name, playerId);
  }

  public async Task GetGames()
  {
    var games = lobby.Games.Select(g => g.GetGameState()).ToArray();
    Console.WriteLine("got request for games");

    await Clients.Client(Context.ConnectionId).SendAsync(Messages.GameList, games);
  }

  public void SubscribeToGame(string gameName)
  {
    Console.WriteLine("subscribing to game");

    var game = lobby.Games.First(g => g.Name == gameName);

    game.ConnectedClients.TryAdd(Context.ConnectionId, 0);

  }

  public async Task PlayerInput(PlayerInputRequest request)
  {
    Console.WriteLine("got player input");
    Console.WriteLine(request);
    var game = lobby.Games.First(g => g.Name == request.GameName);
    // An instant shot shouldn't wait for the next 100 ms tick to show its explosion
    if (game.ReceiveUserInput(request))
      await game.BroadcastUpdate();
  }

  public async Task UpdateDeveloperSettings(string gameName, DeveloperGameSettings settings)
  {
    var game = lobby.Games.First(g => g.Name == gameName);
    game.UpdateDeveloperSettings(settings);
    await game.BroadcastUpdate();
  }

  public async Task UpdateMatchSettings(string gameName, Guid playerId, MatchSettings settings)
  {
    var game = lobby.Games.First(g => g.Name == gameName);
    game.UpdateMatchSettings(playerId, settings);
    await game.BroadcastUpdate();
  }

  public override async Task OnDisconnectedAsync(Exception? exception)
  {
    string? connectionId = Context.ConnectionId;

    foreach (var game in lobby.Games)
    {
      if (game.ConnectedClients.TryRemove(connectionId, out _))
      {
        Console.WriteLine($"Removed connection: {connectionId} from game {game.Name}");
      }
    }
    await base.OnDisconnectedAsync(exception);
  }

}
