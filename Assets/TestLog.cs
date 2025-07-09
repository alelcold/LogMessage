using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestLog : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        // 初始化 LogManager，監聽 "Gameplay" 和 "System" 分類，最低等級為 Info，最多保留 50 筆一般 log
        LogManager.Instance.Initialize(new List<string> { "Gameplay", "System" }, LogLevel.Info, 50);

        // 測試寫入不同分類與等級的 log
        Log.Write(LogLevel.Info, "Gameplay", "玩家進入遊戲");
        Log.Write(LogLevel.Warning, "Gameplay", "玩家血量過低");
        Log.Write(LogLevel.Error, "System", "發生嚴重錯誤");
        Log.Write(LogLevel.Info, "Other", "這個分類不會被 LogManager 收集");

        // 測試儲存 log 檔案
        LogManager.Instance.SaveLogsToFile();

        // 測試擷取截圖
        LogManager.Instance.CaptureAndSaveScreenshot();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}