using System.IO;
using UnityEngine;

/// <summary>
/// Loads images Unity can decode (PNG, JPG, BMP, TGA, GIF via LoadImage) from:
/// - Absolute paths: C:/images/ship.png
/// - StreamingAssets: my/bg.png -> {StreamingAssets}/my/bg.png
/// - Resources: key without extension, e.g. Ships/Player or Resources/Ships/Player
/// </summary>
public static class RuntimeSpriteLoader
{
    /// <summary>
    /// Default longest-edge world size for menu ship preview (matches gameplay player skin fit).
    /// </summary>
    public const float DefaultPlayerSkinPreviewMaxWorldUnits = 1.38f;

    /// <param name="pixelsPerUnit">Used for Resources loads and for file loads when <paramref name="maxWorldUnitsForFileImport"/> is 0.</param>
    /// <param name="maxWorldUnitsForFileImport">If &gt; 0, file-based loads pick PPU so max(tex width, height) / PPU = this value (keeps 4K skins from filling the screen).</param>
    public static Sprite LoadSpriteFlexible(string pathOrKey, float pixelsPerUnit = 100f, float maxWorldUnitsForFileImport = 0f)
    {
        if (string.IsNullOrWhiteSpace(pathOrKey))
            return null;

        string p = pathOrKey.Trim().Replace('\\', '/');

        if (IsDriveOrRooted(p))
        {
            if (File.Exists(p))
                return LoadFromFilePath(p, pixelsPerUnit, maxWorldUnitsForFileImport);
            Debug.LogWarning($"[RuntimeSpriteLoader] File not found: {p}");
            return null;
        }

        var fromRes = TryLoadFromResources(p, pixelsPerUnit);
        if (fromRes != null)
            return fromRes;

        string streamCombined = Path.Combine(Application.streamingAssetsPath, p.TrimStart('/'));
        if (File.Exists(streamCombined))
            return LoadFromFilePath(streamCombined, pixelsPerUnit, maxWorldUnitsForFileImport);

        Debug.LogWarning($"[RuntimeSpriteLoader] Image not found (Resources or StreamingAssets): {pathOrKey}");
        return null;
    }

    static bool IsDriveOrRooted(string p)
    {
        return Path.IsPathRooted(p) || (p.Length > 1 && p[1] == ':');
    }

    static Sprite TryLoadFromResources(string p, float ppu)
    {
        string key = p;
        if (key.StartsWith("Resources/", System.StringComparison.OrdinalIgnoreCase))
            key = key.Substring("Resources/".Length);

        if (string.IsNullOrEmpty(key))
            return null;

        if (Path.HasExtension(key))
        {
            string dir = Path.GetDirectoryName(key);
            if (!string.IsNullOrEmpty(dir))
                dir = dir.Replace('\\', '/');
            string name = Path.GetFileNameWithoutExtension(key);
            key = string.IsNullOrEmpty(dir) ? name : dir + "/" + name;
        }

        var tex = Resources.Load<Texture2D>(key);
        if (tex == null)
            return null;
        return TextureToSprite(tex, ppu);
    }

    /// <summary>After load, textures are scaled so longest edge is at most this (keeps sprites/colliders sane).</summary>
    const int NormalizedMaxEdge = 4096;

    static Sprite LoadFromFilePath(string fullPath, float ppu, float maxWorldUnitsForFileImport)
    {
        byte[] bytes = File.ReadAllBytes(fullPath);
        var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        if (!tex.LoadImage(bytes))
        {
            Debug.LogWarning($"[RuntimeSpriteLoader] Unsupported or corrupt image: {fullPath}");
            Object.Destroy(tex);
            return null;
        }

        tex.Apply();

        tex = NormalizeTextureDimensions(tex, minEdge: 4, maxEdge: NormalizedMaxEdge);
        if (tex == null)
            return null;

        float usePpu = ppu;
        if (maxWorldUnitsForFileImport > 0.001f)
        {
            int maxPx = Mathf.Max(tex.width, tex.height);
            usePpu = Mathf.Max(1f, maxPx / Mathf.Max(0.05f, maxWorldUnitsForFileImport));
        }

        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;
        tex.Apply();
        return TextureToSprite(tex, usePpu);
    }

    /// <summary>Scales down if larger than maxEdge, or up if smaller than minEdge (preserves aspect).</summary>
    static Texture2D NormalizeTextureDimensions(Texture2D source, int minEdge, int maxEdge)
    {
        if (source == null)
            return null;

        int w = source.width;
        int h = source.height;
        float fw = w;
        float fh = h;

        for (int i = 0; i < 6; i++)
        {
            float mx = Mathf.Max(fw, fh);
            float mn = Mathf.Min(fw, fh);
            if (mx <= maxEdge && mn >= minEdge)
                break;

            if (mx > maxEdge)
            {
                float s = maxEdge / mx;
                fw *= s;
                fh *= s;
            }
            else if (mn < minEdge)
            {
                float s = minEdge / mn;
                fw *= s;
                fh *= s;
            }
        }

        int nw = Mathf.Max(1, Mathf.RoundToInt(fw));
        int nh = Mathf.Max(1, Mathf.RoundToInt(fh));
        if (Mathf.Max(nw, nh) > maxEdge)
        {
            float s = maxEdge / (float)Mathf.Max(nw, nh);
            nw = Mathf.Max(1, Mathf.RoundToInt(nw * s));
            nh = Mathf.Max(1, Mathf.RoundToInt(nh * s));
        }
        if (Mathf.Min(nw, nh) < minEdge)
        {
            float s = minEdge / (float)Mathf.Min(nw, nh);
            nw = Mathf.Max(1, Mathf.RoundToInt(nw * s));
            nh = Mathf.Max(1, Mathf.RoundToInt(nh * s));
            if (Mathf.Max(nw, nh) > maxEdge)
            {
                float t = maxEdge / (float)Mathf.Max(nw, nh);
                nw = Mathf.Max(1, Mathf.RoundToInt(nw * t));
                nh = Mathf.Max(1, Mathf.RoundToInt(nh * t));
            }
        }

        if (nw == w && nh == h)
            return source;

        return ResampleTexture(source, nw, nh);
    }

    static Texture2D ResampleTexture(Texture2D source, int newWidth, int newHeight)
    {
        var prevRt = RenderTexture.active;
        var rt = RenderTexture.GetTemporary(newWidth, newHeight, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
        rt.filterMode = FilterMode.Bilinear;
        Graphics.Blit(source, rt);
        var dest = new Texture2D(newWidth, newHeight, TextureFormat.RGBA32, false);
        RenderTexture.active = rt;
        dest.ReadPixels(new Rect(0, 0, newWidth, newHeight), 0, 0);
        dest.Apply();
        RenderTexture.ReleaseTemporary(rt);
        RenderTexture.active = prevRt;
        Object.Destroy(source);
        return dest;
    }

    public static Sprite TextureToSprite(Texture2D tex, float pixelsPerUnit)
    {
        if (tex == null)
            return null;
        return Sprite.Create(
            tex,
            new Rect(0f, 0f, tex.width, tex.height),
            new Vector2(0.5f, 0.5f),
            pixelsPerUnit,
            0u,
            SpriteMeshType.Tight,
            Vector4.zero,
            true);
    }
}
