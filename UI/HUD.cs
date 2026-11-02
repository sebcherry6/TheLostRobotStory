using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TheLostRobotStory.Core;
using TheLostRobotStory.Entities;

namespace TheLostRobotStory
{
    public class HUD
    {
        private readonly SpriteFont _font;

        public HUD(SpriteFont font)
        {
            _font = font;
        }

        // R-01's upgrades, in the order the story hands them out
        public static string UpgradeName(int stage) => stage switch
        {
            0 => "Maintenance tools",
            1 => "Mag-thrusters (double jump)",
            2 => "Welding arc (shoot)",
            3 => "Arc splitter (spread shot)",
            4 => "Overclock (dash)",
            _ => "Unknown"
        };

        public void Draw(SpriteBatch spriteBatch, Player player, IReadOnlyList<(string Text, bool Done)> objectives, int screenWidth, int rebootsLeft = -1, int maxReboots = 3)
        {
            if (_font == null)
                return;

            // ---------- status panel (top-left)
            int pipsWidth = player.MaxHealth * 22;
            Rectangle panel = new Rectangle(12, 10, System.Math.Max(250, 80 + pipsWidth), rebootsLeft >= 0 ? 88 : 62);

            Ui.Rect(spriteBatch, panel, Color.Black * 0.45f);
            Ui.Text(spriteBatch, _font, "R-01", new Vector2(24, 16), Color.Cyan);

            for (int i = 0; i < player.MaxHealth; i++)
            {
                Rectangle pip = new Rectangle(76 + i * 22, 18, 16, 16);
                Ui.Rect(spriteBatch, pip, i < player.Health ? Color.Red : Color.DarkRed * 0.5f);
            }

            Ui.Text(spriteBatch, _font, UpgradeName(player.EvolutionStage), new Vector2(24, 44), Color.LightGray);

            // reboots: respawns left at charging stations before the area restarts
            if (rebootsLeft >= 0)
            {
                Ui.Text(spriteBatch, _font, "REBOOTS", new Vector2(24, 66), Color.Gray);

                for (int i = 0; i < maxReboots; i++)
                {
                    Rectangle cell = new Rectangle(110 + i * 18, 68, 12, 12);
                    Ui.Rect(spriteBatch, cell, i < rebootsLeft ? new Color(90, 220, 140) : new Color(60, 60, 70));
                    Ui.Rect(spriteBatch, new Rectangle(cell.X + 3, cell.Y - 2, 6, 2), i < rebootsLeft ? new Color(90, 220, 140) : new Color(60, 60, 70));
                }
            }

            // ---------- objectives (top-right)
            if (objectives == null || objectives.Count == 0)
                return;

            const int width = 340;
            Rectangle box = new Rectangle(screenWidth - width - 12, 10, width, 30 + objectives.Count * 22);

            Ui.Rect(spriteBatch, box, Color.Black * 0.45f);
            Ui.Text(spriteBatch, _font, "OBJECTIVES", new Vector2(box.X + 12, box.Y + 6), Color.Gray);

            for (int i = 0; i < objectives.Count; i++)
            {
                var (text, done) = objectives[i];

                Ui.Text(
                    spriteBatch,
                    _font,
                    (done ? "[x] " : "[ ] ") + text,
                    new Vector2(box.X + 12, box.Y + 28 + i * 22),
                    done ? Color.Gray : Color.White);
            }
        }
    }
}
