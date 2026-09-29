namespace GameLogic.Game;

public class GameLoopRunner
{
    private readonly Game game;
    private object loopLock { get; } = new object();
    private bool loopIsRunning { get; set; } = false;
    public static double TickIntervalScalar = 10;

    private ReplaySaver? saver;
    public GameLoopRunner(Game game)
    {
        this.game = game;
    }


    public void RunGameLoop()
    {
        Task.Run(async () =>
        {
            game.CancellationTokenSource.Token.ThrowIfCancellationRequested();
            lock (loopLock)
            {
                if (loopIsRunning)
                {
                    Console.WriteLine("Another thread is already running the loop.");
                    return;
                }

                loopIsRunning = true;
            }

            while (!game.CancellationTokenSource.Token.IsCancellationRequested)
            {
                await ProcessGameTick();
                var interval = (int)(10 * TickIntervalScalar);
                // Console.WriteLine($"sleeping {interval}");

                Thread.Sleep(interval);
            }
            loopIsRunning = false;

        });
    }
    public async Task ProcessGameTick()
    {
        Console.WriteLine($"processing game tick {game.Tick}");

        //var copy = game.Tanks.ToArray();
        //foreach (var tank in copy) {
        //    Console.WriteLine(tank);
        //}
        //foreach (var tank in game.Tanks) {
        //    Console.WriteLine(tank);
        //}
        //Console.WriteLine();
        // Input and simulation both replace state; keep either update from overwriting the other.
        lock (game.StateLock)
        {
        // An ended match is frozen; updates keep going out so everyone sees the result
        if (game.Status != GameStatus.Ended)
        {
            game.Tick++;
            game.Tanks = game.Tanks.Select(tank => Tank.ProcessTankMovement(tank, game.Map, game.DeveloperSettings)).ToArray();
            game.Bullets = game.Bullets
                .Select(bullet => Bullet.MoveBullet(bullet, game.Map))
                .Where(bullet => bullet is not null)
                .Cast<Bullet>()
                .ToArray();
            var (tanks, bullets) = Combat.ResolveHits(game.Tanks, game.Bullets, game.DeveloperSettings);
            game.Tanks = tanks;
            game.Bullets = bullets;
            game.ApplyResult(Combat.DecideResult(tanks));
        }
        }

        saver?.SaveTick(game.Tanks, game.Tick, game.Name ?? string.Empty);

        await game.BroadcastUpdate();
    }
}
