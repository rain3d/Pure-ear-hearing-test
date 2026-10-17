# 更新日誌 (CHANGELOG)

本專案遵循 [Semantic Versioning](https://semver.org/lang/zh-TW/) 與 [README.md](file:///d:/AI_code/耳朵聽力測驗_Antigravity/README.md) 分階段規範。

---

## [2.2.0] - 2026-10-17 (完整多語系支援：繁中、簡中、英文即時切換與持久化)

### 新增 (Added)
- **WPF 桌面端雙畫面全介面多語系支援 (zh-TW, zh-CN, en-US)**：
  - 建立 `LocalizationService` 與 `LanguageOption`，支援以 `DynamicResource` 於執行階段無縫熱切換語言，不需重啟程式。
  - 新增三組完整 XAML 字串資源字典：`Strings.zh-TW.xaml`（繁體中文預設）、`Strings.zh-CN.xaml`（簡體中文）、`Strings.en-US.xaml`（英文）。
  - 全面覆蓋主視窗、首頁、操作者控制台、受測者專用副螢幕視窗 (`PatientWindow`)、校正視窗與歷史紀錄。
  - `AudiogramCanvas` 自繪圖表（頻率座標、dB HL 聽力圖例、dB Rel 浮水印）與 `MainViewModel` 動態狀態文字、聽力受損等級等全面語系化。
  - 偏好語言自動持久化儲存於本機偏好設定 (`%LOCALAPPDATA%\HearingTest\preferences.json`)，下次開啟自動還原。
- **手機版 Web PWA 介面多語系支援**：
  - 建立 `mobile-web/js/i18n.js`，收錄 83 個語系鍵值，涵蓋繁中、簡中、英文。
  - 首頁頂部導航列新增語言切換選單，支援切換後即時同步更新所有文字、動態狀態與聽力圖 Canvas 座標標籤。
  - 語言偏好儲存於瀏覽器 `localStorage`。
  - PWA 快取升級至 `hearing-test-pwa-v7-i18n` 並加入 `./js/i18n.js` 離線支援。
  - 新增 Node.js 單元測試 `mobile-web/tests/i18n.test.js`，100% 驗證三語系字典完整度與同步。

---

## [2.1.0] - 2026-09-14 (手機版與核心狀態機 0 dB HL 聽閾卡死修復 & PWA 快取更新)

### 修復 (Fixed)
- **手機版與核心狀態機 0 dB HL 底限判定修復**：
  - 修復受測者聽力正常時，測試從 30 dB 下降至 0 dB HL 會無限卡在 1000 Hz 0 dB 無法前進至下一頻率的缺陷。
  - 於 `mobile-web/js/state-machine.js` 與 `StandardThresholdProtocol.cs` 實作底限 0 dB HL 之 2-of-3 確認原則，於 0 dB HL 累計聽見 2 次立即確立聽閾為 0 dB HL 並前進至下一頻率。
- **PWA Service Worker 快取機制升級**：
  - `sw.js` 升級至 `hearing-test-pwa-v6`，改採 Network-First 策略，防止手機端被舊版本快取綁架。
  - `啟動手機版測驗.bat` 升級支援 UTF-8 編碼、Node.js 環境檢測與清晰連接指引。

---

## [2.0.0] - 2026-09-10 (Phase 4, Phase 5 & Phase 7 全功能整合與可攜發布交付)

### 新增 (Added)
- **Phase 5 對側窄頻遮蔽噪音引擎 (Contralateral Narrowband Masking)**：
  - `AudioSignalGenerator`：實作符合 IEC 60645-1 之 1/3 倍頻程帶通濾波窄頻噪音 (NBN) 合成與等效 RMS 能量校準。
  - `StandardMaskingProtocol`：實作標準跨耳衰減 (IA = 40 dB) 門檻判定與初階遮蔽量推薦演算法。
  - `WasapiAudioOutput`：實作 `PlayToneWithMaskingAsync`，測左耳出右耳雜音、測右耳出左耳雜音，雙聲道獨立 Raised-Cosine 平滑無 Click，嚴格防範同耳遮蔽。
  - 新增 `MaskingTests` 涵蓋立體聲路由、聲道能量分布與同側防呆驗證。
- **Phase 4 操作者／受測者雙畫面與臨床報告深化**：
  - `PatientWindow`：新增受測者副螢幕專用視窗，支援次級顯示器全螢幕模式，極簡大按鈕與觸控支援，隱藏分貝與頻率符合聽力學盲測規範。
  - `OperatorTestView`：新增對側遮蔽即時控制抽屜、自動推薦值套用按鈕、副螢幕啟動按鈕、事件時間軸遮蔽參數欄位。
  - `ReportExporter`：匯出報告全面包含遮蔽狀態、可靠度評估、環境狀態與未校正研究警語。
- **Phase 7 免安裝可攜發布包 (Portable Standalone Delivery)**：
  - 以 Release `win-x64` self-contained 發布至 `publish/HearingTest/`，完全不依賴系統安裝之 .NET SDK 或 Runtime。
  - 提供第三方開源授權清單 `THIRD_PARTY_LICENSES.md`（NAudio MIT、SQLite MIT 等）。
  - 更新完整使用者操作手冊 `README_使用說明.md`。
  - 單元測試套件通過數達 37 項，100% 綠燈。

---

## [1.2.0] - 2026-09-10 (Phase 3 校正系統與防偽簽章交付)

### 新增 (Added)
- **校正檔數位簽章與防竄改 (SHA-256 + HMAC)**：
  - `CalibrationService`：實作防竄改校驗，比對檔案內容雜湊與數位簽章；手動竄改檔案數值（哪怕僅 1 個 byte）自動阻擋載入。
- **本機硬體電聲指紋綁定 (Hardware Fingerprint)**：
  - 結合主機名稱、音訊裝置端點、耳機型號與左右單體序號生成 SHA-256 指紋；若更換耳機或驅動，系統自動判定校正失效並退回研究模式。
- **逐頻率 RETSPL 聲壓換算與到期防呆**：
  - 完整支援 250, 500, 1000, 2000, 4000, 8000 Hz 左右耳校正點；未校正時強制鎖定為研究相對刻度（dBFS/Rel）。
  - 超過 1 年校驗效期自動阻擋臨床模式。
- **UI 校正管理中心升級**：
  - 支援外部 JSON 校正檔匯入、本機電聲校驗檔產生器、即時校正數據表檢視與指紋狀態燈號。
- **自動化單元測試擴充**：
  - 總測試數增至 29 項（包含防竄改測試、硬體變更阻擋測試、過期測試、檔案存取往返測試與 RETSPL 換算精度測試），100% 通過。

---

## [1.1.0] - 2026-09-10 (Phase 2 音訊引擎與輸出保護交付)

### 新增 (Added)
- **即時純音與脈衝音合成引擎**：
  - `AudioSignalGenerator`：固定 48,000 Hz，連續相位正弦波生成器，涵蓋 250, 500, 1000, 2000, 4000, 8000 Hz。
  - `WasapiAudioOutput`：實作 NAudio WASAPI Exclusive / Shared 驅動輸出，支援低延遲立體聲緩衝播放。
- **無 Click 平滑窗口與聲道隔離**：
  - Raised-Cosine（升餘弦）平滑窗口（25 ms 升降），消除開關瞬間暫態 Click 破音。
  - 分耳獨立 Bus：左耳輸出時右聲道嚴格為零，右耳輸出時左聲道嚴格為零，數位串音為 0。
  - 脈衝音序列：200 ms 開 / 200 ms 關臨床標準脈衝序列。
- **安全互鎖與輸出防護**：
  - 數位安全硬上限 `-10 dBFS`，禁止全刻度過載輸出，徹底防止 Clipping 破音。
  - 支援全域緊急停止（Esc 鍵或 UI 按鈕）與裝置異常 5 ms 內強制硬體靜音。
- **自動化 DSP 測試套件**：
  - 新增 11 項音訊專屬單元測試（總測試數由 16 項增至 27 項），頻率精準度誤差 < 0.05%、聲道隔離度 100%、平滑無 Click、安全上限保護皆 100% 通過。

---

## [1.0.0] - 2026-09-10 (Phase 1 專案骨架與模擬器交付)

### 新增 (Added)
- **.NET 8 WPF 桌面專案骨架**：
  - `HearingTest.slnx` 整合 8 個分層專案（App, Domain, Audio, Calibration, Persistence, Reporting, Hardware, Tests）。
  - `HearingTest.App`：WinExe 輸出（無 Console 視窗），自訂高解析度多尺寸 `app.ico`（16~256 px 醫療波形圖示）。
  - `DesignSystem.xaml`：專業醫療儀器深色風格設計系統、高對比顏色、SVG 向量圖示。
- **純音測驗狀態機 (AudiometryStateMachine)**：
  - 嚴格實作 ISO 8253-1 / ASHA 標準：10-down / 5-up 步進、2-of-3 上升序列聽閾確立規則。
  - 0~50 dB HL 安全白名單邊界鎖死；50 dB HL 連續無反應自動判定為 `NR @ 50 dB HL`。
  - 隨機插入 Catch Trial（靜音對照組）紀錄假陽性，不計入聽閾次數。
  - 1000 Hz 複測差異比對機制（超過 5 dB 觸發可靠度警語）。
  - 支援暫停、繼續、回退 (Rollback) 與人工覆核，並自動寫入 `AuditLog`。
- **音訊與受測者模擬器**：
  - `SimulatedAudioOutput`：實作 `IAudioOutput`，支援即時靜音與非同步刺激記錄。
  - `SimulatedPatient`：支援心理物理累積常態/邏輯斯心理計量曲線模擬，支援 `LeftEarVerification` 使用者預設聽閾對照，支援固定亂數種子重現。
- **資料持久層與報告匯出**：
  - `SqliteSessionRepository`：開啟外鍵與 WAL 模式，支援病患資料、刺激事件、聽閾結果與稽核日誌儲存。
  - `ReportExporter`：支援完整 JSON、CSV 匯出，支援匿名研究資料匯出（強制清除姓名、身分識別等 PII）。
- **單元測試套件 (HearingTest.Tests)**：
  - 16 項自動化單元測試 100% 通過（包含 10-down/5-up、2-of-3、Catch trial、1000Hz 複驗、校正檔驗證與過期阻擋、指紋比對、SQLite 資料庫與匿名匯出）。
- **Windows 發布包**：
  - 成功以 Release `win-x64` self-contained 發布 `publish/HearingTest/HearingTest.exe`，內建使用說明與圖示。

---

## [0.1.0] - 2026-09-10 (Phase 0 需求凍結與法規界線)

### 新增 (Added)
- **法規界線與預期用途**：建立 `docs/intended-use.md`，定義研究／個人追蹤版之非醫療診斷界線。
- **硬體物料清單**：建立 `docs/hardware-bom.md`，明定 Windows 10/11 64-bit、WASAPI Exclusive 要求，將缺少之臨床電聲耦合器與 RETSPL 標記為 `BLOCKER`。
- **風險登錄與安全互鎖**：建立 `docs/risk-register.md`，列舉 7 大危害情境與五重安全互鎖機制。
