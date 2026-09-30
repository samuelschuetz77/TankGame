namespace GameLogic;

public static class SpawnSelector
{
    public const int Clearance = Tank.Size;

    public static MapSpawnPoint? Choose(GameMap map, IEnumerable<Tank> tanks, Random rng, DeveloperGameSettings? settings = null)
    {
        settings ??= new DeveloperGameSettings();
        var living = tanks.Where(t => !t.Eliminated && !t.Respawning).ToArray();
        var free = map.SpawnPoints.Where(p =>
        {
            var box = new RectangleArea(p.X, p.Y - settings.VisualTopOffset, Tank.Size, Tank.Size);
            var clearance = new RectangleArea(p.X - Clearance, p.Y - settings.VisualTopOffset - Clearance,
                Tank.Size + 2 * Clearance, Tank.Size + 2 * Clearance);
            return !map.Blocks(box) && !living.Any(t => clearance.Intersects(
                new RectangleArea(t.PositionX, t.PositionY - settings.VisualTopOffset, Tank.Size, Tank.Size)));
        }).ToArray();
        return free.Length == 0 ? null : free[rng.Next(free.Length)];
    }
}
