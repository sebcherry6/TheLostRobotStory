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


        public Door ActiveDoor { get; private set; }


        public bool ChangeLevelRequested { get; private set; }


        public string NextLevel { get; private set; }




        // =====================================================
        // MANAGERS
        // =====================================================

        private readonly WorldManager _worldManager;

        private readonly ParticleManager _particles;

        private readonly List<Projectile> _projectiles;



        private readonly PlayerData _playerData;




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

            _particles = particles;

            _projectiles = projectiles;


            _playerData =
                new PlayerData();



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



            _playerData.Load(
                Player);

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





            if (_attackTimer > 0)
            {
                _attackTimer -= dt;
            }





            // ==============================
            // WORLD
            // ==============================

            _worldManager.Update(
                gameTime,
                Player);







            // ==============================
            // PLAYER
            // ==============================

            Player.Update(
                gameTime,
                input,
                _projectiles,
                CurrentLevel._solids,
                CurrentLevel._movingPlatforms);





            Player.ApplyCollision(
                CurrentLevel._solids,
                (float)gameTime.ElapsedGameTime.TotalSeconds);



            Player.ApplyPlatformCollision(
                CurrentLevel._movingPlatforms);







            // ==============================
            // COLLECTIBLES
            // ==============================

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







            // ==============================
            // DOORS
            // ==============================


            bool cleared =
                CurrentLevel.IsCleared();



            foreach (var door in CurrentLevel._doors)
            {

                door.CanOpen =
                    cleared;



                door.IsPlayerNear =
                    Player.Bounds.Intersects(
                        door.Bounds);



                door.Update(
                    gameTime);

            }




            ActiveDoor = null;




            foreach (var door in CurrentLevel._doors)
            {

                if (Player.Bounds.Intersects(
                    door.Bounds))
                {

                    ActiveDoor =
                        door;



                    if (door.CanOpen &&
                       input.InteractPressed())
                    {


                        _playerData.Save(
                            Player);



                        ChangeLevelRequested =
                            true;



                        NextLevel =
                            door.DestinationLevel;



                        break;

                    }

                }

            }








            // ==============================
            // EVOLUTION
            // ==============================

            foreach (var core in CurrentLevel._evolutionCores)
            {
                core.Update(gameTime);


                if (!core.Collected &&
                   Player.Bounds.Intersects(core.Bounds))
                {

                    int oldStage =
                        Player.EvolutionStage;


                    core.Collect(Player);


                    if (Player.EvolutionStage > oldStage)
                    {
                        _particles.SpawnExplosion(
                            core.Position,
                            Color.Cyan);
                    }

                }
            }








            // ==============================
            // ENEMIES
            // ==============================


            for (int i =
                CurrentLevel._enemies.Count - 1;
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
                        gameTime,
                        CurrentLevel._solids,
                        Player,
                        _projectiles);

                }





                if (Player.AttackHitbox.Intersects(
                    enemy.Bounds))
                {

                    if (_attackTimer <= 0)
                    {

                        enemy.TakeDamage(
                            1);


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



                    CurrentLevel._enemies.RemoveAt(
                        i);

                }

            }








            // ==============================
            // PROJECTILES
            // ==============================


            for (int i = _projectiles.Count - 1;
    i >= 0;
    i--)
            {

                Projectile projectile =
                    _projectiles[i];



                projectile.Update(
                    gameTime,
                    CurrentLevel._solids);



                if (projectile.FromPlayer)
                {

                    foreach (var enemy in CurrentLevel._enemies)
                    {
                        projectile.HitEnemy(enemy);
                    }

                }
                else
                {

                    projectile.HitPlayer(
                        Player);

                }



                if (projectile.IsDead)
                {
                    _projectiles.RemoveAt(i);
                }

            }


            // =================================================
            // HEALTH UPGRADES
            // =================================================

            foreach (var upgrade in CurrentLevel._healthUpgrades)
            {

                upgrade.Update(gameTime);



                if (!upgrade.IsCollected &&
                    Player.Bounds.Intersects(upgrade.Bounds))
                {

                    upgrade.Collect(Player);



                    _particles.SpawnExplosion(
                        upgrade.Position,
                        Color.Red);

                }

            }





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
        // CHANGE LEVEL
        // =====================================================

        public void ChangeLevel(
    string levelName)
        {

            PlayerData data =
                Player.GetPlayerData();



            _worldManager.LoadLevel(
                levelName);



            CurrentLevel =
                _worldManager.CurrentLevel;



            Player.position =
                CurrentLevel._spawnPoint;



            Player.LoadPlayerData(
                data);



            Player.SpawnPoint =
                CurrentLevel._spawnPoint;



            _projectiles.Clear();

        }









        // =====================================================
        // RESTART
        // =====================================================

        public void RestartLevel()
        {

            Player.RespawnAtCheckpoint();

        }






        public void ClearLevelRequest()
        {

            ChangeLevelRequested =
                false;


            NextLevel =
                null;

        }





        public bool HasLevelLoaded()
        {

            return CurrentLevel != null;

        }

    }
}