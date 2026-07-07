using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using TheLostRobotStory.Core;
using TheLostRobotStory.Entities;
using TheLostRobotStory.World;

namespace TheLostRobotStory
{
    public class Game1 : Game
    {
        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;

        private LevelManager _levelManager;
        private Level _level;
        private Player _player;
        private HUD _hud;

        private List<Projectile> _projectiles = new();

        private SpriteFont _uiFont;
        private ParticleManager _particles = new ParticleManager();

        private Door _activeDoor;

        // TRANSITION
        private bool _isTransitioning;
        private bool _levelLoaded;
        private float _fade;

        // INPUT
        private KeyboardState _keyboard;
        private KeyboardState _previousKeyboard;

        // SHAKE
        private float _shakeTimer;
        private float _shakeMagnitude;
        private Vector2 _shakeOffset;

        private Camera _camera;

        public Game1()
        {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
        }

        protected override void Initialize()
        {
            _camera = new Camera();
            _levelManager = new LevelManager();
            _hud = new HUD();

            Boss.BossSlamEvent += () =>
            {
                StartShake(0.25f, 6f);
            };

            base.Initialize();
        }

        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);

            TextureManager.Pixel = new Texture2D(GraphicsDevice, 1, 1);
            TextureManager.Pixel.SetData(new[] { Color.White });

            _uiFont = Content.Load<SpriteFont>("UIFont");

            _levelManager.LoadLevel("level1.txt");
            _level = _levelManager.CurrentLevel;

