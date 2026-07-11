using Microsoft.Xna.Framework.Input;

namespace TheLostRobotStory.Core
{
    public class InputManager
    {

        // =====================================================
        // KEYBOARD STATES
        // =====================================================

        public KeyboardState CurrentKeyboard { get; private set; }

        public KeyboardState PreviousKeyboard { get; private set; }



        // =====================================================
        // UPDATE
        // =====================================================

        public void Update()
        {

            PreviousKeyboard =
                CurrentKeyboard;


            CurrentKeyboard =
                Keyboard.GetState();

        }





        // =====================================================
        // KEY CHECKS
        // =====================================================


        // Key is currently held down
        public bool IsKeyDown(
            Keys key)
        {

            return CurrentKeyboard.IsKeyDown(key);

        }







        // Key was pressed this frame
        public bool KeyPressed(
            Keys key)
        {

            return CurrentKeyboard.IsKeyDown(key)
                &&
                PreviousKeyboard.IsKeyUp(key);

        }







        // Key was released this frame
        public bool KeyReleased(
            Keys key)
        {

            return CurrentKeyboard.IsKeyUp(key)
                &&
                PreviousKeyboard.IsKeyDown(key);

        }





        // =====================================================
        // COMMON GAME INPUTS
        // =====================================================


        public bool Left()
        {

            return IsKeyDown(Keys.A);

        }



        public bool Right()
        {

            return IsKeyDown(Keys.D);

        }



        public bool JumpPressed()
        {

            return KeyPressed(Keys.Space);

        }



        public bool AttackPressed()
        {

            return KeyPressed(Keys.J);

        }



        public bool ShootPressed()
        {

            return KeyPressed(Keys.K);

        }



        public bool InteractPressed()
        {

            return KeyPressed(Keys.E);

        }



        public bool ExitPressed()
        {

            return KeyPressed(Keys.Escape);

        }

    }
}