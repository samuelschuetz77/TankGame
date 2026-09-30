using GameLogic;

namespace GameTest;

public class BulletBounceTests
{
    [Theory]
    [InlineData(0, 180)]
    [InlineData(30, 150)]
    [InlineData(-30, -150)]
    public void VerticalWallReflectsAcrossIt(int angle, int expected)
    {
        var map = new GameMap("Wall", 300, 300, [new Obstacle(100, 0, 20, 300)], []);
        var bullet = new Bullet { PositionX = 80, PositionY = 150, Angle = angle, BouncesLeft = 1 };

        var bounced = Bullet.MoveBullet(bullet, map);

        Assert.NotNull(bounced);
        Assert.Equal(expected, bounced.Angle);
        Assert.Equal(0, bounced.BouncesLeft);
        Assert.Equal(bullet.PositionX, bounced.PositionX);
        Assert.Equal(bullet.PositionY, bounced.PositionY);
    }

    [Theory]
    [InlineData(90, -90)]
    [InlineData(60, -60)]
    [InlineData(120, -120)]
    public void HorizontalWallReflectsAcrossIt(int angle, int expected)
    {
        var map = new GameMap("Floor", 300, 300, [new Obstacle(0, 100, 300, 20)], []);
        var bullet = new Bullet { PositionX = 150, PositionY = 80, Angle = angle, BouncesLeft = 1 };

        var bounced = Bullet.MoveBullet(bullet, map);

        Assert.NotNull(bounced);
        Assert.Equal(expected, bounced.Angle);
    }

    [Fact]
    public void CornerSendsTheBulletStraightBack()
    {
        var map = new GameMap("Corner", 300, 300, [new Obstacle(95, 95, 50, 50)], []);
        var bullet = new Bullet { PositionX = 80, PositionY = 80, Angle = 45, BouncesLeft = 1 };

        var bounced = Bullet.MoveBullet(bullet, map);

        Assert.NotNull(bounced);
        Assert.Equal(-135, bounced.Angle);
    }

    [Fact]
    public void MapEdgeBouncesToo()
    {
        var map = new GameMap("Small", 60, 60, [], []);
        var bullet = new Bullet { PositionX = 45, PositionY = 20, Angle = 0, BouncesLeft = 1 };

        Assert.Equal(180, Bullet.MoveBullet(bullet, map)!.Angle);
    }

    [Fact]
    public void NoBouncesLeftMeansTheBulletDisappears()
    {
        var map = new GameMap("Wall", 300, 300, [new Obstacle(100, 0, 20, 300)], []);
        var bullet = new Bullet { PositionX = 80, PositionY = 150, Angle = 0, BouncesLeft = 0 };

        Assert.Null(Bullet.MoveBullet(bullet, map));
    }

    [Fact]
    public void OpenFlightKeepsItsBounces()
    {
        var map = new GameMap("Open", 300, 300, [], []);
        var bullet = new Bullet { PositionX = 100, PositionY = 100, Angle = 0, BouncesLeft = 2 };

        var moved = Bullet.MoveBullet(bullet, map);

        Assert.NotNull(moved);
        Assert.Equal(2, moved.BouncesLeft);
        Assert.Equal(120, moved.PositionX);
    }

    [Fact]
    public void FiredBulletsGetTheMatchBounceCount()
    {
        var game = TestGames.NewGame(new MatchSettings { MaxBounces = 4 });
        var a = game.JoinGame();

        game.ReceiveUserInput(TestGames.Input(a, shoot: true));

        Assert.Equal(4, game.Bullets.Single().BouncesLeft);
    }
}
