using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Linq; // 需要 System.Linq 來使用 Any()

/// <summary>
/// Log 的等級分類。
/// </summary>
public enum LogLevel
{
    Info,    // 一般資訊
    Warning, // 警告
    Error    // 錯誤
}

/// <summary>
/// 靜態輔助類別，用於產生帶有分類標籤和等級的 Log。
/// 建議在專案中都透過這個類別來寫 Log。
/// </summary>
public static class Log
{
    /// <summary>
    /// 寫入一筆 Log。
    /// </summary>
    /// <param name="level">Log 的等級。</param>
    /// <param name="category">Log 的分類 (字串)。</param>
    /// <param name="message">Log 訊息。</param>
    public static void Write(LogLevel level, string category, string message)
    {
        string formattedMessage = $"[{category}] {message}";
        switch (level)
        {
            case LogLevel.Info:
                Debug.Log(formattedMessage);
                break;
            case LogLevel.Warning:
                Debug.LogWarning(formattedMessage);
                break;
            case LogLevel.Error:
                Debug.LogError(formattedMessage);
                break;
        }
    }
}


/// <summary>
/// 處理遊戲內 Log 的收集、管理與存檔。
/// 這是一個標準 C# 類別，不繼承 MonoBehaviour。
/// 使用靜態實例 (Singleton) 模式，需手動初始化。
/// </summary>
public class LogManager
{
    // --- Singleton 實例 ---
    private static readonly System.Lazy<LogManager> _instance =
        new System.Lazy<LogManager>(() => new LogManager());
    public static LogManager Instance => _instance.Value;

    // --- 設定 ---
    private int _maxGeneralLogCount = 100;
    private string _logFilePrefix = "LogSpace";
    private string _logDirectoryName = "LogSpace";
    // 使用 HashSet 來儲存要監聽的 Log 分類 (字串)，查詢效能高
    private HashSet<string> _activeCategories = new HashSet<string>();
    private LogLevel _minimumLogLevel = LogLevel.Info;

    // --- 資料容器 ---
    private readonly Queue<string> _generalLogs = new Queue<string>();
    private readonly List<string> _errorLogs = new List<string>();

    // 用於鎖定，避免多執行緒同時寫入 Log 造成問題
    private readonly object _threadLock = new object();

    private LogManager() { }

    #region 初始化與關閉

    /// <summary>
    /// 初始化 LogManager，開始監聽 Log 事件。
    /// </summary>
    /// <param name="categoriesToLog">要監聽的 Log 分類 (字串)。如果為 null，則監聽所有分類。</param>
    /// <param name="minimumLevel">要監聽的最低 Log 等級。</param>
    /// <param name="maxLogs">設定一般日誌的最大筆數。</param>
    /// <param name="logFilePrefix">Log 檔案的前綴字串。</param>
    /// <param name="logDirectoryName">Log 檔案儲存的目錄名稱。</param>
    public void Initialize(IEnumerable<string> categoriesToLog = null, LogLevel minimumLevel = LogLevel.Info, int maxLogs = 100, string logFilePrefix = "LogSpace", string logDirectoryName = "GameLogs")
    {
        _maxGeneralLogCount = maxLogs;
        _minimumLogLevel = minimumLevel;
        _logFilePrefix = logFilePrefix;
        _logDirectoryName = logDirectoryName;

        if (categoriesToLog == null)
        {
            _activeCategories = null; // null 代表監聽所有分類
            Debug.Log($"[LogManager] Initialized. Listening to all categories with minimum level: {minimumLevel}.");
        }
        else
        {
            _activeCategories = new HashSet<string>(categoriesToLog);
            Debug.Log($"[LogManager] Initialized. Listening to categories [{string.Join(", ", _activeCategories)}] with minimum level: {minimumLevel}.");
        }

        Application.logMessageReceived += HandleLog;
    }

    /// <summary>
    /// 關閉 LogManager，停止監聽。
    /// </summary>
    public void Shutdown()
    {
        Application.logMessageReceived -= HandleLog;
        Debug.Log("[LogManager] Shutdown and stopped listening.");
    }

    #endregion

    #region 公開方法

    /// <summary>
    /// 將目前收集到的所有 Log 儲存到檔案中。
    /// </summary>
    public void SaveLogsToFile()
    {
        string path = GetLogDirectoryPath();
        string timestamp = System.DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        string filePath = Path.Combine(path, $"{_logFilePrefix}_{timestamp}.txt");

        StringBuilder logContent = new StringBuilder();
        logContent.AppendLine($"Log generated on {System.DateTime.Now}");
        string categories = _activeCategories == null ? "All" : string.Join(", ", _activeCategories);
        logContent.AppendLine($"Listening to categories: [{categories}] with minimum level: {_minimumLogLevel}");
        logContent.AppendLine();

        logContent.AppendLine("========== ERROR LOGS (強制紀錄) ==========");
        logContent.AppendLine("==========================================");
        lock (_threadLock)
        {
            foreach (var log in _errorLogs)
            {
                logContent.AppendLine(log);
            }
        }

        logContent.AppendLine();
        logContent.AppendLine($"========== GENERAL LOGS (最多 {_maxGeneralLogCount} 筆) ==========");
        logContent.AppendLine("===============================================");
        lock (_threadLock)
        {
            foreach (var log in _generalLogs)
            {
                logContent.AppendLine(log);
            }
        }

        try
        {
            File.WriteAllText(filePath, logContent.ToString());
            Log.Write(LogLevel.Info, "System", $"Log 檔案已儲存至: {filePath}");
        }
        catch (System.Exception e)
        {
            Log.Write(LogLevel.Error, "System", $"Log 檔案儲存失敗: {e.Message}");
        }
    }

