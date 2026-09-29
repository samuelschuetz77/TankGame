using System.Collections.Concurrent;
using Microsoft.AspNetCore.SignalR;

namespace GameLogic.Game;

public class Game
{
    private readonly IHubContext<LobbyHub> hubContext;
    internal object StateLock { get; } = new();

    public GameStatus Status { get; private set; } = GameStatus.Playing;
    // Null while playing, and also when an ended match is a draw
    public Guid? WinnerId { get; private set; }
    // Game loop ticks processed so far (10 per second)
    public int Tick { get; internal set; }
    // Tick when the 2nd player joined; the time limit counts from here
    public int? StartedAtTick { get; private set; }
    public int? TicksLeft => Settings.TimeLimitMinutes == 0 || StartedAtTick is null
        ? null
        : StartedAtTick.Value + Settings.TimeLimitMinutes * 60 * GameLoopRunner.TicksPerSecond - Tick;
    //public event Action? OnUpdate;
    public readonly ConcurrentDictionary<string, byte> ConnectedClients = new();
    public string? Name { get; init; }
    public string MatchType { get; init; } = GameMatchTypes.Multiplayer;
    public DeveloperGameSettings DeveloperSettings { get; private set; } = new();
    public GameMap Map { get; init; } = MapCatalog.DefaultMap;
    private MatchSettings settings = new();
    public MatchSettings Settings { get => settings; init => settings = MatchSettings.Sanitize(value); }
    // First player to join; only they can change settings during the match
    public Guid? CreatorId { get; private set; }
    public IEnumerable<Tank> Tanks { get; internal set; } = [];
    public IEnumerable<Bullet> Bullets { get; internal set; } = [];
    public CancellationTokenSource CancellationTokenSource { get; set; } = new CancellationTokenSource();
    public GameLoopRunner loopRunner { get; set; }

    public Game(IHubContext<LobbyHub> context)
    {
        loopRunner = new(this);
        hubContext = context;
    }

    public GameState GetGameState()
    {
        return new()
        {
            Status = Status,
            Name = Name,
            MatchType = MatchType,
            DeveloperSettings = DeveloperSettings,
            Settings = Settings,
            CreatorId = CreatorId,
            WinnerId = WinnerId,
            SecondsLeft = TicksLeft is int ticksLeft
                ? (Math.Max(0, ticksLeft) + GameLoopRunner.TicksPerSecond - 1) / GameLoopRunner.TicksPerSecond
                : null,
            Map = Map,
            Tanks = Tanks.Select(t => new TankState()
            {
                Id = t.Id,
                PositionX = t.PositionX,
                PositionY = t.PositionY,
                Angle = t.Angle,
                TurretAngle = t.TurretAngle,
                Health = t.Health,
                Eliminated = t.Eliminated,
                HitsLanded = t.HitsLanded,
            }).ToArray(),
            Bullets = Bullets.Select(b => new BulletState()
            {
                Id = b.Id,
                PositionX = b.PositionX,
                PositionY = b.PositionY,
                Angle = b.Angle
            }).ToArray()
        };
    }

    public async Task BroadcastUpdate()
    {
        await hubContext.Clients.Clients(ConnectedClients.Keys.ToArray()).SendAsync(Messages.GameUpdate, GetGameState());
    }

    public Guid JoinGame()
    {
        lock (StateLock)
        {
        if (Status == GameStatus.Ended)
            throw new InvalidOperationException($"cannot join game, it has ended: {Name}");

        var spawnPoint = Map.SpawnPoints.ElementAt(Tanks.Count() % Map.SpawnPoints.Count);
        var newTank = new Tank
        {
            PositionX = spawnPoint.X,
            PositionY = spawnPoint.Y,
            Angle = spawnPoint.Angle,
            Health = Settings.Health
        };
        Tanks = Tanks.Append(newTank);
        CreatorId ??= newTank.Id;
        if (Tanks.Count() == 2)
            StartedAtTick = Tick;
        return newTank.Id;
        }
    }

    public void ReceiveUserInput(PlayerInputRequest request)
    {
        lock (StateLock)
        {
        if (Status == GameStatus.Ended)
            return;

        Tanks = Tanks.Select(t =>
        {
            // Eliminated players keep watching but can't drive or shoot
            if (t.Id == request.PlayerId && !t.Eliminated)
            {

                var updatedTank = t with
                {
                    MovingUp = request.Up,
                    MovingLeft = request.Left,
                    MovingRight = request.Right,
                    Shooting = request.Shoot,
                    MovingDown = request.Down,
                    AimX = request.AimX ?? t.AimX,
                    AimY = request.AimY ?? t.AimY,
                };
                updatedTank = Tank.AimTurret(updatedTank, DeveloperSettings);

                // Fire once per press, and only when reloaded; a press during reload is dropped, not queued
                if (updatedTank.Shooting && !t.Shooting && t.ReloadTicksLeft == 0)
                {
                    Bullets = Bullets.Append(Tank.FireBullet(updatedTank, DeveloperSettings, Settings.MaxBounces));
                    updatedTank = updatedTank with { ReloadTicksLeft = Settings.ReloadTicks };
                }

                //if (updatedTank.Bullet != null)
                //{
                //    updatedTank = updatedTank with
                //    {
                //        Bullet = Bullet.MoveBullet(updatedTank)
                //    };
                //}
                return updatedTank;
            }
            return t;
        })
        .ToArray();
        }
    }

    public void UpdateDeveloperSettings(DeveloperGameSettings settings)
    {
        if (MatchType != GameMatchTypes.DeveloperSimulation)
        {
            return;
        }

        DeveloperSettings = settings with
        {
            HitboxInset = Math.Clamp(settings.HitboxInset, 0, Tank.Size / 2 - 1),
            VisualTopOffset = Math.Clamp(settings.VisualTopOffset, 0, Tank.Size),
            CollisionStepPixels = Math.Clamp(settings.CollisionStepPixels, 1, 12),
            ForwardAcceleration = Math.Clamp(settings.ForwardAcceleration, 1, 30),
            BrakeAcceleration = Math.Clamp(settings.BrakeAcceleration, -30, 0),
            MaxSpeed = Math.Clamp(settings.MaxSpeed, 1, 160),
            TurnDegrees = Math.Clamp(settings.TurnDegrees, 1, 180),
            BackwardSpeedMultiplier = Math.Clamp(settings.BackwardSpeedMultiplier, 0.1, 1.5)
        };
    }

    internal void ApplyResult(MatchResult result)
    {
        if (!result.Ended)
            return;
        Status = GameStatus.Ended;
        WinnerId = result.WinnerId;
    }

}

public enum GameStatus
{
    NotStarted,
    Playing,
    Ended,
}
