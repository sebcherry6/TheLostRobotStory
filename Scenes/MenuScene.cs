using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TheLostRobotStory.Core;
using TheLostRobotStory.Story;

namespace TheLostRobotStory.Scenes
{
    // Base class for every list menu (main menu, pause, options, choices).
    public abstract class MenuScene : Scene
    {
        protected class MenuItem
        {
            public Func<string> Label;
            public Action OnSelect;
            public Action<int> OnAdjust; // left/right (-1 / +1), e.g. volume
            public Func<bool> Enabled;
        }

        protected readonly List<MenuItem> Items = new();
        protected int Selected;
        protected string Title = "";
        protected string Subtitle;
        protected float TitleScale = 2f;
        protected float Time;

        protected void Add(string label, Action onSelect, Func<bool> enabled = null)
        {
            Items.Add(new MenuItem { Label = () => label, OnSelect = onSelect, Enabled = enabled });
        }

        protected void AddDynamic(Func<string> label, Action onSelect, Action<int> onAdjust = null, Func<bool> enabled = null)
        {
            Items.Add(new MenuItem { Label = label, OnSelect = onSelect, OnAdjust = onAdjust, Enabled = enabled });
        }

        protected virtual void OnBack() { }

        private static bool IsEnabled(MenuItem item) => item.Enabled == null || item.Enabled();

        public override void OnEnter()
        {
            Selected = 0;

            if (Items.Count > 0 && !IsEnabled(Items[0]))
                Move(1, playSound: false);
        }

        public override void Update(GameTime gameTime, InputManager input)
        {
            Time += (float)gameTime.ElapsedGameTime.TotalSeconds;

            if (Items.Count == 0)
                return;

            if (input.MenuUp())
                Move(-1);

            if (input.MenuDown())
                Move(1);

            MenuItem item = Items[Selected];

            if (item.OnAdjust != null)
            {
                if (input.MenuLeft())
                {
                    item.OnAdjust(-1);
                    AudioManager.Play("ui_move");
                }

                if (input.MenuRight())
                {
                    item.OnAdjust(1);
                    AudioManager.Play("ui_move");
                }
            }

            if (input.Confirm() && item.OnSelect != null && IsEnabled(item))
            {
                AudioManager.Play("ui_select");
                item.OnSelect();
            }
            else if (input.Back())
            {
                OnBack();
            }
        }

        private void Move(int direction, bool playSound = true)
        {
            for (int i = 0; i < Items.Count; i++)
            {
                Selected = (Selected + direction + Items.Count) % Items.Count;

                if (IsEnabled(Items[Selected]))
                    break;
            }

            if (playSound)
                AudioManager.Play("ui_move");
        }

        protected virtual void DrawBackground(SpriteBatch spriteBatch)
        {
            Ui.Rect(spriteBatch, Scenes.Screen, Color.Black * 0.75f);
        }

        protected virtual void DrawExtra(SpriteBatch spriteBatch) { }

        public override void Draw(SpriteBatch spriteBatch)
        {
            SpriteFont font = Scenes.Font;
            float centerX = Scenes.ScreenWidth / 2f;
            float maxWidth = Scenes.ScreenWidth * 0.8f;

            spriteBatch.Begin(samplerState: SamplerState.PointClamp);

            DrawBackground(spriteBatch);

            float y = Scenes.ScreenHeight * 0.18f;

            string title = Ui.Wrap(font, Title, maxWidth, TitleScale);
            Ui.TextCentered(spriteBatch, font, title, centerX, y, Color.Cyan, TitleScale);
            y += font.MeasureString(title).Y * TitleScale + 10;

            if (!string.IsNullOrEmpty(Subtitle))
            {
                Ui.TextCentered(spriteBatch, font, Subtitle, centerX, y, Color.Gray);
            }

            float itemY = Scenes.ScreenHeight * 0.45f;

            for (int i = 0; i < Items.Count; i++)
            {
                bool selected = i == Selected;
                bool enabled = IsEnabled(Items[i]);

                string label = Items[i].Label();

                if (selected)
                    label = "> " + label + " <";

                Color color =
                    !enabled ? Color.DimGray :
                    selected ? Color.Yellow :
                    Color.White;

                // selected entry gently pulses
                if (selected)
                    color *= 0.8f + 0.2f * (float)Math.Sin(Time * 6f);

                Ui.TextCentered(spriteBatch, font, label, centerX, itemY + i * 34, color, 1.25f);
            }

            DrawExtra(spriteBatch);

            spriteBatch.End();
        }
    }

