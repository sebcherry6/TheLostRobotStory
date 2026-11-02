using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TheLostRobotStory.Core;
using TheLostRobotStory.Story;

namespace TheLostRobotStory.Scenes
{
    // Bottom-of-screen dialogue box with typewriter text.
    // Pauses gameplay underneath while it is open.
    // Lines may carry an Action that runs when the line appears;
    // lines with an action but no text are executed and skipped.
    public class DialogueScene : Scene
    {
        private const float CharsPerSecond = 45f;

        private readonly List<DialogueLine> _lines;
        private readonly Action _onFinished;
        private readonly Action<DialogueLine> _onLineStart;

        // called every frame while open (story moments animate the camera, letterbox...)
        private readonly Action<float> _onTick;

        private int _index;
        private float _visibleChars;
        private float _time;
        private string _wrapped = "";
        private bool _finished;

        public override bool IsOverlay => true;

        public DialogueScene(IEnumerable<DialogueLine> lines, Action onFinished = null, Action<DialogueLine> onLineStart = null, Action<float> onTick = null)
        {
            _lines = lines?.ToList() ?? new List<DialogueLine>();
            _onFinished = onFinished;
            _onLineStart = onLineStart;
            _onTick = onTick;
        }

        private DialogueLine Current => _lines[_index];

        private Rectangle Box => new Rectangle(40, Scenes.ScreenHeight - 170, Scenes.ScreenWidth - 80, 140);

        public override void OnEnter()
        {
            _index = 0;
            PrepareLine();
        }

        // runs actions, skips text-less lines, finishes when out of lines
        private void PrepareLine()
        {
            while (_index < _lines.Count)
            {
                _onLineStart?.Invoke(Current);

                if (!string.IsNullOrEmpty(Current.Text))
                {
                    _visibleChars = 0;
                    _wrapped = Ui.Wrap(Scenes.Font, Current.Text, Box.Width - 40);

                    if (Current.Speaker == "THE CORE")
                        AudioManager.Play("nexus_blip", 0.6f);
                    else if (Current.Speaker == "M-4")
                        AudioManager.Play("m4_talk", 0.6f);

                    return;
                }

                _index++;
            }

            Finish();
        }

        public override void Update(GameTime gameTime, InputManager input)
        {
            if (_finished || _index >= _lines.Count)
                return;

            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
            _time += dt;

            _onTick?.Invoke(dt);

            int before = (int)_visibleChars;
            _visibleChars = Math.Min(_visibleChars + CharsPerSecond * dt, _wrapped.Length);
            int after = (int)_visibleChars;

            // typing blip every few characters
            if (after > before && after % 3 == 0 && after < _wrapped.Length)
            {
                AudioManager.Play(IsCore ? "nexus_blip" : "text_blip", 0.35f, 0.15f);
            }

            if (input.Confirm() || input.InteractPressed())
            {
                if (_visibleChars < _wrapped.Length)
                {
                    _visibleChars = _wrapped.Length; // finish the line instantly
                }
                else
                {
                    _index++;
                    PrepareLine();
                }
            }
        }

        private bool IsCore => _index < _lines.Count && Current.Speaker == "THE CORE";

        private void Finish()
        {
            if (_finished)
                return;

            _finished = true;
            Scenes.Pop();
            _onFinished?.Invoke();
        }

        public static Color SpeakerColor(string speaker)
        {
            switch (speaker)
            {
                case "R-01": return Color.Cyan;
                case "THE CORE": return Color.Red;
                case "M-4": return Color.Gold;
                case "WARDEN-7": return Color.OrangeRed;
                case "SYSTEM": return Color.LightGreen;
                case "ALERT": return new Color(255, 70, 70);
                case "DATA": return Color.MediumPurple;
                case "": return Color.Gray;
                default: return Color.Orange; // terminal logs / people
            }
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            if (_finished || _index >= _lines.Count)
                return;

            SpriteFont font = Scenes.Font;
            Rectangle box = Box;
            Color color = SpeakerColor(Current.Speaker);

            string visible = _wrapped.Substring(0, Math.Min((int)_visibleChars, _wrapped.Length));
            Vector2 textPos = new Vector2(box.X + 20, box.Y + 40);
            Color textColor = Color.White;

            // THE CORE "glitches": jitter + corrupted characters on some frames
            if (IsCore)
            {
                Random glitch = new Random((int)(_time * 12f));

                if (glitch.Next(6) == 0)
                    textPos.X += glitch.Next(-3, 4);

                if (visible.Length > 4 && glitch.Next(4) == 0)
                {
                    char[] chars = visible.ToCharArray();
                    int i = glitch.Next(chars.Length);

                    if (chars[i] != '\n' && chars[i] != ' ')
                        chars[i] = "#%&$"[glitch.Next(4)];

                    visible = new string(chars);
                }
            }

            // alerts flash
            if (Current.Speaker == "ALERT")
                textColor = (int)(_time * 4) % 2 == 0 ? new Color(255, 90, 90) : Color.White;

            spriteBatch.Begin(samplerState: SamplerState.PointClamp);

            Ui.Rect(spriteBatch, box, Color.Black * 0.85f);
            Ui.Border(spriteBatch, box, color * 0.8f);

            Ui.Text(spriteBatch, font, Current.Speaker, new Vector2(box.X + 20, box.Y + 12), color);
            Ui.Text(spriteBatch, font, visible, textPos, textColor);

            if (_visibleChars >= _wrapped.Length && (int)(_time * 2) % 2 == 0)
            {
                Ui.Text(spriteBatch, font, "[E] next", new Vector2(box.Right - 90, box.Bottom - 26), Color.Gray);
            }

            spriteBatch.End();
        }
    }

