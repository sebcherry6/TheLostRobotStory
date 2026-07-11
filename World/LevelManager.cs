using Microsoft.Xna.Framework;
using System;
using System.IO;
using TheLostRobotStory.Core;

namespace TheLostRobotStory.World
{
    public class LevelManager
    {

        // =====================================================
        // CURRENT LEVEL
        // =====================================================

        public Level CurrentLevel { get; private set; }




        // =====================================================
        // PARTICLES
        // =====================================================

        private readonly ParticleManager _particles;





        // =====================================================
        // CONSTRUCTOR
        // =====================================================

        public LevelManager(
            ParticleManager particles)
        {

            _particles = particles
                ?? throw new ArgumentNullException(
                    nameof(particles));


            CurrentLevel =
                new Level(_particles);

        }







        // =====================================================
        // LOAD LEVEL
        // =====================================================

        public void LoadLevel(
            string levelName)
        {


            string path =
                Path.Combine(
                    AppContext.BaseDirectory,
                    "Content",
                    "Levels",
                    levelName);





            if (!File.Exists(path))
            {

                throw new FileNotFoundException(
                    "Level file not found:",
                    path);

            }





            System.Diagnostics.Debug.WriteLine(
                $"Loading level: {path}");





            // Create a fresh level
            // Keeps objects from previous level away

            CurrentLevel =
                new Level(_particles);





            CurrentLevel.Load(
                path);



            System.Diagnostics.Debug.WriteLine(
                $"Loaded {levelName}");

        }







        // =====================================================
        // SPAWN POINT
        // =====================================================

        public Vector2 GetSpawnPoint()
        {

            return CurrentLevel._spawnPoint;

        }


    }
}