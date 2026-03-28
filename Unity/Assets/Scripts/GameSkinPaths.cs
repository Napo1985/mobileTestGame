using System;
using System.IO;
using UnityEngine;

public static class GameSkinPaths
{
    public const string RootFolderName = "Skins";

    public static string GetSlotFolderName(GameSkinSlot slot)
    {
        switch (slot)
        {
            case GameSkinSlot.Player: return "Player";
            case GameSkinSlot.EnemyShip: return "EnemyShip";
            case GameSkinSlot.Asteroid: return "Asteroid";
            case GameSkinSlot.Bullet: return "Bullet";
            case GameSkinSlot.Background: return "Background";
            case GameSkinSlot.PickupHealth: return "PickupHealth";
            case GameSkinSlot.PickupPositive: return "PickupPositive";
            case GameSkinSlot.PickupNegative: return "PickupNegative";
            default: return "Misc";
        }
    }

    public static string GetAbsoluteSlotDirectory(GameSkinSlot slot)
    {
        return Path.Combine(Application.streamingAssetsPath, RootFolderName, GetSlotFolderName(slot));
    }

    /// <summary>
    /// Relative path for RuntimeSpriteLoader under StreamingAssets, e.g. Skins/Player/ship.png
    /// </summary>
    public static string GetStreamingRelativePath(GameSkinSlot slot, string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            return null;
        string folder = GetSlotFolderName(slot);
        return $"{RootFolderName}/{folder}/{fileName}".Replace('\\', '/');
    }

    public static void EnsureSkinDirectoriesExist()
    {
        try
        {
            foreach (var o in Enum.GetValues(typeof(GameSkinSlot)))
            {
                var s = (GameSkinSlot)o;
                string dir = GetAbsoluteSlotDirectory(s);
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning("[GameSkinPaths] Could not create skin folders: " + e.Message);
        }
    }
}
