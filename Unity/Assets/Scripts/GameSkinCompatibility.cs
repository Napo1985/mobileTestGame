using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>
/// Only image files that decode and meet size limits are offered as skins.
/// </summary>
public static class GameSkinCompatibility
{
    const int MinDimension = 4;
    const int MaxDimension = 4096;
    const long MaxFileBytes = 16 * 1024 * 1024;

    static readonly HashSet<string> AllowedExtensions = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase)
    {
        ".png", ".jpg", ".jpeg", ".bmp", ".gif"
    };

    public static bool IsAllowedExtension(string path)
    {
        string ext = Path.GetExtension(path);
        return !string.IsNullOrEmpty(ext) && AllowedExtensions.Contains(ext);
    }

    public static bool TryValidateImageFile(string fullPath, out string failureReason)
    {
        failureReason = null;
        if (string.IsNullOrEmpty(fullPath) || !File.Exists(fullPath))
        {
            failureReason = "missing";
            return false;
        }

        if (!IsAllowedExtension(fullPath))
        {
            failureReason = "extension";
            return false;
        }

        var info = new FileInfo(fullPath);
        if (info.Length > MaxFileBytes)
        {
            failureReason = "too_large";
            return false;
        }

        byte[] bytes = File.ReadAllBytes(fullPath);
        var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        if (!tex.LoadImage(bytes))
        {
            failureReason = "decode";
            Object.Destroy(tex);
            return false;
        }

        if (tex.width < MinDimension || tex.height < MinDimension ||
            tex.width > MaxDimension || tex.height > MaxDimension)
        {
            failureReason = "dimensions";
            Object.Destroy(tex);
            return false;
        }

        Object.Destroy(tex);
        return true;
    }

    public static List<string> ListCompatibleFileNames(string absoluteDirectory)
    {
        var result = new List<string>();
        if (string.IsNullOrEmpty(absoluteDirectory) || !Directory.Exists(absoluteDirectory))
            return result;

        foreach (string full in Directory.GetFiles(absoluteDirectory))
        {
            if (!TryValidateImageFile(full, out _))
                continue;
            result.Add(Path.GetFileName(full));
        }

        result.Sort(System.StringComparer.OrdinalIgnoreCase);
        return result;
    }
}
