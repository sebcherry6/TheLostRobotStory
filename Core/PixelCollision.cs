using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace TheLostRobotStory.Core
{
    // What an object looks like right now: which part of which texture is
    // drawn where. Without a texture it is simply its rectangle.
    public struct SpriteShape
    {
        public Texture2D Texture;
        public Rectangle Source;
        public Rectangle Destination;
        public bool FlipX;

        public bool HasPixels => Texture != null && Source.Width > 0 && Source.Height > 0;

        public static SpriteShape FromRect(Rectangle rect) => new SpriteShape { Destination = rect };

        public static SpriteShape FromTexture(Texture2D texture, Rectangle source, Rectangle destination, bool flipX = false)
        {
            if (texture == null)
                return FromRect(destination);

            return new SpriteShape { Texture = texture, Source = source, Destination = destination, FlipX = flipX };
        }
    }

    // Pixel-perfect overlap: two shapes only touch where BOTH have visible
    // (non-transparent) pixels. Used for damage and hits; walls and floors
    // still use rectangles so movement stays solid and predictable.
    public static class PixelCollision
    {
        private const byte AlphaThreshold = 40;

        private static readonly Dictionary<Texture2D, bool[]> _masks = new();

        public static bool[] Mask(Texture2D texture)
        {
            if (_masks.TryGetValue(texture, out bool[] mask))
                return mask;

            Color[] data = new Color[texture.Width * texture.Height];
            texture.GetData(data);

            mask = new bool[data.Length];

            for (int i = 0; i < data.Length; i++)
                mask[i] = data[i].A > AlphaThreshold;

            _masks[texture] = mask;
            return mask;
        }

        public static bool Overlaps(SpriteShape a, SpriteShape b)
        {
            Rectangle overlap = Rectangle.Intersect(a.Destination, b.Destination);

            if (overlap.Width <= 0 || overlap.Height <= 0)
                return false;

            if (!a.HasPixels && !b.HasPixels)
                return true;

            for (int y = overlap.Top; y < overlap.Bottom; y++)
            {
                for (int x = overlap.Left; x < overlap.Right; x++)
                {
                    if (IsSolid(a, x, y) && IsSolid(b, x, y))
                        return true;
                }
            }

            return false;
        }

        public static bool Overlaps(SpriteShape a, Rectangle b) => Overlaps(a, SpriteShape.FromRect(b));

        private static bool IsSolid(SpriteShape shape, int worldX, int worldY)
        {
            if (!shape.HasPixels)
                return true; // plain rectangle: inside the overlap = solid

            Rectangle d = shape.Destination;

            int u = (worldX - d.X) * shape.Source.Width / Math.Max(1, d.Width);
            int v = (worldY - d.Y) * shape.Source.Height / Math.Max(1, d.Height);

            if (shape.FlipX)
                u = shape.Source.Width - 1 - u;

            u = Math.Clamp(u, 0, shape.Source.Width - 1);
            v = Math.Clamp(v, 0, shape.Source.Height - 1);

            int tx = shape.Source.X + u;
            int ty = shape.Source.Y + v;

            if (tx < 0 || ty < 0 || tx >= shape.Texture.Width || ty >= shape.Texture.Height)
                return false;

            return Mask(shape.Texture)[ty * shape.Texture.Width + tx];
        }
    }
}
