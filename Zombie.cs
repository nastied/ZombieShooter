using System.Drawing;

namespace ZombieShooter
{
    public class Zombie
    {
        public int X;
        public int Y;
        public Image Sprite;
        public Rectangle Bounds => new Rectangle(X, Y, Sprite.Width, Sprite.Height);
    }
}