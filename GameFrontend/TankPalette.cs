namespace GameFrontend;

// A tank's color is derived from its id, and its bullets are drawn in the matching color
public static class TankPalette
{
    private static readonly string[] colors = ["Beige", "Black", "Blue", "Green", "Red"];

    public static string TankColor(Guid tankId) => colors[tankId.ToByteArray()[0] % colors.Length];

    // The bullet sprites have no black variant, so black tanks fire silver
    public static string BulletColor(Guid ownerId) => TankColor(ownerId) switch
    {
        "Black" => "Silver",
        var color => color
    };
}
