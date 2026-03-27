using UnityEngine;

/// <summary>
/// Procedural 2D silhouettes (no art assets) for player ship, enemy ships, and asteroids.
/// </summary>
public static class GameplaySprites
{
    const int PlayerSize = 56;
    const int ShipSize = 44;
    const int AsteroidSize = 48;

    public static Sprite PlayerShip(float pixelsPerUnit = 42f)
    {
        int w = PlayerSize;
        int h = PlayerSize;
        var tex = NewTransparentTexture(w, h);

        // Fuselage (arrow pointing up toward +Y)
        FillPolygon(tex, new[]
        {
            new Vector2(w / 2f, h - 4f),
            new Vector2(w / 2f - 7f, h - 18f),
            new Vector2(w / 2f - 5f, 10f),
            new Vector2(w / 2f + 5f, 10f),
            new Vector2(w / 2f + 7f, h - 18f)
        }, C(0.92f, 0.94f, 0.98f, 1f));

        // Wings
        FillRect(tex, 6, h / 2 - 3, w / 2 - 10, h / 2 + 3, C(0.75f, 0.82f, 0.95f, 1f));
        FillRect(tex, w / 2 + 10, h / 2 - 3, w - 6, h / 2 + 3, C(0.75f, 0.82f, 0.95f, 1f));

        // Cockpit window
        FillEllipse(tex, w / 2f, h - 22f, 5f, 7f, C(0.35f, 0.65f, 0.95f, 1f));

        // Engine housings
        FillRect(tex, w / 2 - 12, 4, w / 2 - 4, 12, C(0.5f, 0.55f, 0.62f, 1f));
        FillRect(tex, w / 2 + 4, 4, w / 2 + 12, 12, C(0.5f, 0.55f, 0.62f, 1f));

        tex.Apply();
        tex.filterMode = FilterMode.Point;
        return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.42f), pixelsPerUnit);
    }

    public static Sprite EnemyScoutShip(float pixelsPerUnit = 38f)
    {
        int w = ShipSize;
        int h = ShipSize;
        var tex = NewTransparentTexture(w, h);

        // Aggressive wedge — nose up
        FillPolygon(tex, new[]
        {
            new Vector2(w / 2f, h - 3f),
            new Vector2(w / 2f - 14f, h - 16f),
            new Vector2(w / 2f - 6f, 8f),
            new Vector2(w / 2f + 6f, 8f),
            new Vector2(w / 2f + 14f, h - 16f)
        }, C(0.92f, 0.35f, 0.42f, 1f));

        FillRect(tex, 4, h / 2 - 2, w / 2 - 12, h / 2 + 3, C(0.75f, 0.2f, 0.35f, 1f));
        FillRect(tex, w / 2 + 12, h / 2 - 2, w - 4, h / 2 + 3, C(0.75f, 0.2f, 0.35f, 1f));

        FillEllipse(tex, w / 2f, h - 18f, 3.5f, 4.5f, C(1f, 0.55f, 0.2f, 1f));

        tex.Apply();
        tex.filterMode = FilterMode.Point;
        return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.4f), pixelsPerUnit);
    }

    public static Sprite Asteroid(int seed, float pixelsPerUnit = 36f)
    {
        int w = AsteroidSize;
        int h = AsteroidSize;
        var tex = NewTransparentTexture(w, h);

        float cx = w / 2f;
        float cy = h / 2f;
        float baseR = w * 0.38f;

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                float dx = x - cx;
                float dy = y - cy;
                float a = Mathf.Atan2(dy, dx);
                float n1 = FracSin01(seed, 13.7f, a * 3.1f);
                float n2 = FracSin01(seed, 7.9f, a * 5.3f + n1);
                float r = baseR * (0.82f + 0.13f * n1 + 0.08f * n2);
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                if (d > r)
                    continue;

                float shade = 0.45f + 0.38f * (1f - d / r) + 0.12f * FracSin01(seed, 3.1f, x * 0.21f + y * 0.17f);
                Color rock = Color.Lerp(
                    C(0.38f, 0.3f, 0.26f, 1f),
                    C(0.62f, 0.55f, 0.48f, 1f),
                    Mathf.Clamp01(shade));

                // Craters
                if (Crater(x, y, seed, 11, 14, 9f) ||
                    Crater(x, y, seed ^ 0x5A5A5A5A, 29, 31, 7f))
                {
                    rock = Color.Lerp(rock, C(0.22f, 0.18f, 0.16f, 1f), 0.6f);
                }

                tex.SetPixel(x, y, rock);
            }
        }

        tex.Apply();
        tex.filterMode = FilterMode.Bilinear;
        return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), pixelsPerUnit);
    }

    static bool Crater(int x, int y, int seed, int ox, int oy, float rad)
    {
        float cx = (seed % 31) * 0.7f + ox;
        float cy = ((seed >> 3) % 29) * 0.6f + oy;
        float d = Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy));
        return d < rad;
    }

    static Texture2D NewTransparentTexture(int w, int h)
    {
        var t = new Texture2D(w, h, TextureFormat.RGBA32, false);
        var clear = Color.clear;
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
                t.SetPixel(x, y, clear);
        return t;
    }

    static Color C(float r, float g, float b, float a) => new Color(r, g, b, a);

    static void FillRect(Texture2D tex, int x0, int y0, int x1, int y1, Color c)
    {
        int minX = Mathf.Max(0, Mathf.Min(x0, x1));
        int maxX = Mathf.Min(tex.width - 1, Mathf.Max(x0, x1));
        int minY = Mathf.Max(0, Mathf.Min(y0, y1));
        int maxY = Mathf.Min(tex.height - 1, Mathf.Max(y0, y1));
        for (int y = minY; y <= maxY; y++)
            for (int x = minX; x <= maxX; x++)
                tex.SetPixel(x, y, c);
    }

    static void FillEllipse(Texture2D tex, float cx, float cy, float rx, float ry, Color c)
    {
        int minX = Mathf.Max(0, Mathf.FloorToInt(cx - rx - 1f));
        int maxX = Mathf.Min(tex.width - 1, Mathf.CeilToInt(cx + rx + 1f));
        int minY = Mathf.Max(0, Mathf.FloorToInt(cy - ry - 1f));
        int maxY = Mathf.Min(tex.height - 1, Mathf.CeilToInt(cy + ry + 1f));
        float rx2 = rx * rx;
        float ry2 = ry * ry;
        if (rx2 < 0.0001f || ry2 < 0.0001f)
            return;
        for (int y = minY; y <= maxY; y++)
        {
            for (int x = minX; x <= maxX; x++)
            {
                float dx = x - cx;
                float dy = y - cy;
                if (dx * dx / rx2 + dy * dy / ry2 <= 1f)
                    tex.SetPixel(x, y, c);
            }
        }
    }

    static void FillPolygon(Texture2D tex, Vector2[] poly, Color c)
    {
        if (poly == null || poly.Length < 3)
            return;

        float minXf = poly[0].x;
        float maxXf = poly[0].x;
        float minYf = poly[0].y;
        float maxYf = poly[0].y;
        for (int i = 1; i < poly.Length; i++)
        {
            minXf = Mathf.Min(minXf, poly[i].x);
            maxXf = Mathf.Max(maxXf, poly[i].x);
            minYf = Mathf.Min(minYf, poly[i].y);
            maxYf = Mathf.Max(maxYf, poly[i].y);
        }

        int minX = Mathf.Max(0, Mathf.FloorToInt(minXf));
        int maxX = Mathf.Min(tex.width - 1, Mathf.CeilToInt(maxXf));
        int minY = Mathf.Max(0, Mathf.FloorToInt(minYf));
        int maxY = Mathf.Min(tex.height - 1, Mathf.CeilToInt(maxYf));

        for (int y = minY; y <= maxY; y++)
        {
            for (int x = minX; x <= maxX; x++)
            {
                if (PointInPolygon(new Vector2(x + 0.5f, y + 0.5f), poly))
                    tex.SetPixel(x, y, c);
            }
        }
    }

    static bool PointInPolygon(Vector2 p, Vector2[] poly)
    {
        bool inside = false;
        int n = poly.Length;
        for (int i = 0, j = n - 1; i < n; j = i++)
        {
            if ((poly[i].y > p.y) != (poly[j].y > p.y) &&
                p.x < (poly[j].x - poly[i].x) * (p.y - poly[i].y) / (poly[j].y - poly[i].y) + poly[i].x)
                inside = !inside;
        }

        return inside;
    }

    static float FracSin01(int seed, float mul, float x)
    {
        float t = Mathf.Sin(seed * 0.019f + x * mul);
        return t * 0.5f + 0.5f;
    }
}
