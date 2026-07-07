using System.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;

namespace TheLostRobotStory.World
{
    public class LevelManager
    {
        public Level CurrentLevel { get; private set; }
        public int TargetEvolutionStage;

        public LevelManager()
        {
            CurrentLevel = new Level();
        }

        public void LoadLevel(string levelName)
        {
            string path = Path.Combine(
                AppContext.BaseDirectory,
                "Content",
                "Levels",
                levelName);

            System.Diagnostics.Debug.WriteLine($"Loading level: {path}");

            // Create a fresh level every time (important!)
            CurrentLevel = new Level();

            CurrentLevel.Load(path);
        }

        public Vector2 GetSpawnPoint()
        {
            return CurrentLevel._spawnPoint;
        }
    }
}