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
        public List<Rectangle> _solids = new();
        public List<Collectible> _collectibles = new();
        public List<Enemy> _enemies = new();
        public List<Door> _doors = new();

        public Vector2 _spawnPoint;

        public string CurrentLevelName;

        private const int _tileSize = 32;

        // ==========================================
        // LOAD LEVEL
        // ==========================================
        public void Load(string filePath)
        {
            _solids.Clear();
            _collectibles.Clear();
            _enemies.Clear();
            _doors.Clear();

            CurrentLevelName = Path.GetFileName(filePath);

            string[] lines = File.ReadAllLines(filePath);

            int enemyIndex = 0;

            for (int y = 0; y < lines.Length; y++)
            {
                for (int x = 0; x < lines[y].Length; x++)
                {
                    char tile = lines[y][x];

                    Vector2 worldPos = new Vector2(
                        x * _tileSize,
                        y * _tileSize
                    );

                    switch (tile)
                    {
                        // =========================
                        // SOLID
                        // =========================
                        case '#':
                            _solids.Add(new Rectangle(
                                (int)worldPos.X,
                                (int)worldPos.Y,
                                _tileSize,
                                _tileSize));
                            break;

                        // =========================
                        // PLAYER SPAWN
                        // =========================
                        case 'P':
                            _spawnPoint = worldPos;
                            break;

                        // =========================
                        // ENEMY
                        // =========================
                        case 'E':
                            _enemies.Add(
                                new Enemy(worldPos,
                                GetEnemyType(CurrentLevelName, enemyIndex))
                            );

                            enemyIndex++;
                            break;

                        // =========================
                        // BOSS
                        // =========================
                        case 'B':
                            _enemies.Add(new Boss(worldPos));
                            break;

                        // =========================
                        // DOOR
                        // =========================
                        case 'D':
                            _doors.Add(
                                new Door(
                                    worldPos,
                                    GetNextLevel(CurrentLevelName)
                                )
                            );
                            break;

                        // =========================
                        // COLLECTIBLE
                        // =========================
                        case 'O':
                            _collectibles.Add(new Collectible(worldPos));
                            break;
                    }
                }
            }
        }

        // ==========================================
        // ENEMY TYPES PER LEVEL
        // ==========================================
        private EnemyType GetEnemyType(string levelName, int index)
        {
            switch (levelName)
            {
                case "level1.txt":
                    return EnemyType.Normal;

                case "level2.txt":
                    return (index % 2 == 0)
                        ? EnemyType.Normal
                        : EnemyType.Fast;

                case "level3.txt":
                    {
                        int value = index % 4;

                        if (value == 0)
                            return EnemyType.Tank;

                        if (value == 1)
                            return EnemyType.Fast;

                        if (value == 2)
                            return EnemyType.Normal;

                        return EnemyType.Laser;
                    }

                case "level4.txt":
                    {
                        int value = index % 4;

                        if (value == 0)
                            return EnemyType.Laser;

                        if (value == 1)
                            return EnemyType.Tank;

                        if (value == 2)
                            return EnemyType.Fast;

                        return EnemyType.Normal;
                    }

                default:
                    return EnemyType.Normal;
            }
        }

        // ==========================================
        // NEXT LEVEL
        // ==========================================
        private string GetNextLevel(string currentLevel)
        {
            switch (currentLevel)
            {
                case "level1.txt":
                    return "level2.txt";

                case "level2.txt":
                    return "level3.txt";

                case "level3.txt":
                    return "level4.txt";

                default:
                    return "level1.txt";
            }
        }

        // ==========================================
        // DRAW
        // ==========================================
        public void Draw(SpriteBatch spriteBatch)
        {
            foreach (var tile in _solids)
                spriteBatch.Draw(TextureManager.Pixel, tile, Color.DarkGray);

            foreach (var collectible in _collectibles)
                collectible.Draw(spriteBatch);

            foreach (var door in _doors)
                door.Draw(spriteBatch);

            foreach (var enemy in _enemies)
                enemy.Draw(spriteBatch);
        }

        // ==========================================
        // LEVEL COMPLETE?
        // ==========================================
        public bool IsCleared()
        {
            foreach (var collectible in _collectibles)
            {
                if (!collectible.IsCollected)
                    return false;
            }

            foreach (var enemy in _enemies)
            {
                if (!enemy.IsDead)
                    return false;
            }

            return true;
        }
    }
}