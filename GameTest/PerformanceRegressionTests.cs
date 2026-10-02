using System.Diagnostics;
using System.Text.Json;
using GameLogic;
using GameLogic.Game;
using Xunit.Abstractions;

namespace GameTest;

public class PerformanceRegressionTests(ITestOutputHelper output)
{
    [Fact]
    public async Task MissingBattleSubscriptionReturnsARecoverableError()
    {
        var hub = new LobbyHub(new Lobby(new FakeHubContext()));
        var error = await Assert.ThrowsAsync<Microsoft.AspNetCore.SignalR.HubException>(
            () => hub.SubscribeToGame("Expired match"));
        Assert.Contains("Return to the lobby", error.Message);
    }

    [Fact]
    public void LiveSnapshotsOmitStaticMapsAndKeepInputAcknowledgements()
    {
        var game = new Game(new FakeHubContext())
        {
            Map = MapCatalog.FixedMaps.First(m => m.Name == "Highland Ruins")
        };
        var player = game.JoinGame();
        game.ReceiveUserInput(TestGames.Input(player) with { InputSequence = 42 });

        var initial = game.GetGameState();
        var live = game.GetGameState(includeMap: false);

        Assert.Same(game.Map, initial.Map);
        Assert.Null(live.Map);
        Assert.Equal(42, Assert.Single(live.Tanks!).InputSequence);
        var initialBytes = JsonSerializer.SerializeToUtf8Bytes(initial).Length;
        var liveBytes = JsonSerializer.SerializeToUtf8Bytes(live).Length;
        Assert.True(liveBytes < initialBytes / 4, $"Initial: {initialBytes}; live: {liveBytes}");
    }

    [Fact]
    public void SvgGeometryIsCachedAndInvalidatedWhenShapeChanges()
    {
        var arc = new Obstacle(100, 100, 300, 300) { Shape = ShapeKind.Arc };
        var cached = arc.SvgPoints;
        Assert.Same(cached, arc.SvgPoints);
        var moved = arc with { X = 500 };
        Assert.NotEqual(cached, moved.SvgPoints);
        Assert.Same(moved.SvgPoints, moved.SvgPoints);
        var json = JsonSerializer.Serialize(arc);
        Assert.DoesNotContain("SvgPoints", json);
        Assert.DoesNotContain("Outline", json);
    }

    [Theory]
    [InlineData("Frontier Town")]
    [InlineData("Highland Ruins")]
    [InlineData("Farmland Patrol")]
    public async Task FortyMovingTanksAndProjectilesStayWithinTickBudget(string mapName)
    {
        var game = new Game(new FakeHubContext())
        {
            Map = MapCatalog.GetByName(mapName),
            Settings = new MatchSettings { Health = 10, Lives = 10 },
            SpawnRandom = new FirstSpawnRandom()
        };
        for (var i = 0; i < 40; i++) game.JoinGame();
        var durations = new List<double>();
        for (var tick = 0; tick < 120; tick++)
        {
            foreach (var tank in game.Tanks.ToArray())
                game.ReceiveUserInput(TestGames.Input(tank.Id, tick % 10 == 0,
                    tank.PositionX + 300, tank.PositionY + 100) with { Up = true, Right = tick % 40 >= 20 });
            if (tick % 10 == 0)
                game.Bullets = game.Bullets.Concat(game.Tanks.Where(t => !t.Respawning && !t.Eliminated)
                    .Select(t => Tank.FireBullet(t, game.DeveloperSettings, 2))).ToArray();
            var at = Stopwatch.GetTimestamp();
            await game.loopRunner.ProcessGameTick();
            _ = JsonSerializer.SerializeToUtf8Bytes(game.GetGameState(includeMap: false));
            if (tick >= 20) durations.Add(Stopwatch.GetElapsedTime(at).TotalMilliseconds);
        }
        durations.Sort();
        var p95 = durations[(int)(durations.Count * .95)];
        output.WriteLine($"{mapName}: 40 tanks, simulation + snapshot serialization p95={p95:F2}ms, max={durations[^1]:F2}ms");
        Assert.Equal(40, game.Tanks.Count());
        Assert.True(p95 < 100, $"p95 {p95:F2}ms exceeds the 100ms simulation budget");
    }
}
