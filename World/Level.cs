using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using System.IO;
using TheLostRobotStory.Entities;
using TheLostRobotStory.Core;

namespace TheLostRobotStory.World
{
    public class Level
    {
        public List<Rectangle> _solids = new();
        public List<Enemy> _enemies = new();
        public List<Door> _doors = new();

        public Vector2 _spawnPoint;

        private const int _tileSize = 32;

        public void Load(string filePath)
        {
            _solids.Clear();
            _enemies.Clear();
            _doors.Clear();

            string[] lines = File.ReadAllLines(filePath);

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
                        // WALL
                        case '#':
                            _solids.Add(new Rectangle(
                                (int)worldPos.X,
                                (int)worldPos.Y,
                                _tileSize,
                                _tileSize));
                            break;

                        // PLAYER SPAWN
                        case 'P':
                            _spawnPoint = worldPos;
                            break;

                        // ENEMY
                        case 'E':
                            _enemies.Add(new Enemy(worldPos));
                            break;

                        // DOOR
                        case 'D':
                            _doors.Add(new Door(worldPos, "level2.txt"));
                            break;
                    }
                }
            }
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            // SOLIDS
            foreach (var tile in _solids)
            {
                spriteBatch.Draw(TextureManager.Pixel, tile, Color.DarkGray);
            }

            // DOORS
            foreach (var door in _doors)
            {
                door.Draw(spriteBatch);
            }

            // ENEMIES
            foreach (var enemy in _enemies)
            {
                enemy.Draw(spriteBatch);
            }
        }
    }
}