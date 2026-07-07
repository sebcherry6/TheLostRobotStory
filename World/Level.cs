using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using System.IO;
using TheLostRobotStory.Core;
using TheLostRobotStory.Entities;

namespace TheLostRobotStory.World
{
    public class Level
    {

        // =====================================================
        // OBJECT LISTS
        // =====================================================

        public List<Rectangle> _solids = new();

        // 🟡 Energy crystals (required for door)
        public List<Collectible> _collectibles = new();


        // Enemies
        public List<Enemy> _enemies = new();


        // Doors
        public List<Door> _doors = new();


        // 🔵 Evolution upgrades
        public List<EvolutionCore> _evolutionCores = new();


        // Checkpoints
        public List<Checkpoint> _checkpoints = new();


        // ❤️ Health upgrades
        public List<HealthUpgrade> _healthUpgrades = new();




        // =====================================================
        // PLAYER SPAWN
        // =====================================================

        public Vector2 _spawnPoint;



        public string CurrentLevelName;



        private const int _tileSize = 32;






        // =====================================================
        // HUD COUNTERS
        // =====================================================


        // Energy crystals needed to open door
        public int CollectiblesRemaining
        {
            get
            {
                int count = 0;


                foreach (var item in _collectibles)
                {
                    if (!item.IsCollected)
                        count++;
                }


                return count;
            }
        }





        // Enemies blocking door
        public int EnemiesRemaining
        {
            get
            {
                int count = 0;


                foreach (var enemy in _enemies)
                {
                    if (!enemy.IsDead)
                        count++;
                }


                return count;
            }
        }





        // Optional HUD information
        public int HealthUpgradesRemaining
        {
            get
            {
                int count = 0;


                foreach (var item in _healthUpgrades)
                {
                    if (!item.IsCollected)
                        count++;
                }


                return count;
            }
        }





        // =====================================================
        // LOAD LEVEL
        // =====================================================

        public void Load(string filePath)
        {

            _solids.Clear();

            _collectibles.Clear();

            _enemies.Clear();

            _doors.Clear();

            _evolutionCores.Clear();

            _checkpoints.Clear();

            _healthUpgrades.Clear();



            CurrentLevelName =
                Path.GetFileName(filePath);




            string[] lines =
                File.ReadAllLines(filePath);



            int enemyIndex = 0;




            for (int y = 0; y < lines.Length; y++)
            {

                if (string.IsNullOrWhiteSpace(lines[y]))
                    continue;



                for (int x = 0; x < lines[y].Length; x++)
                {

                    char tile =
                        lines[y][x];



                    Vector2 worldPos =
                        new Vector2(
                            x * _tileSize,
                            y * _tileSize);




                    switch (tile)
                    {


                        // =============================
                        // SOLID TILE
                        // =============================

                        case '#':

                            _solids.Add(
                                new Rectangle(
                                    (int)worldPos.X,
                                    (int)worldPos.Y,
                                    _tileSize,
                                    _tileSize));

                            break;




                        // =============================
                        // PLAYER SPAWN
                        // =============================

                        case 'P':

                            _spawnPoint =
                                worldPos;

                            break;





                        // =============================
                        // ENEMY
                        // =============================

                        case 'E':

                            _enemies.Add(
                                new Enemy(
                                    worldPos,
                                    GetEnemyType(
                                        CurrentLevelName,
                                        enemyIndex)));

                            enemyIndex++;

                            break;





                        // =============================
                        // BOSS
                        // =============================

                        case 'B':

                            _enemies.Add(
                                new Boss(
                                    worldPos));

                            break;





                        // =============================
                        // DOOR
                        // =============================

                        case 'D':

                            _doors.Add(
                                new Door(
                                    worldPos,
                                    GetNextLevel(
                                        CurrentLevelName)));

                            break;




                        // =============================
                        // ENERGY CRYSTAL 🟡
                        // =============================

                        case 'O':

                            _collectibles.Add(
                                new Collectible(
                                    worldPos,
                                    CollectibleType.EnergyCrystal));

                            break;




                        // =============================
                        // EVOLUTION CORE 🔵
                        // =============================

                        case 'X':
                            {

                                int stage = 1;



                                if (CurrentLevelName == "level3.txt")
                                    stage = 2;



                                if (CurrentLevelName == "level4.txt")
                                    stage = 3;




                                _evolutionCores.Add(
                                    new EvolutionCore(
                                        worldPos,
                                        stage));

                                break;
                            }
                        // =============================
                        // CHECKPOINT
                        // =============================

                        case 'C':

                            _checkpoints.Add(
                                new Checkpoint(
                                    worldPos));

                            break;





                        // =============================
                        // HEALTH UPGRADE ❤️
                        // =============================

                        case 'H':

                            _healthUpgrades.Add(
                                new HealthUpgrade(
                                    worldPos));

                            break;


                    }

                }

            }

        }








