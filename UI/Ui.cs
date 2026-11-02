using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TheLostRobotStory.Core;

namespace TheLostRobotStory
{
    // Small drawing helpers shared by menus, dialogue and story screens.
    public static class Ui
    {
        public static void Rect(SpriteBatch spriteBatch, Rectangle rect, Color color)
        {
            spriteBatch.Draw(TextureManager.Pixel, rect, color);
        }

        public static void Border(SpriteBatch spriteBatch, Rectangle rect, Color color, int thickness = 2)
        {
            Rect(spriteBatch, new Rectangle(rect.X, rect.Y, rect.Width, thickness), color);
            Rect(spriteBatch, new Rectangle(rect.X, rect.Bottom - thickness, rect.Width, thickness), color);
            Rect(spriteBatch, new Rectangle(rect.X, rect.Y, thickness, rect.Height), color);
            Rect(spriteBatch, new Rectangle(rect.Right - thickness, rect.Y, thickness, rect.Height), color);
        }

        public static void Text(SpriteBatch spriteBatch, SpriteFont font, string text, Vector2 position, Color color, float scale = 1f)
        {
            spriteBatch.DrawString(
                font,
                Safe(text),
                new Vector2((int)position.X, (int)position.Y),
                color,
                0f,
                Vector2.Zero,
                scale,
                SpriteEffects.None,
                0f);
        }

        public static void TextCentered(SpriteBatch spriteBatch, SpriteFont font, string text, float centerX, float y, Color color, float scale = 1f)
        {
            text = Safe(text);

            // centre every line separately
            string[] lines = text.Split('\n');
            float lineHeight = font.LineSpacing * scale;

            for (int i = 0; i < lines.Length; i++)
            {
                Vector2 size = font.MeasureString(lines[i]) * scale;

                Text(
                    spriteBatch,
                    font,
                    lines[i],
                    new Vector2(centerX - size.X / 2f, y + i * lineHeight),
                    color,
                    scale);
            }
        }

        // UIFont only contains ASCII 32-126. Any other character makes
        // DrawString/MeasureString throw, so everything is filtered here.
        public static string Safe(string text)
        {
            if (string.IsNullOrEmpty(text))
                return "";

            StringBuilder sb = new(text.Length);

            foreach (char c in text)
            {
                switch (c)
                {
                    case '\n':
                        sb.Append(c);
                        break;
                    case '\u2018':
                    case '\u2019':
                        sb.Append('\'');
                        break;
                    case '\u201C':
                    case '\u201D':
                        sb.Append('"');
                        break;
                    case '\u2013':
                    case '\u2014':
                        sb.Append('-');
                        break;
                    case '\u2026':
                        sb.Append("...");
                        break;
                    default:
                        sb.Append(c >= 32 && c <= 126 ? c : '?');
                        break;
                }
            }

            return sb.ToString();
        }

        // Word wrap that keeps the string length identical (spaces become
        // newlines), so a typewriter effect can Substring() the result safely.
        public static string Wrap(SpriteFont font, string text, float maxWidth, float scale = 1f)
        {
            text = Safe(text);

            char[] chars = text.ToCharArray();
            int lineStart = 0;
            int lastSpace = -1;

            for (int i = 0; i < chars.Length; i++)
            {
                if (chars[i] == '\n')
                {
                    lineStart = i + 1;
                    lastSpace = -1;
                    continue;
                }

                if (chars[i] == ' ')
                    lastSpace = i;

                string line = new string(chars, lineStart, i - lineStart + 1);

                if (font.MeasureString(line).X * scale > maxWidth && lastSpace > lineStart)
                {
                    chars[lastSpace] = '\n';
                    lineStart = lastSpace + 1;
                    lastSpace = -1;
                }
            }

            return new string(chars);
        }
    }
}