    // =========================================================
    // MAIN MENU
    // =========================================================

    public class MainMenuScene : MenuScene
    {
        // Single save slot for now. SaveSystem already supports more.
        private const int Slot = 0;

        private SaveData _existingSave;

        public MainMenuScene()
        {
            // title is drawn by DrawExtra (logo or big text)
            Title = "";

            AddDynamic(ContinueLabel, Continue, enabled: () => _existingSave != null);
            Add("New Game", NewGame);
            Add("Options", () => Scenes.Push(new OptionsScene()));
            Add("Quit", () => Scenes.Game.Exit());
        }

        public override void OnEnter()
        {
            _existingSave = SaveSystem.Load(Slot);
            base.OnEnter();
            AudioManager.PlayMusic("menu");
        }

        private string ContinueLabel()
        {
            if (_existingSave == null)
                return "Continue";

            string sector = _existingSave.CurrentLevel.Replace("level", "Area ").Replace(".txt", "");
            int minutes = (int)(_existingSave.PlayTimeSeconds / 60);

            return $"Continue  ({sector}, {minutes} min)";
        }

        private void Continue()
        {
            if (_existingSave == null)
                return;

            Scenes.ClearAndPush(new GameplayScene(Slot, _existingSave));
        }

        private void NewGame()
        {
            if (_existingSave == null)
            {
                StartNewGame();
                return;
            }

            Scenes.Push(new ChoiceScene(
                "Start over? Your current save will be erased.",
                true,
                new ChoiceOption("Erase and start", StartNewGame),
                new ChoiceOption("Cancel", () => Scenes.Pop())));
        }

        private void StartNewGame()
        {
            SaveSystem.Delete(Slot);

            SaveData save = SaveData.NewGame();

            Scenes.ClearAndPush(new StoryScene(
                StoryData.Instance.Intro,
                manager => manager.ClearAndPush(new GameplayScene(Slot, save))));
        }

        // =====================================================
        // MENU ARTWORK
        // A living scene built from the game's own sprites:
        // parallax factory, R-01 running on the assembly line, M-4
        // keeping up, and a ceiling turret tracking them.
        // Optional overrides: Textures/Menu/background.png, Textures/Menu/title.png
        // =====================================================

        protected override void DrawBackground(SpriteBatch spriteBatch)
        {
            int w = Scenes.ScreenWidth;
            int h = Scenes.ScreenHeight;

            Ui.Rect(spriteBatch, Scenes.Screen, new Color(10, 12, 18));

            Texture2D custom = TextureManager.Get("Menu/background");

            if (custom != null)
                spriteBatch.Draw(custom, Scenes.Screen, Color.White);
            else if (TextureManager.Get("Backgrounds/level1_far") != null)
                DrawMenuScene(spriteBatch, w, h);
            else
                DrawGridFallback(spriteBatch, w, h);

            // darken the top for the title and give the menu a panel
            Ui.Rect(spriteBatch, new Rectangle(0, 0, w, (int)(h * 0.32f)), Color.Black * 0.35f);

            Rectangle panel = new Rectangle(w / 2 - 230, (int)(h * 0.45f) - 18, 460, Items.Count * 34 + 30);
            Ui.Rect(spriteBatch, panel, Color.Black * 0.6f);
            Ui.Border(spriteBatch, panel, Color.Cyan * 0.35f, 1);
        }