        // =====================================================
        // ENEMY TYPES
        // =====================================================

        private EnemyType GetEnemyType(
            string levelName,
            int index)
        {

            switch (levelName)
            {

                case "level1.txt":

                    return EnemyType.Normal;




                case "level2.txt":

                    if (index % 2 == 0)
                        return EnemyType.Normal;

                    else
                        return EnemyType.Fast;





                case "level3.txt":

                    switch (index % 4)
                    {

                        case 0:
                            return EnemyType.Tank;


                        case 1:
                            return EnemyType.Fast;


                        case 2:
                            return EnemyType.Normal;


                        default:
                            return EnemyType.Laser;

                    }





                case "level4.txt":

                    switch (index % 4)
                    {

                        case 0:
                            return EnemyType.Laser;


                        case 1:
                            return EnemyType.Tank;


                        case 2:
                            return EnemyType.Fast;


                        default:
                            return EnemyType.Normal;

                    }




                default:

                    return EnemyType.Normal;

            }

        }









        // =====================================================
        // LEVEL FLOW
        // =====================================================

        private string GetNextLevel(
            string current)
        {

            if (current == "level1.txt")
                return "level2.txt";



            if (current == "level2.txt")
                return "level3.txt";



            if (current == "level3.txt")
                return "level4.txt";



            return "level1.txt";

        }
        // =====================================================
        // DRAW LEVEL
        // =====================================================

        public void Draw(
            SpriteBatch spriteBatch)
        {


            // =============================
            // SOLID TILES
            // =============================

            foreach (var tile in _solids)
            {

                spriteBatch.Draw(
                    TextureManager.Pixel,
                    tile,
                    Color.DarkGray);

            }




            // =============================
            // ENERGY CRYSTALS 🟡
            // =============================

            foreach (var item in _collectibles)
            {

                item.Draw(spriteBatch);

            }





            // =============================
            // EVOLUTION CORES 🔵
            // =============================

            foreach (var core in _evolutionCores)
            {

                core.Draw(spriteBatch);

            }





            // =============================
            // HEALTH UPGRADES ❤️
            // =============================

            foreach (var health in _healthUpgrades)
            {

                health.Draw(spriteBatch);

            }





            // =============================
            // CHECKPOINTS
            // =============================

            foreach (var checkpoint in _checkpoints)
            {

                checkpoint.Draw(spriteBatch);

            }





            // =============================
            // DOORS
            // =============================

            foreach (var door in _doors)
            {

                door.Draw(spriteBatch);

            }





            // =============================
            // ENEMIES + BOSS
            // =============================

            foreach (var enemy in _enemies)
            {

                enemy.Draw(spriteBatch);

            }

        }









        // =====================================================
        // DOOR REQUIREMENT CHECK
        // =====================================================

        public bool IsCleared()
        {

            // Need all energy crystals

            if (CollectiblesRemaining > 0)
                return false;



            // Need all enemies defeated

            if (EnemiesRemaining > 0)
                return false;



            return true;

        }


    }
}