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
    public static Sprite LoadSpriteFlexible(string pathOrKey, float pixelsPerUnit = 100f)
    {
        if (string.IsNullOrWhiteSpace(pathOrKey))
            return null;

        string p = pathOrKey.Trim().Replace('\\', '/');

        if (IsDriveOrRooted(p))
        {
            if (File.Exists(p))
                return LoadFromFilePath(p, pixelsPerUnit);
            Debug.LogWarning($"[RuntimeSpriteLoader] File not found: {p}");
            return null;
        }

        var fromRes = TryLoadFromResources(p, pixelsPerUnit);
        if (fromRes != null)
            return fromRes;

        string streamCombined = Path.Combine(Application.streamingAssetsPath, p.TrimStart('/'));
        if (File.Exists(streamCombined))
            return LoadFromFilePath(streamCombined, pixelsPerUnit);

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

    static Sprite LoadFromFilePath(string fullPath, float ppu)
    {
        byte[] bytes = File.ReadAllBytes(fullPath);
        var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        if (!tex.LoadImage(bytes))
        {
            Debug.LogWarning($"[RuntimeSpriteLoader] Unsupported or corrupt image: {fullPath}");
            Object.Destroy(tex);
            return null;
        }

        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;
        tex.Apply();
        return TextureToSprite(tex, ppu);
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
