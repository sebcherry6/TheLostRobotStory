using System;
using Microsoft.Xna.Framework.Content;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Graphics;

namespace TheLostRobotStory.Core
{
    public static class TextureManager
    {

        // =====================================================
        // BASIC TEXTURES
        // =====================================================

        public static Texture2D Pixel { get; private set; }



        // =====================================================
        // ENTITIES
        // =====================================================

        public static Texture2D Player { get; private set; }

        public static Texture2D Enemy { get; private set; }

        public static Texture2D Boss { get; private set; }

        public static Texture2D Projectile { get; private set; }



        // =====================================================
        // WORLD OBJECTS
        // =====================================================

        public static Texture2D Tile { get; private set; }

        public static Texture2D Door { get; private set; }

        public static Texture2D Crystal { get; private set; }

        public static Texture2D EvolutionCore { get; private set; }

        public static Texture2D HealthUpgrade { get; private set; }



        // =====================================================
        // OBSTACLES
        // =====================================================

        public static Texture2D Acid { get; private set; }

        public static Texture2D Laser { get; private set; }

        public static Texture2D MovingPlatform { get; private set; }

        public static Texture2D Checkpoint { get; private set; }

        // Optional per-type / per-state textures (fall back to the ones above)
        public static Texture2D EnemyNormal { get; private set; }
        public static Texture2D EnemyFast { get; private set; }
        public static Texture2D EnemyTank { get; private set; }
        public static Texture2D EnemyLaser { get; private set; }
        public static Texture2D ProjectileEnemy { get; private set; }
        public static Texture2D TileTop { get; private set; }
        public static Texture2D Terminal { get; private set; }

        private static ContentManager _content;
        private static readonly Dictionary<string, Texture2D> _cache = new();





        // =====================================================
        // LOAD ALL TEXTURES
        // =====================================================

        public static void Load(
            ContentManager content,
            GraphicsDevice graphicsDevice)
        {


            // =================================================
            // CREATE PIXEL TEXTURE
            // =================================================

            Pixel =
                new Texture2D(
                    graphicsDevice,
                    1,
                    1);



            Pixel.SetData(
                new[]
                {
                    Microsoft.Xna.Framework.Color.White
                });





            // =================================================
            // LOAD GAME TEXTURES
            // =================================================
            //
            // Uncomment these when you add images
            //
            // Example:
            //
            // Player =
            //     content.Load<Texture2D>("Textures/player");
            //
            //
            // File:
            // Content/Textures/player.png
            //



            // Every texture is optional: drop the PNG into Content/Textures,
            // add it in the MGCB editor and it is picked up automatically.
            // Missing files simply return null (placeholder rectangles are drawn).
            Player = TryLoad(content, "Textures/player");
            Enemy = TryLoad(content, "Textures/enemy");
            Boss = TryLoad(content, "Textures/boss");
            Projectile = TryLoad(content, "Textures/projectile");
            Tile = TryLoad(content, "Textures/tile");
            Door = TryLoad(content, "Textures/door");
            Crystal = TryLoad(content, "Textures/crystal");
            EvolutionCore = TryLoad(content, "Textures/evolution");
            HealthUpgrade = TryLoad(content, "Textures/health");
            Acid = TryLoad(content, "Textures/acid");
            Laser = TryLoad(content, "Textures/laser");
            MovingPlatform = TryLoad(content, "Textures/platform");
            Checkpoint = TryLoad(content, "Textures/checkpoint");

            EnemyNormal = TryLoad(content, "Textures/enemy_normal");
            EnemyFast = TryLoad(content, "Textures/enemy_fast");
            EnemyTank = TryLoad(content, "Textures/enemy_tank");
            EnemyLaser = TryLoad(content, "Textures/enemy_laser");
            ProjectileEnemy = TryLoad(content, "Textures/projectile_enemy");
            TileTop = TryLoad(content, "Textures/tile_top");
            Terminal = TryLoad(content, "Textures/terminal");

            _content = content;
        }

        // Load any texture by name on demand (cached, null if missing).
        // Used for things like Textures/Backgrounds/level1_far.
        public static Texture2D Get(string assetName)
        {
            if (_cache.TryGetValue(assetName, out Texture2D cached))
                return cached;

            Texture2D texture =
                _content == null ? null : TryLoad(_content, "Textures/" + assetName);

            _cache[assetName] = texture;
            return texture;
        }

        private static Texture2D TryLoad(
            ContentManager content,
            string assetName)
        {
            try
            {
                return content.Load<Texture2D>(assetName);
            }
            catch (Exception)
            {
                return null;
            }

        }

    }
}