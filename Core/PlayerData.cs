using Microsoft.Xna.Framework;

namespace TheLostRobotStory.Core

{
    public class PlayerData
    {

        // ==============================
        // PLAYER PROGRESSION
        // ==============================

        public int EvolutionStage { get; set; }


        // ==============================
        // HEALTH
        // ==============================

        public int Health { get; set; }

        public int MaxHealth { get; set; }

        public bool HasShownShootMessage;

        public bool HasShownDoubleJumpMessage;


        public Vector2 SpawnPoint;

        // ==============================
        // CONSTRUCTOR
        // ==============================

        public PlayerData()
        {

            EvolutionStage = 0;


            MaxHealth = 3;

            Health = MaxHealth;

        }



        // ==============================
        // SAVE FROM PLAYER
        // ==============================

        public void Save(
            Entities.Player player)
        {

            EvolutionStage =
                player.EvolutionStage;


            Health =
                player.Health;


            MaxHealth =
                player.MaxHealth;

        }





        // ==============================
        // LOAD INTO PLAYER
        // ==============================

        public void Load(
            Entities.Player player)
        {

            player.SetEvolutionStage(
                EvolutionStage);



            player.Health =
                Health;


            player.MaxHealth =
                MaxHealth;

        }

    }
}