using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TheLostRobotStory.Core;

namespace TheLostRobotStory.Entities
{
    // Bolt = normal shot, Orb = big slow boss shot,
    // Wave = shockwave that runs along the floor (spawned by slams)
    public enum ProjectileKind { Bolt, Orb, Wave }

    public class Projectile
    {
        public Vector2 Position;   // top-left
        public Vector2 Velocity;
        public Vector2 Size;

        public bool IsDead;
        public bool FromPlayer;    // true = player shot
        public int Damage = 1;
        public ProjectileKind Kind;

        private float _life;
        private float _time;

        public Rectangle Bounds =>
            new Rectangle((int)Position.X, (int)Position.Y, (int)Size.X, (int)Size.Y);

        // startPosition is the CENTRE of the projectile
        public Projectile(
            Vector2 startPosition,
            Vector2 direction,
            bool fromPlayer = true,
            float speed = 270f,
            ProjectileKind kind = ProjectileKind.Bolt)
        {
            FromPlayer = fromPlayer;
            Kind = kind;

            switch (kind)
            {
                case ProjectileKind.Orb:
                    Size = new Vector2(14, 14);
                    _life = 4f;
                    break;

                case ProjectileKind.Wave:
                    Size = new Vector2(16, 26);
                    _life = 1.6f;
                    break;

                default:
                    Size = new Vector2(8, 8);
                    _life = 2.5f;
                    break;
            }

            Position = startPosition - Size / 2f;

            if (direction != Vector2.Zero)
                direction.Normalize();

            Velocity = direction * speed;
        }

        // Shockwave whose bottom sits on the floor at floorY
        public static Projectile Shockwave(float x, float floorY, int direction, float speed = 260f)
        {
            Projectile wave = new Projectile(Vector2.Zero, new Vector2(direction, 0), false, speed, ProjectileKind.Wave);
            wave.Position = new Vector2(x - wave.Size.X / 2f, floorY - wave.Size.Y);
            return wave;
        }

        public void Update(GameTime gameTime, List<Rectangle> solids)
        {
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

            _time += dt;
            _life -= dt;

            if (_life <= 0)
            {
                IsDead = true;
                return;
            }

            Position += Velocity * dt;

            Rectangle bounds = Bounds;

            foreach (Rectangle tile in solids)
            {
                if (bounds.Intersects(tile))
                {
                    IsDead = true;
                    return;
                }
            }
        }

        public bool HitEnemy(Enemy enemy)
        {
            if (!FromPlayer || IsDead || enemy.IsDead || enemy.Despawned)
                return false;

            if (!PixelCollision.Overlaps(GetShape(), enemy.GetShape()))
                return false;

            // moving right -> hits the enemy's left side
            int side = Velocity.X > 0 ? -1 : Velocity.X < 0 ? 1 : 0;

            enemy.TakeHit(Damage, true, side);
            IsDead = true;
            return true;
        }

        public bool HitPlayer(Player player)
        {
            if (FromPlayer || IsDead)
                return false;

            if (!PixelCollision.Overlaps(GetShape(), player.GetShape()))
                return false;

            player.TakeDamage(Damage);

            // shockwaves pass through, bullets stop
            if (Kind != ProjectileKind.Wave)
                IsDead = true;

            return true;
        }

        private Texture2D CurrentTexture =>
            Kind == ProjectileKind.Wave ? TextureManager.Get("shockwave") :
            Kind == ProjectileKind.Orb ? TextureManager.Get("orb") :
            FromPlayer ? TextureManager.Projectile :
            TextureManager.ProjectileEnemy ?? TextureManager.Projectile;

        public SpriteShape GetShape()
        {
            Texture2D texture = CurrentTexture;

            if (texture == null)
                return SpriteShape.FromRect(Bounds);

            int frame = Sprites.AnimFrame(texture, (int)Size.X, 10f);
            return SpriteShape.FromTexture(texture, Sprites.FrameRect(texture, frame, (int)Size.X), Bounds, Velocity.X < 0);
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            Texture2D texture =
                Kind == ProjectileKind.Wave ? TextureManager.Get("shockwave") :
                Kind == ProjectileKind.Orb ? TextureManager.Get("orb") :
                FromPlayer ? TextureManager.Projectile :
                TextureManager.ProjectileEnemy ?? TextureManager.Projectile;

            if (texture != null)
            {
                Sprites.Draw(
                    spriteBatch,
                    texture,
                    Bounds,
                    Color.White,
                    frameWidth: (int)Size.X,
                    fps: 10f,
                    effects: Velocity.X < 0 ? SpriteEffects.FlipHorizontally : SpriteEffects.None);

                return;
            }

            switch (Kind)
            {
                case ProjectileKind.Orb:
                    Rectangle glow = Bounds;
                    glow.Inflate(4, 4);
                    spriteBatch.Draw(TextureManager.Pixel, glow, Color.Red * 0.35f);
                    spriteBatch.Draw(TextureManager.Pixel, Bounds, Color.OrangeRed);
                    break;

                case ProjectileKind.Wave:
                    float flicker = 0.7f + 0.3f * (float)Math.Sin(_time * 40f);
                    spriteBatch.Draw(TextureManager.Pixel, Bounds, Color.Orange * flicker);
                    spriteBatch.Draw(TextureManager.Pixel, new Rectangle(Bounds.X + 4, Bounds.Y + 6, (int)Size.X - 8, (int)Size.Y - 6), Color.Yellow * flicker);
                    break;

                default:
                    spriteBatch.Draw(TextureManager.Pixel, Bounds, FromPlayer ? Color.Yellow : Color.Red);
                    break;
            }
        }
    }
}
