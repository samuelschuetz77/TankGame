using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameLogic
{
    public record Bullet
    {
        public Guid Id { get; init; } = Guid.NewGuid();
        public int PositionX { get; init; }
        public int PositionY { get; init; }
        public int Angle { get; init; }
        // Tank that fired it; hitting another tank counts as a hit landed for this tank
        public Guid OwnerId { get; init; }
        // Wall bounces left; set from the match settings when fired
        public int BouncesLeft { get; init; }
        public const int DefaultSpeed = 20;
        // Pixels per tick; set from the match settings when fired
        public int Speed { get; init; } = DefaultSpeed;
        public const int BulletSize = 10;

        public static Bullet? MoveBullet(Bullet bullet)
        {
            return MoveBullet(bullet, MapCatalog.DefaultMap);
        }

        public static Bullet? MoveBullet(Bullet bullet, GameMap map)
        {
            if (map.Blocks(Area(bullet.PositionX, bullet.PositionY)))
            {
                return null;
            }
            double radians = Math.PI * bullet.Angle / 180.0;
            var deltaX = (int)(bullet.Speed * Math.Cos(radians));
            var deltaY = (int)(bullet.Speed * Math.Sin(radians));
            if (!map.Blocks(Area(bullet.PositionX + deltaX, bullet.PositionY + deltaY)))
            {
                return bullet with
                {
                    PositionX = bullet.PositionX + deltaX,
                    PositionY = bullet.PositionY + deltaY
                };
            }
            if (bullet.BouncesLeft <= 0)
            {
                return null;
            }

            // Which way was blocked tells us the wall's direction; the bounce uses up this tick's move
            var blockedX = map.Blocks(Area(bullet.PositionX + deltaX, bullet.PositionY));
            var blockedY = map.Blocks(Area(bullet.PositionX, bullet.PositionY + deltaY));
            var newAngle = blockedX && !blockedY ? 180 - bullet.Angle
                : blockedY && !blockedX ? -bullet.Angle
                : bullet.Angle + 180;
            return bullet with { Angle = NormalizeAngle(newAngle), BouncesLeft = bullet.BouncesLeft - 1 };
        }

        private static RectangleArea Area(int x, int y) => new(x, y, BulletSize, BulletSize);

        // Wraps to (-180, 180]
        private static int NormalizeAngle(int angle)
        {
            var wrapped = ((angle % 360) + 360) % 360;
            return wrapped > 180 ? wrapped - 360 : wrapped;
        }
    }
}
