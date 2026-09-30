namespace GameLogic;

public readonly record struct MapCamera(double Scale, double X, double Y)
{
    public static TankState? FollowTarget(IEnumerable<TankState> tanks, Guid playerId, Guid? previousTarget)
    {
        var living = tanks.Where(t => !t.Eliminated && !t.Respawning).ToArray();
        return living.FirstOrDefault(t => t.Id == playerId)
            ?? living.FirstOrDefault(t => t.Id == previousTarget)
            ?? living.FirstOrDefault();
    }

    public static MapCamera For(GameMap map, double centerX, double centerY)
    {
        if (map.Mode != MapMode.Foggish)
            return new(Math.Min((double)map.ViewWidth / map.Width, (double)map.ViewHeight / map.Height), 0, 0);
        return new(1, Math.Clamp(centerX - map.ViewWidth / 2.0, 0, Math.Max(0, map.Width - map.ViewWidth)),
            Math.Clamp(centerY - map.ViewHeight / 2.0, 0, Math.Max(0, map.Height - map.ViewHeight)));
    }

    public (int X, int Y) ToWorld(double screenX, double screenY) =>
        ((int)Math.Round(screenX / Scale + X), (int)Math.Round(screenY / Scale + Y));
}