    /// <summary>
    /// 擷取目前遊戲畫面並存檔，會根據平台儲存到不同子資料夾。
    /// </summary>
    public void CaptureAndSaveScreenshot()
    {
        string platformSubfolder;
#if UNITY_ANDROID || UNITY_IOS
        platformSubfolder = "Mobile";
#else
        platformSubfolder = "PC";
#endif

        string path = GetScreenshotDirectoryPath(platformSubfolder);
        string timestamp = System.DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        string filePath = Path.Combine(path, $"Screenshot_{timestamp}.png");

        try
        {
            ScreenCapture.CaptureScreenshot(filePath);
            Log.Write(LogLevel.Info, "System", $"畫面已擷取並儲存至: {filePath}");
        }
        catch (System.Exception e)
        {
            Log.Write(LogLevel.Error, "System", $"畫面擷取失敗: {e.Message}");
        }
    }

    #endregion

    #region 私有方法

    /// <summary>
    /// Log 事件的處理函式，所有 Log 都會經過這裡。
    /// </summary>
    private void HandleLog(string logString, string stackTrace, LogType type)
    {
        // 1. 檢查 Log 等級是否符合設定
        if (!IsLogLevelSufficient(type))
        {
            return;
        }

        // 2. 嘗試解析 Log 分類
        if (!TryParseCategory(logString, out string category, out string actualMessage))
        {
            return; // 如果沒有分類標籤，則忽略
        }

        // 3. 檢查該分類是否是我們要監聽的 (如果 _activeCategories 不為 null)
        if (_activeCategories != null && !_activeCategories.Contains(category))
        {
            return; // 不在監聽列表內，忽略
        }

        string formattedLog = $"[{System.DateTime.Now:HH:mm:ss}][{type}][{category}] {actualMessage}";

        lock (_threadLock)
        {
            switch (type)
            {
                case LogType.Error:
                case LogType.Exception:
                    _errorLogs.Add(formattedLog + $"\nStack Trace:\n{stackTrace}");
                    break;

                case LogType.Log:
                case LogType.Warning:
                case LogType.Assert:
                    _generalLogs.Enqueue(formattedLog);
                    while (_generalLogs.Count > _maxGeneralLogCount)
                    {
                        _generalLogs.Dequeue();
                    }
                    break;
            }
        }
    }

    /// <summary>
    /// 檢查傳入的 Unity LogType 是否達到設定的最低等級。
    /// </summary>
    private bool IsLogLevelSufficient(LogType type)
    {
        LogLevel level;
        switch (type)
        {
            case LogType.Log:
                level = LogLevel.Info;
                break;
            case LogType.Warning:
                level = LogLevel.Warning;
                break;
            case LogType.Error:
            case LogType.Exception:
            case LogType.Assert:
                level = LogLevel.Error;
                break;
            default:
                level = LogLevel.Info;
                break;
        }
        return level >= _minimumLogLevel;
    }

    /// <summary>
    /// 嘗試從 Log 字串中解析出分類和實際訊息。
    /// </summary>
    private bool TryParseCategory(string logString, out string category, out string message)
    {
        category = null;
        message = logString;

        if (!logString.StartsWith("[")) return false;

        int closingBracketIndex = logString.IndexOf(']');
        if (closingBracketIndex == -1) return false;

        category = logString.Substring(1, closingBracketIndex - 1);
        message = logString.Substring(closingBracketIndex + 1).TrimStart();
        return true;
    }

    /// <summary>
    /// 取得並確保 Log 儲存的根目錄路徑存在。
    /// </summary>
    private string GetLogDirectoryPath()
    {
        string path = Path.Combine(Application.persistentDataPath, _logDirectoryName);
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }
        return path;
    }

    /// <summary>
    /// 取得並確保截圖儲存的平台專屬子目錄路徑存在。
    /// </summary>
    private string GetScreenshotDirectoryPath(string platformSubfolder)
    {
        string rootPath = GetLogDirectoryPath();
        string screenshotPath = Path.Combine(rootPath, platformSubfolder);
        if (!Directory.Exists(screenshotPath))
        {
            Directory.CreateDirectory(screenshotPath);
        }
        return screenshotPath;
    }

    #endregion
}
