# LogMessage Unity 專案

本專案提供一套簡易且可擴充的 Unity 遊戲內 Log 管理系統，包含 Log 分級、分類、收集、儲存與截圖功能。

## 主要功能
- 支援 Log 分級（Info、Warning、Error）
- 支援 Log 分類（可自訂分類，僅收集指定分類）
- 自動收集 Unity Log，並可儲存為檔案
- 支援錯誤 Log 強制保存，與一般 Log 保留上限
- 支援遊戲畫面截圖並自動分類儲存
- 可自訂 Log 監聽分類、最低等級與最大筆數

## 主要檔案
- `Assets/Logmessage.cs`：Log 管理核心程式
- `Assets/TestLog.cs`：LogManager 測試腳本

## 使用方式
1. 將 `Logmessage.cs` 加入專案
2. 在遊戲初始化時呼叫：
   ```csharp
   LogManager.Instance.Initialize(new List<string> { "Gameplay", "System" }, LogLevel.Info, 100);
   ```
   - 第一個參數：要監聽的分類（null 代表全部）
   - 第二個參數：最低 Log 等級
   - 第三個參數：一般 Log 最大保留筆數
3. 使用 `Log.Write(level, category, message)` 寫入 Log
4. 可隨時呼叫 `LogManager.Instance.SaveLogsToFile()` 儲存 Log 檔案
5. 可呼叫 `LogManager.Instance.CaptureAndSaveScreenshot()` 擷取遊戲畫面

## 測試
- 可將 `TestLog.cs` 掛在任一場景物件上，執行時會自動測試 LogManager 主要功能

## 注意事項
- 本專案為純 C# 腳本，無需額外套件
- Log 檔案與截圖會儲存在 `Application.persistentDataPath` 下的 `LogSpace` 目錄

## 授權
MIT License