        private void DrawMenuScene(SpriteBatch spriteBatch, int w, int h)
        {
            const int scale = 3;
            int floorY = h - 96;

            // ---- parallax backgrounds (scrolling = R-01 running)
            DrawScrollingLayer(spriteBatch, TextureManager.Get("Backgrounds/level1_far"), Time * 10f, h);
            DrawScrollingLayer(spriteBatch, TextureManager.Get("Backgrounds/level1_near"), Time * 45f, h);

            // ---- assembly line floor
            Texture2D belt = TextureManager.Get("conveyor");
            int tile = 32 * 2;

            for (int x = 0; x < w; x += tile)
            {
                Rectangle dest = new Rectangle(x, floorY, tile, tile);

                if (belt != null)
                    Sprites.DrawFrame(spriteBatch, belt, dest, (int)(Time * 10f) % Sprites.FrameCount(belt, 32), Color.White, 32);
                else
                    Ui.Rect(spriteBatch, dest, new Color(55, 58, 66));
            }

            Ui.Rect(spriteBatch, new Rectangle(0, floorY + tile, w, h - floorY - tile), new Color(14, 15, 20));

            // ---- R-01 running against the belt
            Vector2 playerFeet = new Vector2(w * 0.26f, floorY + 2);
            Texture2D player = TextureManager.Player;

            if (player != null)
            {
                int frame = (int)(Time / 0.08f) % 6;
                Rectangle source = new Rectangle(frame * 32, 32, 32, 32); // row 1 = run
                Rectangle dest = new Rectangle((int)playerFeet.X - 16 * scale, (int)playerFeet.Y - 32 * scale, 32 * scale, 32 * scale);
                spriteBatch.Draw(player, dest, source, Color.White);
            }

            // ---- M-4 hovering behind
            Texture2D m4 = TextureManager.Get("m4");

            if (m4 != null)
            {
                float bob = (float)Math.Sin(Time * 3f) * 8f;
                Rectangle dest = new Rectangle((int)playerFeet.X - 150, (int)(playerFeet.Y - 175 + bob), 26 * scale, 26 * scale);
                Sprites.DrawFrame(spriteBatch, m4, dest, (int)(Time * 6f) % 4, Color.White, 26);
            }

            // ---- ceiling turret tracking them
            Texture2D turret = TextureManager.Get("turret");

            if (turret != null)
            {
                int size = 28 * scale;
                Vector2 pivot = new Vector2(w * 0.8f, size / 2f);
                Vector2 target = playerFeet + new Vector2(0, -16 * scale);
                Vector2 aim = target - pivot;
                float angle = (float)Math.Atan2(aim.Y, aim.X);

                // fires a "warning shot" every few seconds
                float cycle = Time % 3f;
                int headFrame = cycle < 0.08f ? 3 : cycle < 0.22f ? 4 : cycle > 2.2f ? 2 : 1;

                spriteBatch.Draw(turret, new Rectangle((int)(pivot.X - size / 2f), 0, size, size), Sprites.FrameRect(turret, 0, 28), Color.White);
                spriteBatch.Draw(turret, pivot, Sprites.FrameRect(turret, headFrame, 28), Color.White, angle, new Vector2(14, 14), scale, SpriteEffects.None, 0f);

                // red targeting line while alert
                if (headFrame == 2)
                {
                    float length = aim.Length() - 40f;
                    spriteBatch.Draw(TextureManager.Pixel, pivot, null, Color.Red * 0.35f, angle, new Vector2(0, 0.5f), new Vector2(length, 2), SpriteEffects.None, 0f);
                }
            }
        }

        private static void DrawScrollingLayer(SpriteBatch spriteBatch, Texture2D texture, float scroll, int screenH)
        {
            if (texture == null)
                return;

            float scale = (float)screenH / texture.Height;
            int drawWidth = Math.Max(1, (int)(texture.Width * scale));
            int offset = -(int)(scroll % drawWidth);

            for (int x = offset; x < spriteBatch.GraphicsDevice.Viewport.Width; x += drawWidth)
                spriteBatch.Draw(texture, new Rectangle(x, 0, drawWidth + 1, screenH), Color.White);
        }

        private void DrawGridFallback(SpriteBatch spriteBatch, int w, int h)
        {
            int offset = (int)(Time * 20f) % 40;
            Color line = new Color(30, 40, 55);

            for (int x = -40 + offset; x < w; x += 40)
                Ui.Rect(spriteBatch, new Rectangle(x, 0, 1, h), line);

            for (int y = -40 + offset; y < h; y += 40)
                Ui.Rect(spriteBatch, new Rectangle(0, y, w, 1), line);
        }