    // =========================================================
    // CHAPTER CARD: "CHAPTER 2 / BELOW THE SURFACE" when a level begins
    // =========================================================

    public class ChapterCardScene : Scene
    {
        private const float Duration = 4.5f;

        private readonly string _chapter;
        private readonly string _title;
        private readonly string _area;
        private readonly string _goal;
        private float _time;
        private bool _closing;

        public override bool IsOverlay => true;

        public ChapterCardScene(string chapter, string title, string area, string goal)
        {
            _chapter = chapter ?? "";
            _title = title ?? "";
            _area = area ?? "";
            _goal = goal ?? "";
        }

        public override void Update(GameTime gameTime, InputManager input)
        {
            _time += (float)gameTime.ElapsedGameTime.TotalSeconds;

            if (!_closing && (_time > Duration || (_time > 0.6f && (input.Confirm() || input.Back()))))
            {
                _closing = true;
                _time = Math.Max(_time, Duration);
            }

            if (_closing && _time > Duration + 0.6f)
                Scenes.Pop();
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            int w = Scenes.ScreenWidth;
            int h = Scenes.ScreenHeight;
            SpriteFont font = Scenes.Font;

            float fadeIn = MathHelper.Clamp(_time / 0.8f, 0f, 1f);
            float fadeOut = _time > Duration ? 1f - MathHelper.Clamp((_time - Duration) / 0.6f, 0f, 1f) : 1f;
            float text = fadeIn * fadeOut;

            spriteBatch.Begin(samplerState: SamplerState.PointClamp);

            Ui.Rect(spriteBatch, Scenes.Screen, Color.Black * fadeOut);

            // thin line that grows under the title
            int lineWidth = (int)(w * 0.4f * MathHelper.Clamp(_time / 1.2f, 0f, 1f));
            Ui.Rect(spriteBatch, new Rectangle(w / 2 - lineWidth / 2, (int)(h * 0.52f), lineWidth, 2), Color.Cyan * 0.6f * fadeOut);

            Ui.TextCentered(spriteBatch, font, _chapter, w / 2f, h * 0.32f, Color.Cyan * text, 1.25f);
            Ui.TextCentered(spriteBatch, font, _title, w / 2f, h * 0.38f, Color.White * text, 3f);
            Ui.TextCentered(spriteBatch, font, _area, w / 2f, h * 0.56f, Color.Gray * text);

            float goalAlpha = MathHelper.Clamp((_time - 1.2f) / 0.8f, 0f, 1f) * fadeOut;
            Ui.TextCentered(spriteBatch, font, _goal, w / 2f, h * 0.64f, new Color(255, 210, 120) * goalAlpha);

            spriteBatch.End();
        }
    }

    // =========================================================
    // FULL SCREEN STORY PAGES (intro, endings, credits)
    // =========================================================

    public class StoryScene : Scene
    {
        private const float CharsPerSecond = 35f;

        private readonly List<string> _pages;
        private readonly Action<SceneManager> _onFinished;

        private int _page;
        private float _visibleChars;
        private float _time;
        private string _wrapped = "";
        private bool _finished;

        public StoryScene(IEnumerable<string> pages, Action<SceneManager> onFinished)
        {
            _pages = pages?.ToList() ?? new List<string>();

            if (_pages.Count == 0)
                _pages.Add("");

            _onFinished = onFinished;
        }

        public override void OnEnter()
        {
            PreparePage();
        }

        private void PreparePage()
        {
            _visibleChars = 0;
            _wrapped = Ui.Wrap(Scenes.Font, _pages[_page], Scenes.ScreenWidth * 0.7f, 1.25f);
        }

        public override void Update(GameTime gameTime, InputManager input)
        {
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
            _time += dt;

            int before = (int)_visibleChars;
            _visibleChars = Math.Min(_visibleChars + CharsPerSecond * dt, _wrapped.Length);

            if ((int)_visibleChars > before && (int)_visibleChars % 3 == 0)
                AudioManager.Play("text_blip", 0.25f, 0.1f);

            if (input.Back())
            {
                Finish(); // skip everything
                return;
            }

            if (input.Confirm())
            {
                if (_visibleChars < _wrapped.Length)
                {
                    _visibleChars = _wrapped.Length;
                }
                else if (_page + 1 < _pages.Count)
                {
                    _page++;
                    PreparePage();
                }
                else
                {
                    Finish();
                }
            }
        }

        private void Finish()
        {
            if (_finished)
                return;

            _finished = true;
            _onFinished?.Invoke(Scenes);
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            SpriteFont font = Scenes.Font;

            spriteBatch.Begin(samplerState: SamplerState.PointClamp);

            Ui.Rect(spriteBatch, Scenes.Screen, Color.Black);

            string visible = _wrapped.Substring(0, (int)_visibleChars);

            // blinking terminal cursor
            if ((int)(_time * 3) % 2 == 0)
                visible += "_";

            Ui.Text(
                spriteBatch,
                font,
                visible,
                new Vector2(Scenes.ScreenWidth * 0.15f, Scenes.ScreenHeight * 0.3f),
                new Color(190, 230, 255),
                1.25f);

            Ui.Text(
                spriteBatch,
                font,
                $"{_page + 1}/{_pages.Count}     [Enter] continue   [Esc] skip",
                new Vector2(Scenes.ScreenWidth - 330, Scenes.ScreenHeight - 36),
                Color.DimGray);

            spriteBatch.End();
        }
    }
}