            _player = new Player(_level._spawnPoint);
        }

        protected override void Update(GameTime gameTime)
        {
            if (Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();

            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

            // IMPORTANT INPUT ORDER FIX
            _previousKeyboard = _keyboard;
            _keyboard = Keyboard.GetState();

            // =========================
            // TRANSITION
            // =========================
            if (_isTransitioning)
            {
                _fade += 0.05f;

                if (_fade >= 1f && !_levelLoaded)
                {
                    _levelManager.LoadLevel(_activeDoor.DestinationLevel);
                    _level = _levelManager.CurrentLevel;

                    _player.position = _level._spawnPoint;
                    _levelLoaded = true;
                }

                if (_fade >= 2f)
                {
                    _fade = 0f;
                    _isTransitioning = false;
                    _levelLoaded = false;
                }

                return;
            }

            // =========================
            // PLAYER
            // =========================
            _player.Update(gameTime, _keyboard, _previousKeyboard, _projectiles);
            _player.ApplyCollision(_level._solids);

            // =========================
            // COLLECTIBLES
            // =========================
            foreach (var c in _level._collectibles)
            {
                if (!c.IsCollected && _player.Bounds.Intersects(c.Bounds))
                    c.IsCollected = true;
            }

            // =========================
            // DOORS
            // =========================
            bool levelReady = _level.IsCleared();
            foreach (var d in _level._doors)
                d.CanOpen = levelReady;

            _activeDoor = null;

            foreach (var door in _level._doors)
            {
                if (_player.Bounds.Intersects(door.Bounds))
                {
                    _activeDoor = door;

                    if (door.CanOpen &&
                        _keyboard.IsKeyDown(Keys.E) &&
                        !_previousKeyboard.IsKeyDown(Keys.E))
                    {
                        _isTransitioning = true;
                        _fade = 0f;
                        break;
                    }
                }
            }

            // =========================
            // PROJECTILES
            // =========================
            for (int i = _projectiles.Count - 1; i >= 0; i--)
            {
                var p = _projectiles[i];

                p.Update(_level._solids);

                foreach (var enemy in _level._enemies)
                    p.HitEnemy(enemy);

                p.HitPlayer(_player);

                if (p.IsDead)
                    _projectiles.RemoveAt(i);
            }

            // =========================
            // SHAKE
            // =========================
            if (_shakeTimer > 0 && !_isTransitioning)
            {
                _shakeTimer -= dt;

                _shakeOffset = new Vector2(
                    (float)(Random.Shared.NextDouble() * 2 - 1) * _shakeMagnitude,
                    (float)(Random.Shared.NextDouble() * 2 - 1) * _shakeMagnitude
                );
            }
            else
            {
                _shakeOffset = Vector2.Zero;
            }

            // =========================
            // ENEMIES (FIXED - NO DOUBLE UPDATE)
            // =========================
            for (int i = _level._enemies.Count - 1; i >= 0; i--)
            {
                var enemy = _level._enemies[i];

                enemy.Update(_level._solids, _player, _projectiles);

                if (_player.AttackHitbox.Intersects(enemy.Bounds))
                {
                    enemy.TakeDamage(1);
                    _particles.SpawnExplosion(enemy.position, Color.Yellow);
                    StartShake(0.1f, 2f);
                }

                if (enemy.IsDead)
                {
                    _particles.SpawnExplosion(enemy.position, Color.OrangeRed);
                    StartShake(0.2f, 4f);
                    _level._enemies.RemoveAt(i);
                }
            }

            if (_level != null && _level.CurrentLevelName == "level3")
            {
                if (_player != null)
                {
                    // simple rule: collect all collectibles OR reach end
                    if (_level.IsCleared())
                    {
                        _player.EvolutionStage = 1;
                    }
                }
            }

            // =========================
            // PLAYER DAMAGE
            // =========================
            _player.CheckEnemyCollision(_level._enemies);
            _player.CheckDeath();

            _camera.Follow(_player.position);
            _particles.Update(gameTime);

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);

            Matrix cameraMatrix =
                _camera.GetViewMatrix() *
                Matrix.CreateTranslation(new Vector3(_shakeOffset, 0));

            // =========================
            // WORLD
            // =========================
            _spriteBatch.Begin(transformMatrix: cameraMatrix);

            _level.Draw(_spriteBatch);
            _player.Draw(_spriteBatch);
            _particles.Draw(_spriteBatch, TextureManager.Pixel);

            foreach (var p in _projectiles)
                p.Draw(_spriteBatch);

            _spriteBatch.End();

            // =========================
            // UI
            // =========================
            _spriteBatch.Begin();

            _hud.Draw(_spriteBatch, _player.Health);

            // =========================
            // BOSS HP BAR (FIXED POSITION)
            // =========================
            foreach (var enemy in _level._enemies)
            {
                if (enemy is Boss boss)
                {
                    float hp = (float)boss.Health / boss.MaxHealth;

                    Vector2 screenPos = boss.position;

                    // offset above boss
                    screenPos.Y -= 25;
                    screenPos.X -= 40;

                    int w = 80;
                    int h = 6;

                    Rectangle bg = new Rectangle((int)screenPos.X, (int)screenPos.Y, w, h);
                    Rectangle fg = new Rectangle((int)screenPos.X, (int)screenPos.Y, (int)(w * hp), h);

                    _spriteBatch.Draw(TextureManager.Pixel, bg, Color.DarkRed);
                    _spriteBatch.Draw(TextureManager.Pixel, fg, Color.LimeGreen);
                }
            }

            // =========================
            // DOOR TEXT
            // =========================
            if (_activeDoor != null && !_isTransitioning)
            {
                string text = _activeDoor.CanOpen
                    ? "Press E to enter"
                    : "Find all collectibles";

                Vector2 size = _uiFont.MeasureString(text);

                Vector2 pos = new Vector2(
                    (_graphics.PreferredBackBufferWidth / 2f) - (size.X / 2f),
                    200
                );

                _spriteBatch.DrawString(_uiFont, text, pos, Color.White);
            }

            _spriteBatch.End();

            // =========================
            // FADE + ARENA WALLS
            // =========================
            _spriteBatch.Begin();

            if (_isTransitioning)
            {
                _spriteBatch.Draw(
                    TextureManager.Pixel,
                    new Rectangle(0, 0, 1920, 1080),
                    Color.Black * MathHelper.Clamp(_fade, 0f, 1f)
                );
            }

            float shrink = Boss.ArenaShrink;

            Rectangle leftWall = new Rectangle((int)shrink, 0, 10, 2000);
            Rectangle rightWall = new Rectangle(1920 - (int)shrink, 0, 10, 2000);

            _spriteBatch.Draw(TextureManager.Pixel, leftWall, Color.Red);
            _spriteBatch.Draw(TextureManager.Pixel, rightWall, Color.Red);

            _spriteBatch.End();

            base.Draw(gameTime);
        }

        private void StartShake(float duration, float magnitude)
        {
            _shakeTimer = duration;
            _shakeMagnitude = magnitude;
        }
    }
}