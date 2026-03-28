using UnityEngine;

/// <summary>
/// Persists selected skin file name per slot (empty = use built-in procedural art).
/// </summary>
public static class GameSkinStore
{
    const string KeyPrefix = "GameSkin_v1_";

    public static string GetSelectedFileName(GameSkinSlot slot)
    {
        return PlayerPrefs.GetString(KeyPrefix + slot, string.Empty);
    }

    public static void SetSelectedFileName(GameSkinSlot slot, string fileNameOrEmpty)
    {
        if (string.IsNullOrWhiteSpace(fileNameOrEmpty))
            PlayerPrefs.DeleteKey(KeyPrefix + slot);
        else
            PlayerPrefs.SetString(KeyPrefix + slot, fileNameOrEmpty.Trim());
        PlayerPrefs.Save();
    }

    /// <summary>
    /// StreamingAssets-relative path for RuntimeSpriteLoader, or null for default art.
    /// </summary>
    public static string GetStreamingPathOrNull(GameSkinSlot slot)
    {
        string name = GetSelectedFileName(slot);
        if (string.IsNullOrEmpty(name))
            return null;
        return GameSkinPaths.GetStreamingRelativePath(slot, name);
    }
}
