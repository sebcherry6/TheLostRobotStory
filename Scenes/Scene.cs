using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TheLostRobotStory.Core;

namespace TheLostRobotStory.Scenes
{
    // A screen of the game: main menu, gameplay, pause, dialogue...
    // Each scene owns its own SpriteBatch.Begin/End calls.
    public abstract class Scene
    {
        protected SceneManager Scenes { get; private set; }

        // Overlay scenes (pause, dialogue) draw on top of the scene below
        // and freeze it, because only the top scene is updated.
        public virtual bool IsOverlay => false;

        internal void Attach(SceneManager manager) => Scenes = manager;

        public virtual void OnEnter() { }
        public virtual void OnExit() { }

        public abstract void Update(GameTime gameTime, InputManager input);
        public abstract void Draw(SpriteBatch spriteBatch);
    }

    // Stack of scenes. Changes are queued and applied between frames so a
    // scene can safely push/pop while it is being updated.
    public class SceneManager
    {
        private readonly List<Scene> _stack = new();
        private readonly List<Action> _pending = new();

        public Game1 Game { get; }
        public SpriteFont Font { get; }

        public GraphicsDevice GraphicsDevice => Game.GraphicsDevice;
        public int ScreenWidth => GraphicsDevice.Viewport.Width;
        public int ScreenHeight => GraphicsDevice.Viewport.Height;
        public Rectangle Screen => new Rectangle(0, 0, ScreenWidth, ScreenHeight);

        public bool IsEmpty => _stack.Count == 0 && _pending.Count == 0;

        public SceneManager(Game1 game, SpriteFont font)
        {
            Game = game;
            Font = font;
        }

        public void Push(Scene scene) => _pending.Add(() =>
        {
            scene.Attach(this);
            _stack.Add(scene);
            scene.OnEnter();
        });

        public void Pop() => _pending.Add(() =>
        {
            if (_stack.Count == 0)
                return;

            Scene top = _stack[^1];
            _stack.RemoveAt(_stack.Count - 1);
            top.OnExit();
        });

        // Remove every scene, then show this one (e.g. "Quit to menu")
        public void ClearAndPush(Scene scene)
        {
            _pending.Add(() =>
            {
                for (int i = _stack.Count - 1; i >= 0; i--)
                    _stack[i].OnExit();

                _stack.Clear();
            });

            Push(scene);
        }

        public void Update(GameTime gameTime, InputManager input)
        {
            ApplyPending();

            if (_stack.Count > 0)
                _stack[^1].Update(gameTime, input);

            ApplyPending();
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            if (_stack.Count == 0)
                return;

            // find the lowest scene that is visible under the overlays
            int first = _stack.Count - 1;

            while (first > 0 && _stack[first].IsOverlay)
                first--;

            for (int i = first; i < _stack.Count; i++)
                _stack[i].Draw(spriteBatch);
        }

        private void ApplyPending()
        {
            while (_pending.Count > 0)
            {
                Action action = _pending[0];
                _pending.RemoveAt(0);
                action();
            }
        }
    }
}
