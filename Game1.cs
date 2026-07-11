using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using TheLostRobotStory.Core;
using TheLostRobotStory.Entities;

namespace TheLostRobotStory
{
    public class Game1 : Game
    {

        private GraphicsDeviceManager _graphics;

        private SpriteBatch _spriteBatch;



        // =====================================================
        // CORE SYSTEMS
        // =====================================================

        private GameManager _gameManager;

        private InputManager _inputManager;



        private readonly List<Projectile> _projectiles =
            new();



        private readonly ParticleManager _particles =
            new();





        // =====================================================
        // CAMERA
        // =====================================================

        private Camera _camera;

        private float _shakeTimer;

        private float _shakeMagnitude;

        private Vector2 _shakeOffset;





        // =====================================================
        // UI
        // =====================================================

        private SpriteFont _uiFont;

        private HUD _hud;



        private string _messageText = "";

        private float _messageTimer;






        // =====================================================
        // LEVEL TRANSITION
        // =====================================================

        private bool _isTransitioning;

        private float _fade;






        public Game1()
        {

            _graphics =
                new GraphicsDeviceManager(this);


            Content.RootDirectory =
                "Content";


            IsMouseVisible = true;

        }







        // =====================================================
        // INITIALIZE
        // =====================================================

        protected override void Initialize()
        {

            _camera =
                new Camera(
                    GraphicsDevice.Viewport.Width,
                    GraphicsDevice.Viewport.Height);



            _inputManager =
                new InputManager();



            _gameManager =
                new GameManager(
                    _particles,
                    _projectiles);





            Boss.BossSlamEvent += () =>
            {

                StartShake(
                    0.25f,
                    6f);

            };



            base.Initialize();

        }








        // =====================================================
        // LOAD CONTENT
        // =====================================================

        protected override void LoadContent()
        {

            _spriteBatch =
                new SpriteBatch(
                    GraphicsDevice);





            TextureManager.Load(
                Content,
                GraphicsDevice);





            _uiFont =
                Content.Load<SpriteFont>(
                    "UIFont");



            _hud =
                new HUD(
                    _uiFont);





            _gameManager.LoadGame(
                "level1.txt");





            ShowMessage(
                "A - Left\nD - Right\nSPACE - Jump\nJ - Attack",
                5f);

        }









        // =====================================================
        // UPDATE
        // =====================================================

        protected override void Update(
            GameTime gameTime)
        {

            float dt =
                (float)
                gameTime.ElapsedGameTime.TotalSeconds;





            // INPUT

            _inputManager.Update();




            if (_inputManager.ExitPressed())
            {

                Exit();

            }






            // MESSAGE TIMER

            if (_messageTimer > 0)
            {

                _messageTimer -= dt;


                if (_messageTimer <= 0)
                {

                    _messageText = "";

                }

            }







            // GAME UPDATE

            _gameManager.Update(
                gameTime,
                _inputManager);




            // CAMERA

            if (_gameManager.Player != null)
            {
                _camera.Follow(
                    _gameManager.Player.position,
                    gameTime);
            }






            // PARTICLES

            _particles.Update(
                gameTime);





            UpdateShake(
                dt);




            base.Update(
                gameTime);

        }

        // =====================================================
        // DRAW
        // =====================================================

        protected override void Draw(
            GameTime gameTime)
        {

            GraphicsDevice.Clear(
                Color.CornflowerBlue);





            Matrix cameraMatrix =
                _camera.GetViewMatrix()
                *
                Matrix.CreateTranslation(
                    _shakeOffset.X,
                    _shakeOffset.Y,
                    0);






            // =====================================================
            // WORLD
            // =====================================================

            _spriteBatch.Begin(
                transformMatrix:
                cameraMatrix);





            _gameManager.Draw(
                _spriteBatch);





            _particles.Draw(
                _spriteBatch,
                TextureManager.Pixel);







            // =====================================================
            // BOSS HEALTH BAR
            // =====================================================

            if (_gameManager.CurrentLevel != null)
            {

                foreach (var enemy in
                    _gameManager.CurrentLevel._enemies)
                {

                    if (enemy is Boss boss)
                    {

                        float hp =
                            MathHelper.Clamp(
                                (float)boss.Health /
                                boss.MaxHealth,
                                0,
                                1);





                        Rectangle back =
                            new Rectangle(
                                boss.Bounds.Center.X - 50,
                                boss.Bounds.Top - 20,
                                100,
                                8);




                        Rectangle front =
                            new Rectangle(
                                boss.Bounds.Center.X - 50,
                                boss.Bounds.Top - 20,
                                (int)(100 * hp),
                                8);






                        _spriteBatch.Draw(
                            TextureManager.Pixel,
                            back,
                            Color.DarkRed);





                        _spriteBatch.Draw(
                            TextureManager.Pixel,
                            front,
                            Color.LimeGreen);


                    }

                }

            }






            _spriteBatch.End();








            // =====================================================
            // UI
            // =====================================================

            _spriteBatch.Begin();





            if (_gameManager.Player != null &&
               _gameManager.CurrentLevel != null)
            {

                _hud.Draw(
                    _spriteBatch,
                    _gameManager.Player,
                    _gameManager.CurrentLevel.CollectiblesRemaining,
                    _gameManager.CurrentLevel.EnemiesRemaining);

            }







            // =====================================================
            // MESSAGE POPUP
            // =====================================================

            if (!string.IsNullOrEmpty(_messageText))
            {

                Vector2 size =
                    _uiFont.MeasureString(
                        _messageText);




                _spriteBatch.DrawString(
                    _uiFont,
                    _messageText,
                    new Vector2(
                        (_graphics.PreferredBackBufferWidth / 2)
                        - size.X / 2,
                        80),
                    Color.White);

            }






            _spriteBatch.End();







            // =====================================================
            // FADE
            // =====================================================

            _spriteBatch.Begin();




            if (_isTransitioning)
            {

                _spriteBatch.Draw(
                    TextureManager.Pixel,
                    new Rectangle(
                        0,
                        0,
                        _graphics.PreferredBackBufferWidth,
                        _graphics.PreferredBackBufferHeight),
                    Color.Black *
                    MathHelper.Clamp(
                        _fade,
                        0,
                        1));

            }





            _spriteBatch.End();




            base.Draw(
                gameTime);

        }
        // =====================================================
        // POPUP MESSAGE
        // =====================================================

        private void ShowMessage(
            string message,
            float duration)
        {

            _messageText =
                message;


            _messageTimer =
                duration;

        }







        // =====================================================
        // CAMERA SHAKE
        // =====================================================

        private void StartShake(
            float duration,
            float magnitude)
        {

            _shakeTimer =
                duration;


            _shakeMagnitude =
                magnitude;

        }







        private void UpdateShake(
            float dt)
        {

            if (_shakeTimer > 0)
            {

                _shakeTimer -= dt;




                _shakeOffset =
                    new Vector2(

                        (float)
                        (Random.Shared.NextDouble() * 2 - 1)
                        *
                        _shakeMagnitude,


                        (float)
                        (Random.Shared.NextDouble() * 2 - 1)
                        *
                        _shakeMagnitude

                    );

            }
            else
            {

                _shakeOffset =
                    Vector2.Zero;

            }

        }

    }
}
