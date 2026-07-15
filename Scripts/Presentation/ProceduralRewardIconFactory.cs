using System.Collections.Generic;
using Godot;

namespace ProjectMannequin.Presentation;

/// <summary>
/// Draws small, flat category icons for run rewards entirely in code (no external
/// art dependency). Icons are keyed by Move Card type ("Launcher", "Projectile",
/// "ComboExtension", "Special", "Ultimate", "Basic") or "Artifact", and cached so
/// each icon is only rasterized once. This is a procedural placeholder pass; a
/// Higgsfield hero pass can later replace these by keying the same strings.
/// </summary>
public static class ProceduralRewardIconFactory
{
    private const int Size = 64;
    private static readonly Dictionary<string, Texture2D> Cache = new();

    public static Texture2D GetIcon(string iconKey)
    {
        if (string.IsNullOrEmpty(iconKey))
        {
            iconKey = "Basic";
        }

        if (Cache.TryGetValue(iconKey, out var cached))
        {
            return cached;
        }

        var texture = Build(iconKey);
        Cache[iconKey] = texture;
        return texture;
    }

    private static Texture2D Build(string iconKey)
    {
        var image = Image.CreateEmpty(Size, Size, false, Image.Format.Rgba8);
        image.Fill(Colors.Transparent);

        var (background, glyph) = Palette(iconKey);
        FillDisc(image, 32, 32, 30, background);
        Ring(image, 32, 32, 30, 3, background.Lightened(0.25f));

        var facet = background.Darkened(0.35f);
        switch (iconKey)
        {
            case "Launcher":
                FillTriangle(image, new Vector2I(32, 12), new Vector2I(19, 33), new Vector2I(45, 33), glyph);
                FillRect(image, 28, 31, 8, 20, glyph);
                break;
            case "Projectile":
                Line(image, 14, 50, 26, 43, 2, glyph);
                Line(image, 15, 43, 28, 36, 3, glyph);
                FillDisc(image, 37, 27, 10, glyph);
                FillDisc(image, 33, 23, 3, Colors.White);
                break;
            case "ComboExtension":
                Line(image, 23, 18, 34, 32, 4, glyph);
                Line(image, 34, 32, 23, 46, 4, glyph);
                Line(image, 35, 18, 46, 32, 4, glyph);
                Line(image, 46, 32, 35, 46, 4, glyph);
                break;
            case "Special":
                Line(image, 32, 12, 32, 52, 4, glyph);
                Line(image, 12, 32, 52, 32, 4, glyph);
                FillDisc(image, 32, 32, 4, Colors.White);
                break;
            case "Ultimate":
                Line(image, 32, 10, 32, 54, 4, glyph);
                Line(image, 10, 32, 54, 32, 4, glyph);
                Line(image, 18, 18, 46, 46, 4, glyph);
                Line(image, 46, 18, 18, 46, 4, glyph);
                FillDisc(image, 32, 32, 5, glyph.Lightened(0.35f));
                break;
            case "Artifact":
                FillTriangle(image, new Vector2I(32, 14), new Vector2I(18, 32), new Vector2I(46, 32), glyph);
                FillTriangle(image, new Vector2I(18, 32), new Vector2I(46, 32), new Vector2I(32, 51), glyph);
                Line(image, 18, 32, 46, 32, 1, facet);
                Line(image, 32, 14, 32, 51, 1, facet);
                break;
            default: // "Basic" and any unknown key.
                FillRect(image, 22, 26, 20, 17, glyph);
                FillRect(image, 19, 33, 4, 9, glyph);
                Line(image, 27, 26, 27, 42, 2, facet);
                Line(image, 32, 26, 32, 42, 2, facet);
                Line(image, 37, 26, 37, 42, 2, facet);
                break;
        }

        return ImageTexture.CreateFromImage(image);
    }

    private static (Color Background, Color Glyph) Palette(string iconKey)
    {
        return iconKey switch
        {
            "Launcher" => (new Color(0.34f, 0.32f, 0.74f), Colors.White),
            "Projectile" => (new Color(0.16f, 0.55f, 0.82f), Colors.White),
            "ComboExtension" => (new Color(0.22f, 0.6f, 0.38f), Colors.White),
            "Special" => (new Color(0.82f, 0.5f, 0.2f), Colors.White),
            "Ultimate" => (new Color(0.72f, 0.24f, 0.42f), new Color(1.0f, 0.86f, 0.4f)),
            "Artifact" => (new Color(0.18f, 0.56f, 0.56f), new Color(1.0f, 0.86f, 0.4f)),
            _ => (new Color(0.36f, 0.38f, 0.44f), Colors.White),
        };
    }

