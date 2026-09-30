using GameLogic;

namespace GameTest;

public class LivesAndRespawnTests
{
    private static readonly DeveloperGameSettings Dev = new();

    private static Tank TankAt(int x, int health = 3) => new() { PositionX = x, PositionY = 200, Health = health };

    private static Bullet BulletOn(Tank target, Guid owner) => new()
    {
        PositionX = Tank.GetCollisionArea(target, Dev).X + 5,
        PositionY = Tank.GetCollisionArea(target, Dev).Y + 5,
        OwnerId = owner,
    };

    [Fact]
    public void FirstDeathWaitsForTheRespawnDelayInsteadOfEliminating()
    {
        var shooter = TankAt(100);
        var target = TankAt(400, health: 1);
        var match = new MatchSettings { Lives = 5, RespawnSeconds = 5 };

        var (tanks, _) = Combat.ResolveHits([shooter, target], [BulletOn(target, shooter.Id)], Dev, match);

        var dead = tanks.Single(t => t.Id == target.Id);
        Assert.False(dead.Eliminated);
        Assert.True(dead.Respawning);
        Assert.Equal(1, dead.Deaths);
        Assert.Equal(50, dead.RespawnTicksLeft);
    }

    [Fact]
    public void FifthDeathIsPermanent()
    {
        var shooter = TankAt(100);
        var target = TankAt(400, health: 1) with { Deaths = 4 };
        var match = new MatchSettings { Lives = 5 };

        var (tanks, _) = Combat.ResolveHits([shooter, target], [BulletOn(target, shooter.Id)], Dev, match);

        var dead = tanks.Single(t => t.Id == target.Id);
        Assert.True(dead.Eliminated);
        Assert.Equal(5, dead.Deaths);
    }

    [Fact]
    public void RespawningTankCannotBeHit()
    {
        var shooter = TankAt(100);
        var target = TankAt(400, health: 0) with { RespawnTicksLeft = 30 };

        var (tanks, bullets) = Combat.ResolveHits([shooter, target], [BulletOn(target, shooter.Id)], Dev);

        Assert.Equal(0, tanks.Single(t => t.Id == target.Id).Deaths);
        Assert.Single(bullets);
    }

    [Fact]
    public void RespawnCountsDownThenReturnsWithFullHealthAtASpawnPoint()
    {
        var match = new MatchSettings { Health = 4 };
        var waiting = TankAt(400, health: 0) with { RespawnTicksLeft = 2, Deaths = 1 };

        var counting = Combat.TickRespawns([waiting], TestGames.Arena, match, new Random(1)).Single();
        Assert.True(counting.Respawning);
        Assert.Equal(1, counting.RespawnTicksLeft);

        var back = Combat.TickRespawns([counting], TestGames.Arena, match, new Random(1)).Single();
        Assert.False(back.Respawning);
        Assert.Equal(4, back.Health);
        Assert.Contains(TestGames.Arena.SpawnPoints, s => s.X == back.PositionX && s.Y == back.PositionY);
    }

    [Fact]
    public void RespawnSpotIsRandomAndCanRepeat()
    {
        var match = new MatchSettings();
        var seen = new HashSet<(int, int)>();
        var rng = new Random(7);
        for (var i = 0; i < 60; i++)
        {
            var waiting = TankAt(400, health: 0) with { RespawnTicksLeft = 1, Deaths = 1 };
            var back = Combat.TickRespawns([waiting], TestGames.Arena, match, rng).Single();
            seen.Add((back.PositionX, back.PositionY));
        }
        // Every spawn point gets picked over enough respawns, not a fixed rotation
        Assert.Equal(TestGames.Arena.SpawnPoints.Count, seen.Count);
    }

    [Fact]
    public void ZeroSecondRespawnReturnsNextTick()
    {
        var target = TankAt(1, health: 1);
        var shooter = TankAt(100);
        var match = new MatchSettings { RespawnSeconds = 0 };
        var (tanks, _) = Combat.ResolveHits([shooter, target], [BulletOn(target, shooter.Id)], Dev, match);

        var back = Combat.TickRespawns(tanks, TestGames.Arena, match, new Random(1)).Single(t => t.Id == target.Id);

        Assert.False(back.Respawning);
        Assert.Equal(match.Health, back.Health);
    }

    [Fact]
    public void RespawningTankDoesNotMove()
    {
        var waiting = TankAt(400, health: 0) with { RespawnTicksLeft = 20, MovingRight = true, Speed = 8 };

        var after = Tank.ProcessTankMovement(waiting, TestGames.Arena, Dev);

        Assert.Equal(400, after.PositionX);
    }

    [Fact]
    public void LivesAreLockedOnceTheMatchStarts()
    {
        var running = MatchSettings.Sanitize(new MatchSettings { Lives = 3 });
        var changed = MatchSettings.Sanitize(new MatchSettings { Lives = 9, RespawnSeconds = 2 }).WithLockedFrom(running);

        Assert.Equal(3, changed.Lives);
        Assert.Equal(2, changed.RespawnSeconds);
    }

    [Fact]
    public void FewerDeathsWinTheTimeLimitTiebreak()
    {
        var careful = new Tank { Deaths = 1 };
        var reckless = new Tank { Deaths = 3, HitsLanded = 10 };

        var result = Combat.DecideResult([careful, reckless], ticksLeft: 0);

        Assert.Equal(careful.Id, result.WinnerId);
    }
}

public class DeveloperSimulationSettingsTests
{
    private static GameLogic.Game.Game NewGame(string matchType, MatchSettings settings) =>
        new(new FakeHubContext()) { Map = TestGames.Arena, MatchType = matchType, Settings = settings };

    [Fact]
    public void DeveloperSimulationCanChangeHealthLivesAndTimeLimitLive()
    {
        var game = NewGame(GameMatchTypes.DeveloperSimulation, new MatchSettings { Health = 3, Lives = 5 });
        var creator = game.JoinGame();

        game.UpdateMatchSettings(creator, new MatchSettings { Health = 6, Lives = 2, TimeLimitMinutes = 3 });

        Assert.Equal(6, game.Settings.Health);
        Assert.Equal(2, game.Settings.Lives);
        Assert.Equal(3, game.Settings.TimeLimitMinutes);
    }

    [Fact]
    public void ChangingHealthInDeveloperSimulationRefillsTanks()
    {
        var game = NewGame(GameMatchTypes.DeveloperSimulation, new MatchSettings { Health = 3 });
        var creator = game.JoinGame();
        game.Tanks = game.Tanks.Select(t => t with { Health = 1 }).ToArray();

        game.UpdateMatchSettings(creator, new MatchSettings { Health = 8 });

        Assert.Equal(8, game.Tanks.Single().Health);
    }

    [Fact]
    public void MultiplayerStillLocksHealthLivesAndTimeLimit()
    {
        var game = NewGame(GameMatchTypes.Multiplayer, new MatchSettings { Health = 3, Lives = 5 });
        var creator = game.JoinGame();

        game.UpdateMatchSettings(creator, new MatchSettings { Health = 9, Lives = 1, TimeLimitMinutes = 5, RespawnSeconds = 2 });

        Assert.Equal(3, game.Settings.Health);
        Assert.Equal(5, game.Settings.Lives);
        Assert.Equal(0, game.Settings.TimeLimitMinutes);
        Assert.Equal(2, game.Settings.RespawnSeconds);
    }
}
