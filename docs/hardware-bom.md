# 硬體物料清單與電聲鏈規格 (Hardware Bill of Materials & Electroacoustic Chain)

> 文件版本：v1.0.0  
> 階段：Phase 0 需求凍結與法規界線  
> 適用軟體：專業純音聽力測驗工具 (HearingTest)

---

## 1. 運算系統與作業環境 (Computing Platform)

| 項目 | 規格要求 | 驗證狀態 | 備註 |
| :--- | :--- | :--- | :--- |
| **作業系統** | Windows 10 (64-bit) 21H2 或更高 / Windows 11 (64-bit) | 通過 | 支援 100%, 125%, 150% DPI 縮放 |
| **執行環境** | .NET 8.0 桌面執行時期（發布為 Self-Contained，不要求使用者預裝） | 通過 | 產出單一或免安裝目錄 `HearingTest.exe` |
| **輸出架構** | x64 原生架構，WinExe（嚴禁 Console 視窗） | 通過 | |

---

## 2. 音訊傳輸與數位輸出 (Audio Transmission & Digital Pipeline)

| 項目 | 規格要求 | 驗證狀態 | 備註 |
| :--- | :--- | :--- | :--- |
| **音訊驅動模式** | Windows Audio Session API (WASAPI) Exclusive Mode | 通過 | 繞過 Windows 混音器、杜比空間音效、通訊衰減 |
| **取樣率 (Sample Rate)** | 固定 48,000 Hz | 通過 | 測試中偵測取樣率改變立即終止測驗 |
| **位元深度 (Bit Depth)** | 固定 24-bit PCM 或 32-bit Float | 通過 | |
| **聲道隔離度** | 左右聲道分離獨立 Bus，串音 (Crosstalk) 衰減 > 80 dB | 待測 | 數位匯流排完全獨立；類比端依硬體量測 |
| **音訊硬體指紋** | 鎖定裝置 Endpoint ID、驅動程式版本、FriendlyName | `PENDING_FINGERPRINT` | 若裝置插拔變更，校正檔立即判定失效 |

---

## 3. 換能器與校正耦合器清單 (Transducers & Acoustic Couplers)

> [!WARNING]
> 更換以下清單中的任何項目（包括音效卡耳機孔、轉接頭、耳罩海綿墊），原校正檔立即作廢！

| 設備類別 | 建議/標準型號 | 當前專案配置 | 狀態 / BLOCKER |
| :--- | :--- | :--- | :--- |
| **臨床參考耳機** | Telephonics TDH-39P (搭配 MX41/AR 氣墊) 或 Sennheiser HDA 300 | 尚未取得指定臨床專用耳機 | `BLOCKER` (臨床版必要) |
| **研究版耳機** | 封閉式監聽耳機 (Sony MDR-7506 / Sennheiser HD 280 Pro) | 預設軟體模擬 / 一般立體聲耳機 | `RESEARCH_MODE_ONLY` (僅限研究版) |
| **聲學耦合器 / 人工耳** | IEC 60318-1 (Type 1 人工耳) / IEC 60318-3 (6cc 耦合器) | 尚未送校正實驗室 | `BLOCKER` (輸出 dB HL 前必須) |
| **精密聲音計 / 麥克風** | Class 1 聲級計 (如 B&K 2250 / Larson Davis) | 待校正實驗室出具校驗報告 | `BLOCKER` |
| **受測者專用按鍵** | USB 免驅 HID 回應按鈕 / 鍵盤 Space 鍵 | 支援鍵盤空白鍵與螢幕大型按鍵 | 通過 |
