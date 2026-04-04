using System;
using System.IO;
using UnityEngine;

/// <summary>NDJSON debug ingest for debug session 22c654 (workspace root).</summary>
public static class AgentDebugLog
{
    static string LogPath =>
        Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", "debug-22c654.log"));

    public static void Write(string hypothesisId, string location, string message, string dataJsonObject)
    {
        long ts = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        string line =
            "{\"sessionId\":\"22c654\",\"hypothesisId\":\"" + hypothesisId +
            "\",\"location\":\"" + location + "\",\"message\":\"" + message +
            "\",\"data\":" + dataJsonObject + ",\"timestamp\":" + ts + "}\n";
        try
        {
            File.AppendAllText(LogPath, line);
        }
        catch
        {
            // ignore logging failures (read-only FS, etc.)
        }
    }

    public static string J(string s)
    {
        if (string.IsNullOrEmpty(s))
            return "\"\"";
        return "\"" + s.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";
    }

    public static void WriteObj(string hypothesisId, string location, string message, string key, string value)
    {
        Write(hypothesisId, location, message, "{" + J(key) + ":" + J(value) + "}");
    }
}
