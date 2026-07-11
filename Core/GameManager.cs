using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using TheLostRobotStory.Entities;
using TheLostRobotStory.World;

namespace TheLostRobotStory.Core
{
    public class GameManager
    {

        // =====================================================
        // CURRENT GAME OBJECTS
        // =====================================================

        public Level CurrentLevel { get; private set; }

        public Player Player { get; private set; }




        // =====================================================
        // MANAGERS
        // =====================================================

        private WorldManager _worldManager;

        private ParticleManager _particles;

        private List<Projectile> _projectiles;




        // =====================================================
        // ATTACK
        // =====================================================

        private float _attackTimer;

        private const float AttackCooldown = 0.25f;





        // =====================================================
        // CONSTRUCTOR
        // =====================================================

        public GameManager(
            ParticleManager particles,
            List<Projectile> projectiles)
        {

            _particles =
                particles;


            _projectiles =
                projectiles;



            _worldManager =
                new WorldManager(
                    particles);

        }





        // =====================================================
        // LOAD GAME
        // =====================================================

        public void LoadGame(
            string levelName)
        {

            _worldManager.LoadLevel(
                levelName);



            CurrentLevel =
                _worldManager.CurrentLevel;



            Player =
                new Player(
                    CurrentLevel._spawnPoint);

            Player.SpawnPoint =
    CurrentLevel._spawnPoint;
        }







        // =====================================================
        // UPDATE
        // =====================================================

        public void Update(
    GameTime gameTime,
    InputManager input)
        {

            if (CurrentLevel == null ||
                Player == null)
                return;



            float dt =
                (float)
                gameTime.ElapsedGameTime.TotalSeconds;




            // =================================================
            // ATTACK TIMER
            // =================================================

            if (_attackTimer > 0)
            {
                _attackTimer -= dt;
            }





            // =================================================
            // UPDATE WORLD FIRST
            // =================================================

            _worldManager.Update(
                gameTime,
                Player);






            // =================================================
            // PLAYER
            // =================================================

            Player.Update(
                gameTime,
                input,
                _projectiles,
                CurrentLevel._solids,
                CurrentLevel._movingPlatforms);






            // =================================================
            // COLLISION
            // =================================================

            Player.ApplyCollision(
                CurrentLevel._solids);



            Player.ApplyPlatformCollision(
                CurrentLevel._movingPlatforms);







            // =================================================
            // COLLECTIBLES
            // =================================================

            foreach (var collectible in CurrentLevel._collectibles)
            {

                if (!collectible.IsCollected &&
                   Player.Bounds.Intersects(
                   collectible.Bounds))
                {

                    collectible.Collect(
                        Player);



                    _particles.SpawnExplosion(
                        collectible.Position,
                        Color.Yellow);

                }

            }








            // =================================================
            // HEALTH UPGRADES
            // =================================================

            foreach (var upgrade in CurrentLevel._healthUpgrades)
            {

                if (!upgrade.IsCollected &&
                   Player.Bounds.Intersects(
                   upgrade.Bounds))
                {

                    upgrade.Collect(
                        Player);



                    Player.IncreaseHealth();



                    _particles.SpawnExplosion(
                        upgrade.Position,
                        Color.Red);

                }

            }








            // =================================================
            // EVOLUTION CORES
            // =================================================

            foreach (var core in CurrentLevel._evolutionCores)
            {

                if (!core.Collected &&
                   Player.Bounds.Intersects(
                   core.Bounds))
                {

                    int oldStage =
                        Player.EvolutionStage;



                    core.Collect(
                        Player);



                    if (Player.EvolutionStage > oldStage)
                    {

                        _particles.SpawnExplosion(
                            core.Position,
                            Color.Cyan);

                    }

                }

            }









            // =================================================
            // ENEMIES
            // =================================================

            for (int i = CurrentLevel._enemies.Count - 1;
                i >= 0;
                i--)
            {

                Enemy enemy =
                    CurrentLevel._enemies[i];



                if (enemy is Boss boss)
                {

                    boss.Update(
                        CurrentLevel._solids,
                        Player,
                        _projectiles,
                        dt);

                }
                else
                {

                    enemy.Update(
                        CurrentLevel._solids,
                        Player,
                        _projectiles);

                }






                // PLAYER ATTACK

                if (Player.AttackHitbox.Intersects(
                    enemy.Bounds))
                {

                    if (_attackTimer <= 0)
                    {

                        enemy.TakeDamage(1);


                        _attackTimer =
                            AttackCooldown;



                        _particles.SpawnExplosion(
                            enemy.position,
                            Color.Orange);

                    }

                }





                if (enemy.IsDead)
                {

                    _particles.SpawnExplosion(
                        enemy.position,
                        Color.Red);



                    CurrentLevel._enemies.RemoveAt(i);

                }

            }









            // =================================================
            // PROJECTILES
            // =================================================

            for (int i = _projectiles.Count - 1;
                i >= 0;
                i--)
            {

                Projectile projectile =
                    _projectiles[i];



                projectile.Update(
                    CurrentLevel._solids);





                foreach (var enemy in CurrentLevel._enemies)
                {

                    projectile.HitEnemy(
                        enemy);

                }






                if (projectile.IsDead)
                {

                    _projectiles.RemoveAt(i);

                }

            }








            // =================================================
            // DAMAGE + DEATH
            // =================================================

            Player.CheckEnemyCollision(
                CurrentLevel._enemies);



            Player.CheckDeath();

        }








        // =====================================================
        // DRAW
        // =====================================================

        public void Draw(
            SpriteBatch spriteBatch)
        {

            if (CurrentLevel == null)
                return;



            _worldManager.Draw(
                spriteBatch);



            Player.Draw(
                spriteBatch);





            foreach (var projectile in _projectiles)
            {

                projectile.Draw(
                    spriteBatch);

            }

        }








        // =====================================================
        // LEVEL CHANGE
        // =====================================================

        public void ChangeLevel(
            string levelName)
        {

            LoadGame(
                levelName);

        }








        // =====================================================
        // RESTART
        // =====================================================

        public void RestartLevel()
        {

            Player.RespawnAtCheckpoint();

        }








        // =====================================================
        // ACCESS
        // =====================================================

        public bool HasLevelLoaded()
        {

            return CurrentLevel != null;

        }

    }
}