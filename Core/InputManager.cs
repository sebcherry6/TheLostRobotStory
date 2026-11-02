using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace TheLostRobotStory.Core
{
    // Keyboard + gamepad input. Gameplay and menu actions are named
    // so scenes never check raw keys (makes rebinding easy later).
    public class InputManager
    {
        public KeyboardState CurrentKeyboard { get; private set; }
        public KeyboardState PreviousKeyboard { get; private set; }

        public GamePadState CurrentPad { get; private set; }
        public GamePadState PreviousPad { get; private set; }

        private const float StickThreshold = 0.5f;

        public void Update()
        {
            PreviousKeyboard = CurrentKeyboard;
            CurrentKeyboard = Keyboard.GetState();

            PreviousPad = CurrentPad;
            CurrentPad = GamePad.GetState(PlayerIndex.One);
        }

        // =====================================================
        // RAW CHECKS
        // =====================================================

        public bool IsKeyDown(Keys key) =>
            CurrentKeyboard.IsKeyDown(key);

        public bool KeyPressed(Keys key) =>
            CurrentKeyboard.IsKeyDown(key) &&
            PreviousKeyboard.IsKeyUp(key);

        public bool KeyReleased(Keys key) =>
            CurrentKeyboard.IsKeyUp(key) &&
            PreviousKeyboard.IsKeyDown(key);

        public bool ButtonDown(Buttons button) =>
            CurrentPad.IsConnected &&
            CurrentPad.IsButtonDown(button);

        public bool ButtonPressed(Buttons button) =>
            CurrentPad.IsConnected &&
            CurrentPad.IsButtonDown(button) &&
            PreviousPad.IsButtonUp(button);

        private float StickX => CurrentPad.IsConnected ? CurrentPad.ThumbSticks.Left.X : 0f;

        private static bool Crossed(float previous, float current, float threshold) =>
            threshold > 0
                ? current > threshold && previous <= threshold
                : current < threshold && previous >= threshold;

        private bool StickFlick(bool horizontal, float threshold)
        {
            if (!CurrentPad.IsConnected)
                return false;

            float prev = horizontal ? PreviousPad.ThumbSticks.Left.X : PreviousPad.ThumbSticks.Left.Y;
            float cur = horizontal ? CurrentPad.ThumbSticks.Left.X : CurrentPad.ThumbSticks.Left.Y;

            return Crossed(prev, cur, threshold);
        }

        // =====================================================
        // GAMEPLAY
        // =====================================================

        public bool Left() =>
            IsKeyDown(Keys.A) || IsKeyDown(Keys.Left) ||
            ButtonDown(Buttons.DPadLeft) || StickX < -StickThreshold;

        public bool Right() =>
            IsKeyDown(Keys.D) || IsKeyDown(Keys.Right) ||
            ButtonDown(Buttons.DPadRight) || StickX > StickThreshold;

        public bool JumpPressed() =>
            KeyPressed(Keys.Space) || ButtonPressed(Buttons.A);

        // still holding jump (variable jump height)
        public bool JumpHeld() =>
            IsKeyDown(Keys.Space) || ButtonDown(Buttons.A);

        public bool AttackPressed() =>
            KeyPressed(Keys.J) || ButtonPressed(Buttons.X);

        public bool ShootPressed() =>
            KeyPressed(Keys.K) || ButtonPressed(Buttons.Y) || ButtonPressed(Buttons.RightTrigger);

        public bool DashPressed() =>
            KeyPressed(Keys.L) || KeyPressed(Keys.LeftShift) || ButtonPressed(Buttons.RightShoulder);

        public bool InteractPressed() =>
            KeyPressed(Keys.E) || ButtonPressed(Buttons.B);

        public bool PausePressed() =>
            KeyPressed(Keys.Escape) || KeyPressed(Keys.P) || ButtonPressed(Buttons.Start);

        // kept so older code still compiles
        public bool ExitPressed() => PausePressed();

        // =====================================================
        // MENUS
        // =====================================================

        public bool MenuUp() =>
            KeyPressed(Keys.W) || KeyPressed(Keys.Up) ||
            ButtonPressed(Buttons.DPadUp) || StickFlick(false, StickThreshold);

        public bool MenuDown() =>
            KeyPressed(Keys.S) || KeyPressed(Keys.Down) ||
            ButtonPressed(Buttons.DPadDown) || StickFlick(false, -StickThreshold);

        public bool MenuLeft() =>
            KeyPressed(Keys.A) || KeyPressed(Keys.Left) ||
            ButtonPressed(Buttons.DPadLeft) || StickFlick(true, -StickThreshold);

        public bool MenuRight() =>
            KeyPressed(Keys.D) || KeyPressed(Keys.Right) ||
            ButtonPressed(Buttons.DPadRight) || StickFlick(true, StickThreshold);

        public bool Confirm() =>
            KeyPressed(Keys.Enter) || KeyPressed(Keys.Space) || KeyPressed(Keys.E) ||
            ButtonPressed(Buttons.A);

        public bool Back() =>
            KeyPressed(Keys.Escape) || KeyPressed(Keys.Back) ||
            ButtonPressed(Buttons.B) || ButtonPressed(Buttons.Start);
    }
}
