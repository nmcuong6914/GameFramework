using UnityEngine;

/// <summary>
/// Filtered Debug logger that only shows logs in development builds
/// Automatically disabled in LIVE_BUILD
/// </summary>
public static class FDebug
{
    /// <summary>
    /// Log a message (only in dev builds)
    /// </summary>
    public static void Log(string message)
    {
#if !LIVE_BUILD
        Debug.Log(message);
#endif
    }
    
    /// <summary>
    /// Log a message with context (only in dev builds)
    /// </summary>
    public static void Log(string message, Object context)
    {
#if !LIVE_BUILD
        Debug.Log(message, context);
#endif
    }
    
    /// <summary>
    /// Log a warning (only in dev builds)
    /// </summary>
    public static void LogWarning(string message)
    {
#if !LIVE_BUILD
        Debug.LogWarning(message);
#endif
    }
    
    /// <summary>
    /// Log a warning with context (only in dev builds)
    /// </summary>
    public static void LogWarning(string message, Object context)
    {
#if !LIVE_BUILD
        Debug.LogWarning(message, context);
#endif
    }
    
    /// <summary>
    /// Log an error (shows in all builds - errors should always be visible)
    /// </summary>
    public static void LogError(string message)
    {
        Debug.LogError(message);
    }
    
    /// <summary>
    /// Log an error with context (shows in all builds)
    /// </summary>
    public static void LogError(string message, Object context)
    {
        Debug.LogError(message, context);
    }
    
    /// <summary>
    /// Log an exception (shows in all builds)
    /// </summary>
    public static void LogException(System.Exception exception)
    {
        Debug.LogException(exception);
    }
    
    /// <summary>
    /// Log an exception with context (shows in all builds)
    /// </summary>
    public static void LogException(System.Exception exception, Object context)
    {
        Debug.LogException(exception, context);
    }
    
    /// <summary>
    /// Check if debug logging is enabled
    /// </summary>
    public static bool IsDebugBuild
    {
        get
        {
#if !LIVE_BUILD
            return true;
#else
            return false;
#endif
        }
    }
}
