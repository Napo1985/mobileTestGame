using UnityEngine;

/// <summary>
/// Procedural starfield + Milky Way band (no art assets), plus optional lightweight parallax star tiles.
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

    /// <summary>Deterministic 0–1 hash for procedural pixels (shared by main sky + star tiles).</summary>
    static float Hash01(int a, int b, int c)
    {
        uint h = (uint)(a * 374761393 + b * 668265263 + c * 1442695041);
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

    /// <summary>
    /// Scale a child SpriteRenderer so it covers the ortho view; does not move transform (use under backdrop root).
    /// </summary>
    public static void FitSpriteLocalToOrthoCamera(SpriteRenderer sr, Camera cam, float margin = 1.18f)
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
    }

    /// <summary>
    /// Sparse star tile for scrolling parallax (cheap: point-filtered small texture, alpha stars only).
    /// </summary>
    public static Sprite CreateStarTileSprite(int size = 128, int salt = 0, float pixelsPerUnit = 40f)
    {
        size = Mathf.Clamp(size, 64, 256);
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Repeat;
        tex.filterMode = FilterMode.Point;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
                tex.SetPixel(x, y, Color.clear);
        }

        int count = Mathf.RoundToInt(size * size * 0.0018f);
        for (int i = 0; i < count; i++)
        {
            int px = (int)(Hash01(i, salt, 1) * size) % size;
            int py = (int)(Hash01(i, salt, 2) * size) % size;
            float br = 0.35f + Hash01(i, salt, 3) * 0.65f;
            tex.SetPixel(px, py, new Color(br, br * 0.95f, 1f, br));
            if (Hash01(i, salt, 4) < 0.22f && px + 1 < size)
                tex.SetPixel(px + 1, py, new Color(br * 0.5f, br * 0.5f, 1f, br * 0.45f));
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

    /// <summary>
    /// Parents two scrolling star planes under <paramref name="backdropRoot"/> (expects root already placed at camera).
    /// </summary>
    public static void AddScrollingStarLayers(
        GameObject backdropRoot,
        Camera cam,
        Color tintMultiply,
        float farWorldScrollSpeed = 0.11f,
        float nearWorldScrollSpeed = 0.24f)
    {
        if (backdropRoot == null || cam == null)
            return;

        var farGo = new GameObject("ParallaxStarsFar");
        farGo.transform.SetParent(backdropRoot.transform, false);
        farGo.transform.localPosition = new Vector3(0f, 0f, 0.02f);
        var farScroll = farGo.AddComponent<StarParallaxScroller>();
        farScroll.Build(
            cam,
            CreateStarTileSprite(128, 11, 40f),
            new Color(tintMultiply.r, tintMultiply.g, tintMultiply.b, 0.38f),
            -199,
            farWorldScrollSpeed,
            1.2f);

        var nearGo = new GameObject("ParallaxStarsNear");
        nearGo.transform.SetParent(backdropRoot.transform, false);
        nearGo.transform.localPosition = new Vector3(0f, 0f, 0.03f);
        var nearScroll = nearGo.AddComponent<StarParallaxScroller>();
        nearScroll.Build(
            cam,
            CreateStarTileSprite(128, 97, 40f),
            new Color(
                Mathf.Min(1f, tintMultiply.r * 1.08f),
                Mathf.Min(1f, tintMultiply.g * 1.04f),
                Mathf.Min(1f, tintMultiply.b * 1.06f),
                0.52f),
            -198,
            nearWorldScrollSpeed,
            1.2f);
    }
}

/// <summary>
/// Two stacked star tiles under a scaled parent; scrolls in local space with seamless wrap.
/// </summary>
public class StarParallaxScroller : MonoBehaviour
{
    float _speedWorld;
    float _tileSpanLocal;
    Transform _a;
    Transform _b;
    float _parentSy = 1f;

    public void Build(Camera cam, Sprite sprite, Color color, int sortingOrder, float worldScrollSpeed, float margin = 1.2f)
    {
        if (cam == null || sprite == null)
            return;

        _speedWorld = worldScrollSpeed;
        float halfH = cam.orthographicSize * margin;
        float halfW = halfH * cam.aspect * margin;
        float spriteH = sprite.rect.height / sprite.pixelsPerUnit;
        float spriteW = sprite.rect.width / sprite.pixelsPerUnit;
        if (spriteH < 0.0001f || spriteW < 0.0001f)
            return;

        transform.localScale = new Vector3((halfW * 2f) / spriteW, (halfH * 2f) / spriteH, 1f);
        _parentSy = transform.localScale.y;
        _tileSpanLocal = spriteH;

        var childA = new GameObject("TileA");
        childA.transform.SetParent(transform, false);
        var cpa = childA.AddComponent<SpriteRenderer>();
        cpa.sprite = sprite;
        cpa.color = color;
        cpa.sortingOrder = sortingOrder;
        childA.transform.localScale = Vector3.one;
        childA.transform.localPosition = Vector3.zero;

        var childB = new GameObject("TileB");
        childB.transform.SetParent(transform, false);
        var cpb = childB.AddComponent<SpriteRenderer>();
        cpb.sprite = sprite;
        cpb.color = color;
        cpb.sortingOrder = sortingOrder;
        childB.transform.localScale = Vector3.one;
        childB.transform.localPosition = Vector3.up * _tileSpanLocal;

        _a = childA.transform;
        _b = childB.transform;
    }

    void LateUpdate()
    {
        if (_a == null || _b == null || _parentSy < 0.0001f)
            return;

        float dl = (_speedWorld * Time.deltaTime) / _parentSy;
        _a.localPosition += Vector3.down * dl;
        _b.localPosition += Vector3.down * dl;

        if (_a.localPosition.y < -_tileSpanLocal)
            _a.localPosition += Vector3.up * (_tileSpanLocal * 2f);
        if (_b.localPosition.y < -_tileSpanLocal)
            _b.localPosition += Vector3.up * (_tileSpanLocal * 2f);
    }
}
