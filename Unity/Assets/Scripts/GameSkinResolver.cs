using System.IO;
using UnityEngine;

/// <summary>
/// Resolves final load path: non-empty inspector override wins, else saved skin under StreamingAssets/Skins.
/// </summary>
public static class GameSkinResolver
{
    public static string ResolvePathForLoader(string inspectorOverride, GameSkinSlot slot)
    {
        if (!string.IsNullOrWhiteSpace(inspectorOverride))
            return inspectorOverride.Trim();

        string rel = GameSkinStore.GetStreamingPathOrNull(slot);
        if (string.IsNullOrEmpty(rel))
            return string.Empty;

        string full = Path.Combine(Application.streamingAssetsPath, rel.Replace('/', Path.DirectorySeparatorChar));
        if (!File.Exists(full))
            return string.Empty;
        if (!GameSkinCompatibility.TryValidateImageFile(full, out _))
            return string.Empty;

        return rel;
    }
}
