using UnityEngine;

/// <summary>
/// Procedural starfield + Milky Way band (no art assets).
/// </summary>
public static class SpaceBackdrop
{
    public static Sprite CreateSprite(int size = 400, float pixelsPerUnit = 40f)
    {
        size = Mathf.Clamp(size, 128, 1024);
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;

        var darkBottom = new Color(0.015f, 0.02f, 0.06f, 1f);
        var darkTop = new Color(0.04f, 0.02f, 0.1f, 1f);
        float cos = Mathf.Cos(0.55f);
        float sin = Mathf.Sin(0.55f);

        for (int y = 0; y < size; y++)
        {
            float v = y / (float)Mathf.Max(1, size - 1);
            for (int x = 0; x < size; x++)
            {
                float u = x / (float)Mathf.Max(1, size - 1);
                Color baseCol = Color.Lerp(darkBottom, darkTop, Mathf.Pow(v, 0.85f));

                float dx = u - 0.5f;
                float dy = v - 0.5f;
                float perp = -dx * sin + dy * cos;
                float along = dx * cos + dy * sin;
                float band = Mathf.Exp(-perp * perp / 0.028f) * 0.45f;
                band += Mathf.Exp(-perp * perp / 0.09f) * 0.18f;
                band *= 0.7f + 0.3f * Mathf.PerlinNoise(along * 6f + 2.3f, perp * 14f + 1.1f);
                var milky = new Color(0.45f, 0.5f, 0.72f, 1f) * band;
                Color c = baseCol + milky;

                float h = Hash01(x, y, 1);
                if (h < 0.0008f)
                    c += new Color(1f, 1f, 1f, 0f) * (0.35f + Hash01(x, y, 2) * 0.65f);
                else if (h < 0.008f)
                    c += new Color(0.85f, 0.9f, 1f, 0f) * (0.1f + Hash01(x, y, 3) * 0.18f);
                else if (h < 0.03f)
                    c += new Color(0.6f, 0.75f, 1f, 0f) * 0.06f;

                float nebula = Mathf.PerlinNoise(u * 3.2f + 11f, v * 3.2f + 7f);
                c = Color.Lerp(c, new Color(0.12f, 0.06f, 0.22f, 1f), nebula * 0.12f);

                tex.SetPixel(x, y, c);
            }
        }

        tex.Apply();
        return Sprite.Create(
            tex,
            new Rect(0f, 0f, size, size),
            new Vector2(0.5f, 0.5f),
            pixelsPerUnit,
            0u,
            SpriteMeshType.FullRect,
            Vector4.zero,
            false);
    }

    static float Hash01(int x, int y, int salt)
    {
        uint h = (uint)(x * 374761393 + y * 668265263 + salt * 1442695041);
        h = (h ^ (h >> 13)) * 1274126177u;
        return (h & 0xFFFFFF) / 16777216f;
    }

    public static void SetupRendererForOrthoCamera(SpriteRenderer sr, Camera cam, float margin = 1.15f)
    {
        if (sr == null || cam == null || sr.sprite == null)
            return;
        float halfH = cam.orthographicSize * margin;
        float halfW = halfH * cam.aspect * margin;
        float spriteH = sr.sprite.rect.height / sr.sprite.pixelsPerUnit;
        float spriteW = sr.sprite.rect.width / sr.sprite.pixelsPerUnit;
        if (spriteH < 0.0001f || spriteW < 0.0001f)
            return;
        sr.transform.localScale = new Vector3((halfW * 2f) / spriteW, (halfH * 2f) / spriteH, 1f);
        sr.transform.position = new Vector3(cam.transform.position.x, cam.transform.position.y, 5f);
        sr.sortingOrder = -200;
    }
}
