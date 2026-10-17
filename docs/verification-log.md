# 驗證與驗收紀錄 (Verification Log)

> 專案：專業純音聽力測驗工具 (HearingTest)  
> 基準規範：[README.md](file:///d:/AI_code/耳朵聽力測驗_Antigravity/README.md)

---

## 階段驗收總覽 (Phase Summary)

| 階段 | 說明 | 狀態 | 驗收日期 | 主要成果 / 阻礙標記 |
| :---: | :--- | :---: | :---: | :--- |
| **Phase 0** | 需求凍結與法規界線 | **通過** | 2026-09-10 | 完成 `intended-use.md`, `hardware-bom.md`, `risk-register.md`，所有未定電聲與臨床參數標記為 `BLOCKER` |
| **Phase 1** | 專案骨架與模擬器 | **通過** | 2026-09-10 | 完成分層架構、狀態機、模擬受測者、16 項 xUnit 測試 100% 通過、Release win-x64 `HearingTest.exe` 產出 |
| **Phase 2** | 音訊引擎與輸出保護 | **通過** | 2026-09-10 | 完成 WASAPI 輸出、AudioSignalGenerator、Raised-Cosine 無 Click 平滑、分耳獨立 Bus、27 項測試 100% 通過 |
| **Phase 3** | 校正系統 | **通過** | 2026-09-10 | 完成 SHA-256 簽章防竄改、硬體指紋綁定、RETSPL 查表、過期與變更阻擋臨床模式、29 項測試 100% 通過 |
| **Phase 4** | 純音測驗 UI 與報告 | **通過** | 2026-09-10 | 完成操作者即時控制台、受測者全螢幕副畫面視窗 (`PatientWindow`)、即時聽力圖、JSON/CSV 匿名去識別化匯出、37 項測試通過 |
| **Phase 5** | 遮蔽噪音 | **通過** | 2026-09-10 | 完成對側窄頻噪音 (NBN) 1/3 倍頻程帶通合成、同側遮蔽防呆、跨耳衰減 IA 40 dB 自動警示提示、立體聲同步路由 |
| **Phase 6** | 電聲與臨床驗證 | **外部受限** | - | 本地軟體演算法與回歸測試 100% 完成；硬體電聲耦合器量測需待取得實體人工耳與認證聽力師簽核 (BLOCKER) |
| **Phase 7** | Windows EXE 打包與交付 | **通過** | 2026-09-10 | 完成 Release `win-x64` self-contained 可攜版、免安裝獨立運行、第三方授權清單 `THIRD_PARTY_LICENSES.md` 與使用手冊 |

---

## Phase 5 驗收檢核清單 (對側窄頻遮蔽噪音)
- [x] 標準 1/3 倍頻程窄頻噪音 (NBN) 數位帶通濾波器與能量 RMS 歸一化校正
- [x] 測左耳時：左聲道輸出純音／脈衝音，右聲道輸出對側窄頻噪音
- [x] 測右耳時：右聲道輸出純音／脈衝音，左聲道輸出對側窄頻噪音
- [x] 遮蔽耳不可為測試純音耳（同側防呆互鎖，拋出 `ArgumentException`）
- [x] 跨耳衰減 (Interaural Attenuation, IA = 40 dB) 自動判定與推薦初階遮蔽量
- [x] 操作者介面支援手動啟用遮蔽、手動選擇強度 (0~50 dB HL, 5 dB 階)、一鍵代入推薦量
- [x] 遮蔽歷程與強度逐事件記錄於時間軸、SQLite 資料庫與匯出報告中
- [x] 7 項遮蔽專屬自動化單元測試全數通過

---

## Phase 4 驗收檢核清單 (操作者/受測者雙畫面與臨床報告)
- [x] 操作者控制台：即時聽力圖 (Live Audiogram)、六頻標準順序、5 dB 階步進、即時假陽性率評估
- [x] 受測者專用視窗 (`PatientWindow.xaml`)：支援副螢幕獨立顯示、全螢幕觸控與大按鈕、遮蔽頻率與分貝參數符合盲測規範
- [x] 鍵盤全域熱鍵：空白鍵反應、F5 播放、Esc 緊急停止
- [x] 報告匯出：支援完整 CSV 與 JSON 匯出，具備 PII 去識別化隱私保護選項
- [x] 50 dB HL 無反應時自動記錄 `NR @ 50 dB HL` 並推進

---

## Phase 7 驗收檢核清單 (可攜發布包交付)
- [x] 以 Release `win-x64` self-contained `WinExe` 發布至 `publish/HearingTest/`
- [x] 雙擊 `HearingTest.exe` 直接啟動，無黑色 Console 視窗，工作列與圖示正常
- [x] 包含所有原生執行檔、WPF 引擎與 SQLite 原生二進位 (`e_sqlite3.dll`)
- [x] 包含第三方開源授權說明文件 (`THIRD_PARTY_LICENSES.md`)
- [x] 包含繁體中文詳細使用說明手冊 (`README_使用說明.md`)

---

## Phase 3 驗收檢核清單
- [x] 校正檔 JSON 結構與 SHA-256 / HMAC 數位簽章防竄改
- [x] 檔案修改 1 個 byte 自動觸發損毀/竄改警告並拒絕載入
- [x] 本機電腦、音訊卡、耳機序號硬體指紋比對
- [x] 更換耳機序號或驅動程式時，立即使原校正檔失效並強制降級為研究模式
- [x] 校正檔到期自動阻擋臨床模式
- [x] 逐頻率（250, 500, 1000, 2000, 4000, 8000 Hz）左右耳 RETSPL 標準查表
- [x] 未校正時 UI 強制標註「研究模式 / 未校正」，禁止宣稱 dB HL
- [x] UI 提供校正檔匯入、樣品校正檔產生器與校正點資料表檢視

---

## Phase 2 驗收檢核清單
- [x] 48,000 Hz 固定取樣率與連續相位純音即時合成
- [x] 25 ms Raised-Cosine 升降餘弦平滑窗口，消除暫態 Click 破音
- [x] 臨床脈衝音（200 ms 開 / 200 ms 關）序列生成
- [x] 分耳獨立 Bus：左耳輸出時右聲道嚴格為零，右耳輸出時左聲道嚴格為零
- [x] 數位安全硬上限 `-10 dBFS`，禁止全刻度輸出，避免 Clipping
- [x] 全域緊急停止（Esc 鍵 / UI 按鈕）立即靜音

---

## Phase 1 驗收檢核清單
- [x] .NET 8 分層架構（Domain, Audio, Calibration, Persistence, Reporting, Hardware, Tests, App）
- [x] 輸出類型為 WinExe，無 Console 視窗，自訂 16~256 px `app.ico` 圖示
- [x] ISO 8253-1 / ASHA 10-down / 5-up 步進狀態機
- [x] 2-of-3 上升序列聽閾確認邏輯
- [x] 0~50 dB HL 安全白名單邊界鎖死
- [x] 50 dB HL 無反應自動判定為 `NR @ 50 dB HL`
- [x] Catch trial 假陽性偵測，不計入聽閾
- [x] 1000 Hz 複測差異警告（>5 dB）
- [x] 手動覆核與回退 (Rollback) 自動產生 `AuditLog`
- [x] `SimulatedAudioOutput` 與 `SimulatedPatient` 完成
- [x] 固定亂數種子重現完整受測過程測試通過
- [x] SQLite 資料庫儲存與匿名資料匯出驗證通過
