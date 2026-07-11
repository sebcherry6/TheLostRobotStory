using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using TheLostRobotStory.Core;
using TheLostRobotStory.Entities;

namespace TheLostRobotStory.World
{
    public class WorldManager
    {

        // =====================================================
        // CURRENT LEVEL
        // =====================================================

        public Level CurrentLevel { get; private set; }




        // =====================================================
        // DEPENDENCIES
        // =====================================================

        private LevelManager _levelManager;

        private ParticleManager _particles;





        // =====================================================
        // CONSTRUCTOR
        // =====================================================

        public WorldManager(
            ParticleManager particles)
        {

            _particles = particles;


            _levelManager =
                new LevelManager(
                    _particles);

        }





        // =====================================================
        // LOAD LEVEL
        // =====================================================

        public void LoadLevel(
            string levelName)
        {

            _levelManager.LoadLevel(
                levelName);


            CurrentLevel =
                _levelManager.CurrentLevel;

        }





        // =====================================================
        // UPDATE WORLD
        // =====================================================

        public void Update(
            GameTime gameTime,
            Player player)
        {

            if (CurrentLevel == null)
                return;



            // =============================================
            // MOVING PLATFORMS
            // =============================================

            foreach (var platform in CurrentLevel._movingPlatforms)
            {

                platform.Update(
                    gameTime);

            }




            // =============================================
            // EVOLUTION CORES
            // =============================================

            foreach (var core in CurrentLevel._evolutionCores)
            {

                core.Update(
                    gameTime);

            }





            // =============================================
            // ACID POOLS
            // =============================================

            foreach (var acid in CurrentLevel._acidPools)
            {

                acid.Update(
                    gameTime,
                    player);

            }





            // =============================================
            // LASER GATES
            // =============================================

            foreach (var laser in CurrentLevel._laserGates)
            {

                laser.Update(
                    gameTime,
                    player);

            }





            // =============================================
            // CHECKPOINTS
            // =============================================

            foreach (var checkpoint in CurrentLevel._checkpoints)
            {

                if (player.Bounds.Intersects(
                    checkpoint.Bounds))
                {

                    checkpoint.Activate(
                        player);

                }

            }





            // =============================================
            // HEALTH UPGRADES
            // =============================================

            foreach (var health in CurrentLevel._healthUpgrades)
            {

                health.Update(
                    gameTime);

            }





            // =============================================
            // COLLECTIBLES
            // =============================================

            foreach (var collectible in CurrentLevel._collectibles)
            {

                collectible.Update(
                    gameTime);

            }

        }





        // =====================================================
        // DRAW WORLD
        // =====================================================

        public void Draw(
            SpriteBatch spriteBatch)
        {

            if (CurrentLevel == null)
                return;



            CurrentLevel.Draw(
                spriteBatch);

        }





        // =====================================================
        // LEVEL INFORMATION
        // =====================================================

        public int CollectiblesRemaining
        {

            get
            {

                if (CurrentLevel == null)
                    return 0;


                return CurrentLevel
                    .CollectiblesRemaining;

            }

        }




        public int EnemiesRemaining
        {

            get
            {

                if (CurrentLevel == null)
                    return 0;


                return CurrentLevel
                    .EnemiesRemaining;

            }

        }





        public bool IsLevelCleared()
        {

            if (CurrentLevel == null)
                return false;


            return CurrentLevel.IsCleared();

        }





        // =====================================================
        // GET SPAWN POINT
        // =====================================================

        public Vector2 SpawnPoint
        {

            get
            {

                return CurrentLevel._spawnPoint;

            }

        }

    }
}