using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace ZombieShooter
{
    public partial class Form1 : Form
    {
        bool goLeft, goRight, goUp, goDown, gameOver;
        string facing = "up";
        int playerHealth = 100;
        int speed = 10;
        int zombieSpeed = 3;
        int ammo = 10, score = 0;
        Random randNum = new Random();

        int cnt = 0;
        int cnt2 = 0;
        bool ok = false;

        // Player custom properties
        int playerX;
        int playerY;
        Image playerImage;
        Rectangle playerBounds => new Rectangle(playerX, playerY, playerImage.Width, playerImage.Height);

        // Our Game Data Lists
        List<Zombie> zombiesList = new List<Zombie>();
        List<Drop> dropsList = new List<Drop>();
        List<Bullet> bulletsList = new List<Bullet>();

        public Form1()
        {
            InitializeComponent();

            // 1. Lock window size
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;

            // 2. Turn on Double Buffering
            this.DoubleBuffered = true;

            label1.Text = "Health: ";
            label2.Text = "Give Up \\-\\";

            // FIX: Assign the image FIRST so it isn't null!
            playerImage = Properties.Resources.up;

            // NOW we can safely calculate its width and height to center it
            playerX = (this.ClientSize.Width / 2) - (playerImage.Width / 2);
            playerY = (this.ClientSize.Height / 2) - (playerImage.Height / 2);

            RestartGame();
        }

        private void MainTimerEvent(object sender, EventArgs e)
        {
            if (playerHealth > 1)
            {
                healthBar.Value = playerHealth;
            }
            else
            {
                gameOver = true;
                panel1.Visible = true;
                panel1.BringToFront();
                playerImage = Properties.Resources.dead;
                GameTimer.Stop();
            }

            txtAmmo.Text = "Ammo: " + ammo;
            txtScore.Text = "Kills: " + score;

            // 1. MOVE PLAYER
            if (goLeft && playerX > 0) playerX -= speed;
            if (goRight && playerX + playerImage.Width < this.ClientSize.Width) playerX += speed;
            if (goUp && playerY > 45) playerY -= speed;
            if (goDown && playerY + playerImage.Height < this.ClientSize.Height) playerY += speed;

            // Tick buff timers
            cnt++;
            cnt2++;
            if (cnt2 > 150 && ok)
            {
                speed -= 5;
                ok = false;
            }

            // 2. MOVE BULLETS
            for (int i = bulletsList.Count - 1; i >= 0; i--)
            {
                Bullet b = bulletsList[i];
                if (b.Direction == "left") b.X -= b.Speed;
                if (b.Direction == "right") b.X += b.Speed;
                if (b.Direction == "up") b.Y -= b.Speed;
                if (b.Direction == "down") b.Y += b.Speed;

                // Delete bullet if it goes off screen
                if (b.X < 0 || b.X > this.ClientSize.Width || b.Y < 0 || b.Y > this.ClientSize.Height)
                {
                    bulletsList.RemoveAt(i);
                }
            }

            // 3. CHECK DROPS (Ammo, Health, Speed, Bad)
            for (int i = dropsList.Count - 1; i >= 0; i--)
            {
                Drop d = dropsList[i];
                d.Timer++;

                // Despawn bad item after ~3 seconds
                if (d.Tag == "bad" && d.Timer > 150)
                {
                    dropsList.RemoveAt(i);
                    continue;
                }

                if (playerBounds.IntersectsWith(d.Bounds))
                {
                    if (d.Tag == "ammo") ammo += 5;
                    if (d.Tag == "ammo2") ammo += 3;
                    if (d.Tag == "health")
                    {
                        playerHealth += 10;
                        if (playerHealth > 100) playerHealth = 100;
                    }
                    if (d.Tag == "speed")
                    {
                        speed += 5;
                        cnt2 = 0;
                        ok = true;
                    }
                    if (d.Tag == "bad") playerHealth -= 5;

                    dropsList.RemoveAt(i);
                }
            }

            // 4. MOVE ZOMBIES & CHECK COLLISIONS
            int newZombiesToSpawn = 0;

            for (int i = zombiesList.Count - 1; i >= 0; i--)
            {
                Zombie z = zombiesList[i];

                // Damage Player
                if (playerBounds.IntersectsWith(z.Bounds))
                {
                    playerHealth -= 1;
                }

                // Zombie follows player
                if (z.X > playerX) { z.X -= zombieSpeed; z.Sprite = Properties.Resources.zleft; }
                if (z.X < playerX) { z.X += zombieSpeed; z.Sprite = Properties.Resources.zright; }
                if (z.Y > playerY) { z.Y -= zombieSpeed; z.Sprite = Properties.Resources.zup; }
                if (z.Y < playerY) { z.Y += zombieSpeed; z.Sprite = Properties.Resources.zdown; }

                // Did a bullet hit this zombie?
                bool zombieDead = false;
                for (int j = bulletsList.Count - 1; j >= 0; j--)
                {
                    if (z.Bounds.IntersectsWith(bulletsList[j].Bounds))
                    {
                        score++;
                        newZombiesToSpawn++;
                        zombieDead = true;
                        bulletsList.RemoveAt(j); // Destroy bullet

                        // Spawn random drop
                        int nr = randNum.Next(1, 31);
                        if (nr % 5 == 0) dropsList.Add(new Drop { X = z.X, Y = z.Y, Sprite = Properties.Resources.ammo2, Tag = "ammo2" });
                        else if (nr % 7 == 0) dropsList.Add(new Drop { X = z.X, Y = z.Y, Sprite = Properties.Resources.bad, Tag = "bad" });
                        else if (nr % 8 == 0) dropsList.Add(new Drop { X = z.X, Y = z.Y, Sprite = Properties.Resources.speed, Tag = "speed" });
                        else if (nr % 9 == 0) dropsList.Add(new Drop { X = z.X, Y = z.Y, Sprite = Properties.Resources.health, Tag = "health" });

                        break;
                    }
                }

                if (zombieDead)
                {
                    zombiesList.RemoveAt(i);
                }
            }

            // Spawn replacement zombies
            for (int i = 0; i < newZombiesToSpawn; i++)
            {
                MakeZombies();
            }

            // 5. WIPE THE SCREEN AND REDRAW EVERYTHING!
            this.Invalidate();
        }

        // Overriding the default form painting to draw our graphics perfectly
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics;

            // Draw drops (bottom layer)
            foreach (Drop d in dropsList)
            {
                g.DrawImage(d.Sprite, d.X, d.Y);
            }

            // Draw zombies (middle layer)
            foreach (Zombie z in zombiesList)
            {
                g.DrawImage(z.Sprite, z.X, z.Y);
            }

            // Draw player (top layer)
            if (playerImage != null)
            {
                g.DrawImage(playerImage, playerX, playerY);
            }

            // Draw bullets
            foreach (Bullet b in bulletsList)
            {
                g.FillRectangle(Brushes.White, b.Bounds);
            }
        }

        private void KeyIsDown(object sender, KeyEventArgs e)
        {
            if (gameOver) return;

            if (e.KeyCode == Keys.Left) { goLeft = true; facing = "left"; playerImage = Properties.Resources.left; }
            if (e.KeyCode == Keys.Right) { goRight = true; facing = "right"; playerImage = Properties.Resources.right; }
            if (e.KeyCode == Keys.Up) { goUp = true; facing = "up"; playerImage = Properties.Resources.up; }
            if (e.KeyCode == Keys.Down) { goDown = true; facing = "down"; playerImage = Properties.Resources.down; }
        }

        private void KeyIsUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Left) goLeft = false;
            if (e.KeyCode == Keys.Right) goRight = false;
            if (e.KeyCode == Keys.Up) goUp = false;
            if (e.KeyCode == Keys.Down) goDown = false;

            if (e.KeyCode == Keys.Space && ammo > 0 && !gameOver)
            {
                ammo--;
                ShootBullet(facing);

                if (ammo < 1)
                {
                    DropAmmo();
                }
            }

            if (e.KeyCode == Keys.Enter && gameOver)
            {
                RestartGame();
            }
        }

        private void ShootBullet(string direction)
        {
            Bullet shootBullet = new Bullet();
            shootBullet.Direction = direction;
            shootBullet.X = playerX + (playerImage.Width / 2);
            shootBullet.Y = playerY + (playerImage.Height / 2);
            bulletsList.Add(shootBullet);
        }

        private void MakeZombies()
        {
            Zombie z = new Zombie();
            z.Sprite = Properties.Resources.zdown;
            z.X = randNum.Next(10, this.ClientSize.Width - 50);
            z.Y = randNum.Next(50, this.ClientSize.Height - 50);
            zombiesList.Add(z);
        }

        private void DropAmmo()
        {
            Drop ammoDrop = new Drop();
            ammoDrop.Sprite = Properties.Resources.ammo_Image;
            ammoDrop.X = randNum.Next(10, this.ClientSize.Width - 50);
            ammoDrop.Y = randNum.Next(60, this.ClientSize.Height - 50);
            ammoDrop.Tag = "ammo";
            dropsList.Add(ammoDrop);
        }

        private void RestartGame()
        {
            panel1.Visible = false;
            playerImage = Properties.Resources.up;

            // Re-center player based on the hidden picturebox
            // Re-center player in the middle of the screen
            playerX = (this.ClientSize.Width / 2) - (playerImage.Width / 2);
            playerY = (this.ClientSize.Height / 2) - (playerImage.Height / 2);

            zombiesList.Clear();
            dropsList.Clear();
            bulletsList.Clear();

            for (int i = 0; i < 3; i++)
            {
                MakeZombies();
            }

            goUp = goDown = goLeft = goRight = gameOver = false;

            playerHealth = 100;
            score = 0;
            ammo = 10;

            this.ActiveControl = null;
            GameTimer.Start();
        }

        private void label2_Click(object sender, EventArgs e)
        {
            System.Environment.Exit(0);
        }
    }

    // --- The Drop Class ---
    public class Drop
    {
        public int X;
        public int Y;
        public string Tag;
        public Image Sprite;
        public int Timer = 0;
        public Rectangle Bounds => new Rectangle(X, Y, Sprite.Width, Sprite.Height);
    }
}