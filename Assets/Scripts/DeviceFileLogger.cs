using System;
using System.IO;
using System.Text;
using UnityEngine;

// Diagnostic helper that writes all runtime logs (and stack traces for
// errors/exceptions) into a text file under Application.persistentDataPath.
// Purpose: capture device-specific errors (e.g. on old phones like LG V30)
// without needing an ADB/logcat connection. The file can be pulled via MTP.
//
// No scene setup needed: RuntimeInitializeOnLoadMethod starts the logger
// before the first scene loads, so it also captures LogoScene Firebase init.
//
// Only compiled into development/editor builds, never into release builds.
public static class DeviceFileLogger
{
    private static readonly object fileLock = new object();
    private static string logFilePath;
    private static bool initialized = false;

    // Stop writing after this many entries so a per-frame error cannot fill storage.
    private const int MaxEntries = 1000;
    private static int entryCount = 0;

#if DEVELOPMENT_BUILD || UNITY_EDITOR
    // Runs before any scene loads -> captures everything from the very start.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Init()
    {
        if (initialized) return;
        initialized = true;

        logFilePath = Path.Combine(Application.persistentDataPath, "croxcro_log.txt");

        // Overwrite the previous session log at each launch.
        try
        {
            lock (fileLock)
            {
                string header =
                    "=== CROxCRO device log ===\n" +
                    "Start: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "\n" +
                    "Device: " + SystemInfo.deviceModel + "\n" +
                    "OS: " + SystemInfo.operatingSystem + "\n" +
                    "GraphicsAPI: " + SystemInfo.graphicsDeviceType + "\n" +
                    "Path: " + logFilePath + "\n\n";
                File.WriteAllText(logFilePath, header);
            }
        }
        catch
        {
            // Ignore: logging must never break the game.
        }

        // Threaded version also catches logs raised on background threads
        // (Firebase callbacks often run off the main thread).
        Application.logMessageReceivedThreaded += HandleLog;
    }

    private static void HandleLog(string message, string stackTrace, LogType type)
    {
        if (entryCount >= MaxEntries) return;

        try
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("[").Append(DateTime.Now.ToString("HH:mm:ss")).Append("] ");
            sb.Append(type.ToString()).Append(": ").Append(message).Append("\n");

            // Include stack trace only for the entries that matter for debugging.
            if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
            {
                if (!string.IsNullOrEmpty(stackTrace))
                {
                    sb.Append(stackTrace).Append("\n");
                }
            }

            lock (fileLock)
            {
                if (entryCount >= MaxEntries) return;
                entryCount++;
                File.AppendAllText(logFilePath, sb.ToString());
            }
        }
        catch
        {
            // Ignore: logging must never break the game.
        }
    }
#endif
}