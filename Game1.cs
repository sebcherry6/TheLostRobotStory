using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TheLostRobotStory.Core;
using TheLostRobotStory.Scenes;
using TheLostRobotStory.Story;

namespace TheLostRobotStory
{
    // Game1 is now only the "host": it loads shared resources and runs
    // the scene stack. Gameplay code moved to Scenes/GameplayScene.cs.
    public class Game1 : Game
    {
        public static readonly Color BackgroundColor = new Color(16, 18, 26);

        private readonly GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;
        private InputManager _input;
        private SceneManager _scenes;

        public GameSettings Settings { get; private set; } = new GameSettings();

        public Game1()
        {
            _graphics = new GraphicsDeviceManager(this)
            {
                PreferredBackBufferWidth = 1280,
                PreferredBackBufferHeight = 720
            };

            Content.RootDirectory = "Content";
            IsMouseVisible = false;
        }

        protected override void Initialize()
        {
            Window.Title = "The Lost Robot Story";
            _input = new InputManager();
            base.Initialize();
        }

        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);

            TextureManager.Load(Content, GraphicsDevice);
            AudioManager.Initialize(Content);
            AudioManager.Preload();
            StoryData.Load();
            LevelDatabase.Load();

            ApplySettings(SaveSystem.LoadSettings());

            SpriteFont font = Content.Load<SpriteFont>("UIFont");

            _scenes = new SceneManager(this, font);
            _scenes.Push(new MainMenuScene());
        }

        public void ApplySettings(GameSettings settings)
        {
            Settings = settings;

            AudioManager.SetVolumes(settings.MusicVolume, settings.SfxVolume);

            if (_graphics.IsFullScreen != settings.Fullscreen)
            {
                _graphics.IsFullScreen = settings.Fullscreen;
                _graphics.ApplyChanges();
            }
        }

        protected override void Update(GameTime gameTime)
        {
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

            _input.Update();
            AudioManager.Update(dt);
            _scenes.Update(gameTime, _input);

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(BackgroundColor);

            _scenes.Draw(_spriteBatch);

            base.Draw(gameTime);
        }
    }
}