    private static void SetPx(Image image, int x, int y, Color color)
    {
        if (x < 0 || y < 0 || x >= Size || y >= Size)
        {
            return;
        }

        image.SetPixel(x, y, color);
    }

    private static void FillDisc(Image image, int cx, int cy, int radius, Color color)
    {
        for (var y = -radius; y <= radius; y++)
        {
            for (var x = -radius; x <= radius; x++)
            {
                if (x * x + y * y <= radius * radius)
                {
                    SetPx(image, cx + x, cy + y, color);
                }
            }
        }
    }

    private static void Ring(Image image, int cx, int cy, int radius, int thickness, Color color)
    {
        var inner = (radius - thickness) * (radius - thickness);
        for (var y = -radius; y <= radius; y++)
        {
            for (var x = -radius; x <= radius; x++)
            {
                var distanceSquared = x * x + y * y;
                if (distanceSquared <= radius * radius && distanceSquared >= inner)
                {
                    SetPx(image, cx + x, cy + y, color);
                }
            }
        }
    }

    private static void FillRect(Image image, int x0, int y0, int width, int height, Color color)
    {
        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                SetPx(image, x0 + x, y0 + y, color);
            }
        }
    }

    private static void Line(Image image, int x0, int y0, int x1, int y1, int thickness, Color color)
    {
        var dx = Mathf.Abs(x1 - x0);
        var dy = -Mathf.Abs(y1 - y0);
        var sx = x0 < x1 ? 1 : -1;
        var sy = y0 < y1 ? 1 : -1;
        var error = dx + dy;

        while (true)
        {
            Stamp(image, x0, y0, thickness, color);
            if (x0 == x1 && y0 == y1)
            {
                break;
            }

            var doubleError = 2 * error;
            if (doubleError >= dy)
            {
                error += dy;
                x0 += sx;
            }

            if (doubleError <= dx)
            {
                error += dx;
                y0 += sy;
            }
        }
    }

    private static void Stamp(Image image, int cx, int cy, int thickness, Color color)
    {
        var half = thickness / 2;
        for (var y = -half; y <= half; y++)
        {
            for (var x = -half; x <= half; x++)
            {
                SetPx(image, cx + x, cy + y, color);
            }
        }
    }

    private static void FillTriangle(Image image, Vector2I a, Vector2I b, Vector2I c, Color color)
    {
        var minX = Mathf.Max(0, Mathf.Min(a.X, Mathf.Min(b.X, c.X)));
        var maxX = Mathf.Min(Size - 1, Mathf.Max(a.X, Mathf.Max(b.X, c.X)));
        var minY = Mathf.Max(0, Mathf.Min(a.Y, Mathf.Min(b.Y, c.Y)));
        var maxY = Mathf.Min(Size - 1, Mathf.Max(a.Y, Mathf.Max(b.Y, c.Y)));

        for (var y = minY; y <= maxY; y++)
        {
            for (var x = minX; x <= maxX; x++)
            {
                if (InTriangle(new Vector2I(x, y), a, b, c))
                {
                    SetPx(image, x, y, color);
                }
            }
        }
    }

    private static bool InTriangle(Vector2I p, Vector2I a, Vector2I b, Vector2I c)
    {
        var d1 = EdgeSign(p, a, b);
        var d2 = EdgeSign(p, b, c);
        var d3 = EdgeSign(p, c, a);
        var hasNegative = d1 < 0 || d2 < 0 || d3 < 0;
        var hasPositive = d1 > 0 || d2 > 0 || d3 > 0;
        return !(hasNegative && hasPositive);
    }

    private static int EdgeSign(Vector2I p1, Vector2I p2, Vector2I p3)
    {
        return (p1.X - p3.X) * (p2.Y - p3.Y) - (p2.X - p3.X) * (p1.Y - p3.Y);
    }
}
