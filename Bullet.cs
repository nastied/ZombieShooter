using System.Drawing;

namespace ZombieShooter
{
    public class Bullet
    {
        public int X;
        public int Y;
        public string Direction;
        public int Speed = 20;
        public Rectangle Bounds => new Rectangle(X, Y, 5, 5);
    }
}