        protected override void DrawExtra(SpriteBatch spriteBatch)
        {
            int w = Scenes.ScreenWidth;
            int h = Scenes.ScreenHeight;
            SpriteFont font = Scenes.Font;

            Texture2D logo = TextureManager.Get("Menu/title");

            if (logo != null)
            {
                float scale = Math.Min(1f, w * 0.7f / logo.Width) * 2f;
                Rectangle dest = new Rectangle((int)(w / 2f - logo.Width * scale / 2f), (int)(h * 0.06f), (int)(logo.Width * scale), (int)(logo.Height * scale));
                spriteBatch.Draw(logo, dest, Color.White);
            }
            else
            {
                float y = h * 0.1f;
                Ui.TextCentered(spriteBatch, font, "THE LOST ROBOT STORY", w / 2f + 4, y + 4, Color.Black * 0.8f, 3f);
                Ui.TextCentered(spriteBatch, font, "THE LOST ROBOT STORY", w / 2f, y, Color.White, 3f);

                bool flicker = Math.Sin(Time * 13f) > 0.93;
                Ui.TextCentered(spriteBatch, font, "- SIGNAL LOST -", w / 2f, y + 64, (flicker ? Color.DarkCyan : Color.Cyan) * 0.9f, 1.25f);
            }

            int endings = Scenes.Game.Settings.EndingsSeen.Count;

            if (endings > 0)
            {
                Ui.Text(spriteBatch, font, $"Endings found: {endings}/3",
                    new Vector2(20, h - 30), Color.Gray);
            }
        }
    }

    // =========================================================
    // PAUSE
    // =========================================================

    public class PauseScene : MenuScene
    {
        private readonly GameplayScene _game;

        public override bool IsOverlay => true;

        public PauseScene(GameplayScene game)
        {
            _game = game;

            Title = "PAUSED";

            Add("Resume", () => Scenes.Pop());
            Add("Options", () => Scenes.Push(new OptionsScene()));
            Add("Save and quit to menu", () =>
            {
                _game.SaveGame();
                Scenes.ClearAndPush(new MainMenuScene());
            });
            Add("Save and quit to desktop", () =>
            {
                _game.SaveGame();
                Scenes.Game.Exit();
            });
        }

        public override void OnEnter()
        {
            base.OnEnter();
            Subtitle = _game.ObjectiveText();
            AudioManager.SetDucked(true);
        }

        public override void OnExit()
        {
            AudioManager.SetDucked(false);
        }

        protected override void OnBack() => Scenes.Pop();
    }

    // =========================================================
    // OPTIONS
    // =========================================================

    public class OptionsScene : MenuScene
    {
        public override bool IsOverlay => true;

        private GameSettings Settings => Scenes.Game.Settings;

        public OptionsScene()
        {
            Title = "OPTIONS";
            Subtitle = "left / right to change";

            AddDynamic(
                () => $"Music volume: {(int)Math.Round(Settings.MusicVolume * 100)}%",
                null,
                dir => Settings.MusicVolume = Math.Clamp(Settings.MusicVolume + dir * 0.1f, 0f, 1f));

            AddDynamic(
                () => $"Sound volume: {(int)Math.Round(Settings.SfxVolume * 100)}%",
                null,
                dir => Settings.SfxVolume = Math.Clamp(Settings.SfxVolume + dir * 0.1f, 0f, 1f));

            AddDynamic(
                () => "Fullscreen: " + (Settings.Fullscreen ? "On" : "Off"),
                () => Settings.Fullscreen = !Settings.Fullscreen,
                dir => Settings.Fullscreen = !Settings.Fullscreen);

            Add("Back", Close);
        }

        public override void Update(GameTime gameTime, InputManager input)
        {
            base.Update(gameTime, input);

            // apply live so the player hears the volume change
            Scenes.Game.ApplySettings(Settings);
        }

        protected override void OnBack() => Close();

        private void Close()
        {
            SaveSystem.SaveSettings(Settings);
            Scenes.Pop();
        }
    }

    // =========================================================
    // CHOICE (confirmations + the ending decision)
    // =========================================================

    public record ChoiceOption(string Label, Action OnSelect);

    public class ChoiceScene : MenuScene
    {
        private readonly bool _allowBack;

        public override bool IsOverlay => true;

        public ChoiceScene(string prompt, bool allowBack, params ChoiceOption[] options)
        {
            Title = prompt;
            TitleScale = 1.25f;
            _allowBack = allowBack;

            foreach (ChoiceOption option in options)
                Add(option.Label, option.OnSelect);
        }

        protected override void OnBack()
        {
            if (_allowBack)
                Scenes.Pop();
        }
    }
}
