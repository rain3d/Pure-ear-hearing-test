/**
 * Mobile Web i18n Module (zh-TW, zh-CN, en-US)
 */

export const translations = {
  'zh-TW': {
    // App & Header
    appTitle: '專業純音聽力測驗 (Pure Tone Audiometry)',
    brandTitle: '純音聽力測驗',
    brandSubtitle: 'ISO 8253-1 臨床尋閾流程',
    calibUncalib: '未校正 (相對刻度)',
    calibValid: '已校正 (臨床標準)',

    // Home View
    cardStartTitle: '開始標準雙耳六頻測驗',
    cardStartDesc: '依序檢測 250 ~ 8000 Hz 左右耳氣導聽閾，具備 2 秒實體發聲 (3 秒反應視窗)、隨機 1~3 秒靜音空白防盲猜、對側窄頻遮蔽與即時動態聽力圖。',
    maskingToggleTitle: '對側耳窄頻遮蔽 (Masking)',
    maskingToggleDesc: '測試單耳時，對側非測試耳同步播放窄頻防干擾雜音',
    catchTrialToggleTitle: '對照靜音防盲猜 (Catch Trial)',
    catchTrialToggleDesc: '隨機插入無聲對照試驗，精準抓取未聽見卻盲猜按鍵之假陽性',
    btnStartTest: '🎧 戴上耳機，開始測驗',

    // Sound Check
    soundCheckTitle: '🔊 測前聲音與耳機試聽',
    audioUnlockPrompt: '🟡 輕觸螢幕解鎖音訊',
    audioReadyPrompt: '🟢 音訊核心已就緒',
    soundCheckDesc: '測驗前請先點擊下方試聽按鈕，確認耳機左右聲道發聲正常，並調整手機媒體音量至舒適清晰（建議 50%~70%）。',
    btnTestBoth: '🔊 雙耳純音試聽',
    btnTestNoiseOnly: '🛡️ 單獨遮蔽雜音',
    btnTestLeftMask: '🎧 左耳純音+右耳遮蔽',
    btnTestRightMask: '🎧 右耳純音+左耳遮蔽',

    // Research Statement & Safety Warning
    safetyWarningTitle: '⚠️ 安全警語與免責聲明 (聽力保護重要須知)',
    safetyWarningDesc: '【音量保護原則】：測試前請務必「先將耳機音量調至低音量」再開始測試，切勿以過高音量直接施測，以防突發大音量損害聽力！【免責權與用途聲明】：本工具僅供個人日常聽力趨勢自我追蹤與科學研究，非臨床醫療器材亦無正式醫療診斷效力。若有突發性耳鳴或聽力異常，請立即諮詢耳鼻喉科專科醫師。',
    disclaimerTitle: '📋 研究與自我追蹤聲明',
    disclaimerDesc: '本系統僅供聽力科學研究與個人聽閾追蹤，非臨床醫療診斷器材。如自覺聽力衰退或單耳突發性耳鳴，請速至各大醫院耳鼻喉科就診。',

    // Test View
    blindAlertText: '⚠️ 提醒：目前處於靜音間隔，請勿盲目猜測！請等實際在耳機聽到蜂鳴音時再按鍵。',
    earLeft: '左耳 (Left Ear)',
    earRight: '右耳 (Right Ear)',
    earTestingLeft: '左耳測試中 (Left)',
    earTestingRight: '右耳測試中 (Right)',
    labelFreq: '頻率 (Frequency)',
    labelLevel: '刺激音量 (Level)',
    maskingPillPrefix: '🛡️ 遮蔽中: ',
    maskingPillOff: '🛡️ 遮蔽: 關閉',
    catchTrialPill: '🤫 對照靜音 (Catch Trial)',

    // Countdown
    countdownTitle: '準備開始測驗',
    countdownSub: '倒數結束後將自動播放測試音',
    countdownTip: '戴妥耳機，調整音量至 50%~70% 並保持環境安靜',
    btnSkipCountdown: '立即開始 (跳過倒數)',
    btnCancelCountdown: '取消',

    // Testing Active
    statusReady: '準備就緒',
    statusPlaying: '播放刺激音中 (請聆聽)...',
    statusWaiting: '等待受測者反應...',
    statusRecorded: '已記錄聽見反應！',
    statusNoResponse: '本次無反應',
    statusCompleted: '測驗全數完成！',
    btnHeardText: '我聽到了！',
    btnHeardSub: '聽到蜂鳴音請立即輕按',
    cleanModeShow: '切換至受測者極簡模式',
    cleanModeHide: '切換回監控面板',
    patientCleanTitle: '請保持安靜，專注聆聽耳機聲音',
    patientCleanSub: '只要聽到微弱聲音請立即點擊按鈕',
    btnEmergencyStop: '🛑 緊急停止 (Esc)',

    // Results View
    resultsTitle: '測驗結果總結',
    leftEarPta: '左耳平均聽閾 (PTA)',
    rightEarPta: '右耳平均聽閾 (PTA)',
    gradeNormal: '正常聽力 (Normal)',
    gradeMild: '輕度聽損 (Mild)',
    gradeModerate: '中度聽損 (Moderate)',
    gradeModeratelySevere: '中重度聽損 (Moderately Severe)',
    gradeSevere: '重度聽損 (Severe)',
    gradeProfound: '極重度聽損 (Profound)',
    gradeNone: '未測得有效聽閾',
    reliabilityLabel: '可靠度評估',
    reliabilityGood: '良好 (Good) - 數據可信度高',
    reliabilityQuestionable: '可疑 (Questionable) - 假陽性偏高',
    reliabilityPoor: '不佳 (Poor) - 建議重新施測',
    catchTrialStatsLabel: '假陽性率 (Catch Trial)',
    btnRestartTest: '🔄 重新測驗',
    btnExportJson: '📥 匯出 JSON',
    btnExportCsv: '📊 匯出 CSV',

    // Calibration View
    calibTitle: '耳機聲學校正設定',
    calibDesc: '依據耳機型號調整各頻率 offsets (dB)',
    btnApplyCalib: '⚡ 計算並儲存校正檔',
    btnResetCalib: '重設為預設刻度',

    // Emergency Modal
    modalEstopTitle: '測驗已緊急中止',
    modalEstopDesc: '已立即切斷所有聲音輸出與排程，保護您的聽力安全。',
    btnModalHome: '返回首頁',
    btnModalRestart: '重新開始測驗',

    // Bottom Nav
    navHome: '首頁',
    navTest: '測驗',
    navAudiogram: '聽力圖',
    navCalib: '校正',

    // Canvas
    canvasNormalBand: '正常聽力範圍 (0 ~ 25 dB HL)',
    canvasFreqAxis: '頻率 Frequency (Hz)',
    canvasDbAxis: '聽力級 (dB HL)',

    // Additional UI & Guidelines
    iphoneSilentWarning: '⚠️ <strong>iPhone / 手機無聲排查</strong>：請確認手機左側<strong>實體靜音開關已關閉</strong>（不可顯示橘色點），並確認手機「媒體音量」已開啟。',
    testProtocolTitle: '📋 受測重要指引',
    testProtocolDesc: '• <strong>刺激時間</strong>：每音實體播放 2 秒，反應確認視窗共 3 秒。<br>• <strong>對照靜音</strong>：測驗中隨機穿插「對照靜音試驗 (無聲)」，請確實聽見聲音再按鍵，避免盲猜。<br>• <strong>隨機間隔</strong>：每音之間隨機靜音 1 ~ 3 秒空白停留，徹底打破節奏盲猜。<br>• <strong>聽到即按</strong>：只要隱約聽見聲音，請立即按下綠色【我聽到了】大按鈕（發聲中按下會瞬間中斷）。<br>• <strong>嚴禁盲猜</strong>：靜音空白或對照靜音時盲按會觸發震動警告並記錄假陽性。<br>• <strong>系統音量</strong>：測驗前請將手機音量固定在 50%~70%，測驗中切勿更動。',
    footerLicenseNotice: '⚖️ <strong>CC BY-NC 4.0</strong> | 作者：<a href="https://github.com/rain3d" target="_blank" style="color: #38bdf8; text-decoration: none;">rain3d</a><br>個人與學術研究免費自由使用，嚴禁任何未授權商業營利行為。',
    patientCleanBadge: '受測者純淨盲測模式',
    btnEmergencyStopShort: '🛑 緊急停止',
    btnScrollToMonitor: '▲ 往上滑看監控',
    patientCleanNote: '手機可交由受測者單手操作 • 施測者可隨時往上滑動查閱即時數據',
    completedNoticeTakeOff: '<strong>請摘下耳機</strong>，並於座位上稍候休息。',
    completedNoticeReport: '系統已為您產出純音均值 (PTA) 與聽力圖數據供解說與參考。',
    legendRightEar: '● 右耳 (O)',
    legendLeftEar: '✕ 左耳 (X)',
    calibSelectEarLabel: '選擇發聲耳機單體：',
    calibEarLeft: '左耳單體 (Left Channel)',
    calibEarRight: '右耳單體 (Right Channel)',
    calibPlayRefTone: '🔊 播放 1000Hz 參考音',
    calibStopTone: '⏹️ 停止',
    calibSplLeftLabel: '左耳實測 (dB SPL)',
    calibSplRightLabel: '右耳實測 (dB SPL)'
  },

  'zh-CN': {
    // App & Header
    appTitle: '专业纯音听力测试 (Pure Tone Audiometry)',
    brandTitle: '纯音听力测试',
    brandSubtitle: 'ISO 8253-1 临床寻阈流程',
    calibUncalib: '未校准 (相对刻度)',
    calibValid: '已校准 (临床标准)',

    // Home View
    cardStartTitle: '开始标准双耳六频测试',
    cardStartDesc: '依序检测 250 ~ 8000 Hz 左右耳气导听阈，具备 2 秒实体发声 (3 秒反应视窗)、随机 1~3 秒静音空白防盲猜、对侧窄带掩蔽与即时动态听力图。',
    maskingToggleTitle: '对侧耳窄带掩蔽 (Masking)',
    maskingToggleDesc: '测试单耳时，对侧非测试耳同步播放窄带防干扰噪声',
    catchTrialToggleTitle: '对照静音防盲猜 (Catch Trial)',
    catchTrialToggleDesc: '随机插入无声对照试验，精准抓取未听见却盲猜按键之假阳性',
    btnStartTest: '🎧 戴上耳机，开始测试',

    // Sound Check
    soundCheckTitle: '🔊 测前声音与耳机试听',
    audioUnlockPrompt: '🟡 轻触屏幕解锁音频',
    audioReadyPrompt: '🟢 音频核心已就绪',
    soundCheckDesc: '测试前请先点击下方试听按钮，确认耳机左右声道发声正常，并调整手机媒体音量至舒适清晰（建议 50%~70%）。',
    btnTestBoth: '🔊 双耳纯音试听',
    btnTestNoiseOnly: '🛡️ 单独掩蔽噪声',
    btnTestLeftMask: '🎧 左耳纯音+右耳掩蔽',
    btnTestRightMask: '🎧 右耳纯音+左耳掩蔽',

    // Research Statement & Safety Warning
    safetyWarningTitle: '⚠️ 安全警语与免责声明 (听力保护重要须知)',
    safetyWarningDesc: '【音量保护原则】：测试前请务必「先将耳机音量调至低音量」再开始测试，切勿以过高音量直接施测，以防突发大音量损害听力！【免责权与用途声明】：本工具仅供个人日常听力趋势自我追踪与科学研究，非临床医疗器材亦无正式医疗诊断效力。若有突发性耳鸣或听力异常，请立即咨询耳鼻喉科专科医师。',
    disclaimerTitle: '📋 研究与自我追踪声明',
    disclaimerDesc: '本系统仅供听力科学研究与个人听阈追踪，非临床医疗诊断器材。如自觉听力衰退或单耳突发性耳鸣，请速至各大医院耳鼻喉科就诊。',

    // Test View
    blindAlertText: '⚠️ 提醒：目前处于静音间隔，请勿盲目猜测！请等实际在耳机听到蜂鸣音时再按键。',
    earLeft: '左耳 (Left Ear)',
    earRight: '右耳 (Right Ear)',
    earTestingLeft: '左耳测试中 (Left)',
    earTestingRight: '右耳测试中 (Right)',
    labelFreq: '频率 (Frequency)',
    labelLevel: '刺激音量 (Level)',
    maskingPillPrefix: '🛡️ 掩蔽中: ',
    maskingPillOff: '🛡️ 掩蔽: 关闭',
    catchTrialPill: '🤫 对照静音 (Catch Trial)',

    // Countdown
    countdownTitle: '准备开始测试',
    countdownSub: '倒数结束后将自动播放测试音',
    countdownTip: '戴好耳机，调整音量至 50%~70% 并保持环境安静',
    btnSkipCountdown: '立即开始 (跳过倒数)',
    btnCancelCountdown: '取消',

    // Testing Active
    statusReady: '准备就绪',
    statusPlaying: '播放刺激音中 (请聆听)...',
    statusWaiting: '等待受试者反应...',
    statusRecorded: '已记录听见反应！',
    statusNoResponse: '本次无反应',
    statusCompleted: '测试全数完成！',
    btnHeardText: '我听到了！',
    btnHeardSub: '听到蜂鸣音请立即轻按',
    cleanModeShow: '切换至受试者极简模式',
    cleanModeHide: '切换回监控面板',
    patientCleanTitle: '请保持安静，专注聆听耳机声音',
    patientCleanSub: '只要听到微弱声音请立即点击按钮',
    btnEmergencyStop: '🛑 紧急停止 (Esc)',

    // Results View
    resultsTitle: '测试结果总结',
    leftEarPta: '左耳平均听阈 (PTA)',
    rightEarPta: '右耳平均听阈 (PTA)',
    gradeNormal: '正常听力 (Normal)',
    gradeMild: '轻度听损 (Mild)',
    gradeModerate: '中度听损 (Moderate)',
    gradeModeratelySevere: '中重度听损 (Moderately Severe)',
    gradeSevere: '重度听损 (Severe)',
    gradeProfound: '极重度听损 (Profound)',
    gradeNone: '未测得有效听阈',
    reliabilityLabel: '可靠度评估',
    reliabilityGood: '良好 (Good) - 数据可信度高',
    reliabilityQuestionable: '可疑 (Questionable) - 假阳性偏高',
    reliabilityPoor: '不佳 (Poor) - 建议重新施测',
    catchTrialStatsLabel: '假阳性率 (Catch Trial)',
    btnRestartTest: '🔄 重新测试',
    btnExportJson: '📥 导出 JSON',
    btnExportCsv: '📊 导出 CSV',

    // Calibration View
    calibTitle: '耳机声学校准设定',
    calibDesc: '依据耳机型号调整各频率 offsets (dB)',
    btnApplyCalib: '⚡ 计算并存储校准档',
    btnResetCalib: '重置为默认刻度',

    // Emergency Modal
    modalEstopTitle: '测试已紧急中止',
    modalEstopDesc: '已立即切断所有声音输出与排程，保护您的听力安全。',
    btnModalHome: '返回首页',
    btnModalRestart: '重新开始测试',

    // Bottom Nav
    navHome: '首页',
    navTest: '测试',
    navAudiogram: '听力图',
    navCalib: '校准',

    // Canvas
    canvasNormalBand: '正常听力范围 (0 ~ 25 dB HL)',
    canvasFreqAxis: '频率 Frequency (Hz)',
    canvasDbAxis: '听力级 (dB HL)',

    // Additional UI & Guidelines
    iphoneSilentWarning: '⚠️ <strong>iPhone / 手机无声排查</strong>：请确认手机左侧<strong>实体静音开关已关闭</strong>（不可显示橙色点），并确认手机“媒体音量”已开启。',
    testProtocolTitle: '📋 受测重要指引',
    testProtocolDesc: '• <strong>刺激时间</strong>：每音实体播放 2 秒，反应确认窗口共 3 秒。<br>• <strong>对照静音</strong>：测试中随机穿插“对照静音试验 (无声)”，请切实听见声音再按键，避免盲猜。<br>• <strong>随机间隔</strong>：每音之间随机静音 1 ~ 3 秒空白停留，彻底打破节奏盲猜。<br>• <strong>听到即按</strong>：只要隐约听见声音，请立即按下绿色【我听到了】大按钮（发声中按下会瞬间中断）。<br>• <strong>严禁盲猜</strong>：静音空白或对照静音时盲按会触发震动警告并记录假阳性。<br>• <strong>系统音量</strong>：测试前请将手机音量固定在 50%~70%，测试中切勿变动。',
    footerLicenseNotice: '⚖️ <strong>CC BY-NC 4.0</strong> | 作者：<a href="https://github.com/rain3d" target="_blank" style="color: #38bdf8; text-decoration: none;">rain3d</a><br>个人与学术研究免费自由使用，严禁任何未授权商业营利行为。',
    patientCleanBadge: '受测者纯净盲测模式',
    btnEmergencyStopShort: '🛑 紧急停止',
    btnScrollToMonitor: '▲ 往上滑看监控',
    patientCleanNote: '手机可交由受测者单手操作 • 施测者可随时往上滑动查阅即时数据',
    completedNoticeTakeOff: '<strong>请摘下耳机</strong>，并于座位上稍候休息。',
    completedNoticeReport: '系统已为您产出纯音均值 (PTA) 与听力图数据供解说与参考。',
    legendRightEar: '● 右耳 (O)',
    legendLeftEar: '✕ 左耳 (X)',
    calibSelectEarLabel: '选择发声耳机单体：',
    calibEarLeft: '左耳单体 (Left Channel)',
    calibEarRight: '右耳单体 (Right Channel)',
    calibPlayRefTone: '🔊 播放 1000Hz 参考音',
    calibStopTone: '⏹️ 停止',
    calibSplLeftLabel: '左耳实测 (dB SPL)',
    calibSplRightLabel: '右耳实测 (dB SPL)'
  },

  'en-US': {
    // App & Header
    appTitle: 'Pure Tone Audiometry (PTA)',
    brandTitle: 'Pure Tone Audiometry',
    brandSubtitle: 'ISO 8253-1 Clinical Threshold Protocol',
    calibUncalib: 'Uncalibrated (Rel)',
    calibValid: 'Calibrated (Clinical)',

    // Home View
    cardStartTitle: 'Start Standard 6-Frequency Test',
    cardStartDesc: 'Tests 250 ~ 8000 Hz air-conduction thresholds sequentially, with 2s tones, 1~3s random silent pauses to prevent guessing, contralateral narrowband masking, and dynamic live audiogram.',
    maskingToggleTitle: 'Contralateral Masking',
    maskingToggleDesc: 'Plays narrowband masking noise in the non-test ear to prevent cross-hearing',
    catchTrialToggleTitle: 'Silent Catch Trial',
    catchTrialToggleDesc: 'Randomly inserts silent trials to accurately detect false-positive responses',
    btnStartTest: '🎧 Put On Headphones, Start Test',

    // Sound Check
    soundCheckTitle: '🔊 Pre-Test Audio & Earphone Check',
    audioUnlockPrompt: '🟡 Tap Screen to Unlock Audio',
    audioReadyPrompt: '🟢 Audio Engine Ready',
    soundCheckDesc: 'Before testing, please test the sound below to ensure both channels work properly, and set media volume to 50%~70%.',
    btnTestBoth: '🔊 Both Ears Tone Test',
    btnTestNoiseOnly: '🛡️ Masking Noise Only',
    btnTestLeftMask: '🎧 Left Tone + Right Mask',
    btnTestRightMask: '🎧 Right Tone + Left Mask',

    // Research Statement & Safety Warning
    safetyWarningTitle: '⚠️ Safety Warning & Legal Disclaimer (Hearing Protection Notice)',
    safetyWarningDesc: '[Volume Protection Rule]: Always turn down your headphone volume to a low level before starting the test. Never begin tests at high volume to prevent acoustic trauma! [Disclaimer & Intended Use]: This tool is strictly for personal hearing trend monitoring and research purposes; it is not a certified medical device and carries no clinical diagnostic validity. Consult an ENT physician if you experience hearing issues.',
    disclaimerTitle: '📋 Research & Personal Tracking Notice',
    disclaimerDesc: 'This tool is designed for hearing science research and personal threshold monitoring only, not as a diagnostic medical device. If you experience hearing loss or tinnitus, consult an ENT specialist.',

    // Test View
    blindAlertText: '⚠️ Warning: Currently in silent pause! Do not guess. Only respond when you genuinely hear a tone in headphones.',
    earLeft: 'Left Ear',
    earRight: 'Right Ear',
    earTestingLeft: 'Testing Left Ear',
    earTestingRight: 'Testing Right Ear',
    labelFreq: 'Frequency',
    labelLevel: 'Stimulus Level',
    maskingPillPrefix: '🛡️ Masking: ',
    maskingPillOff: '🛡️ Masking: Off',
    catchTrialPill: '🤫 Catch Trial (Silent)',

    // Countdown
    countdownTitle: 'Preparing Test',
    countdownSub: 'Tones will play automatically after countdown',
    countdownTip: 'Put on headphones, set volume to 50%~70%, and stay in a quiet place',
    btnSkipCountdown: 'Start Immediately (Skip Countdown)',
    btnCancelCountdown: 'Cancel',

    // Testing Active
    statusReady: 'Ready',
    statusPlaying: 'Presenting tone (please listen)...',
    statusWaiting: 'Awaiting response...',
    statusRecorded: 'Response recorded!',
    statusNoResponse: 'No response recorded',
    statusCompleted: 'Test Completed!',
    btnHeardText: 'I Heard It!',
    btnHeardSub: 'Tap immediately upon hearing tone',
    cleanModeShow: 'Switch to Patient Simple Mode',
    cleanModeHide: 'Return to Monitor Panel',
    patientCleanTitle: 'Please remain quiet and listen carefully',
    patientCleanSub: 'Tap the button as soon as you perceive any sound',
    btnEmergencyStop: '🛑 Emergency Stop (Esc)',

    // Results View
    resultsTitle: 'Test Results Summary',
    leftEarPta: 'Left Ear PTA',
    rightEarPta: 'Right Ear PTA',
    gradeNormal: 'Normal Hearing (0 ~ 25 dB)',
    gradeMild: 'Mild Loss (26 ~ 40 dB)',
    gradeModerate: 'Moderate Loss (41 ~ 55 dB)',
    gradeModeratelySevere: 'Moderately Severe Loss (56 ~ 70 dB)',
    gradeSevere: 'Severe Loss (71 ~ 90 dB)',
    gradeProfound: 'Profound Loss (> 90 dB)',
    gradeNone: 'No Valid Threshold',
    reliabilityLabel: 'Reliability Rating',
    reliabilityGood: 'Good - High Trustworthiness',
    reliabilityQuestionable: 'Questionable - Elevated False Positives',
    reliabilityPoor: 'Poor - Retest Recommended',
    catchTrialStatsLabel: 'False Positive Rate',
    btnRestartTest: '🔄 Restart Test',
    btnExportJson: '📥 Export JSON',
    btnExportCsv: '📊 Export CSV',

    // Calibration View
    calibTitle: 'Acoustic Calibration',
    calibDesc: 'Adjust frequency offsets (dB) for your headphones',
    btnApplyCalib: '⚡ Save Calibration Profile',
    btnResetCalib: 'Reset to Default Offsets',

    // Emergency Modal
    modalEstopTitle: 'Test Emergency Stopped',
    modalEstopDesc: 'All sound output and testing schedules have been terminated immediately.',
    btnModalHome: 'Back to Home',
    btnModalRestart: 'Restart Test',

    // Bottom Nav
    navHome: 'Home',
    navTest: 'Test',
    navAudiogram: 'Audiogram',
    navCalib: 'Calibrate',

    // Canvas
    canvasNormalBand: 'Normal Range (0 ~ 25 dB HL)',
    canvasFreqAxis: 'Frequency (Hz)',
    canvasDbAxis: 'Hearing Level (dB HL)',

    // Additional UI & Guidelines
    iphoneSilentWarning: '⚠️ <strong>iPhone / Audio Troubleshooting</strong>: Ensure the physical silent switch on the side is <strong>turned OFF</strong> (no orange dot showing), and your device media volume is turned on.',
    testProtocolTitle: '📋 Important Testing Guidelines',
    testProtocolDesc: '• <strong>Tone Duration</strong>: Each tone plays for 2s, with a 3s total response window.<br>• <strong>Silent Catch Trials</strong>: Silent trials are randomly inserted—only press when you actually hear a tone, avoid guessing.<br>• <strong>Random Intervals</strong>: Random 1~3s pauses between tones eliminate rhythmic pattern guessing.<br>• <strong>Press When Heard</strong>: Press the large green [I Hear It] button as soon as you detect a tone.<br>• <strong>Do Not Guess</strong>: Guessing during pauses or silent trials triggers vibration warnings and logs false positives.<br>• <strong>Volume Setting</strong>: Keep system volume fixed at 50%~70% throughout the test; do not adjust during testing.',
    footerLicenseNotice: '⚖️ <strong>CC BY-NC 4.0</strong> | Author: <a href="https://github.com/rain3d" target="_blank" style="color: #38bdf8; text-decoration: none;">rain3d</a><br>Free for personal and academic research use; unauthorized commercial use strictly prohibited.',
    patientCleanBadge: 'Patient Blind Testing Mode',
    btnEmergencyStopShort: '🛑 Emergency Stop',
    btnScrollToMonitor: '▲ Swipe Up for Monitor',
    patientCleanNote: 'Handheld operation by patient • Operator can swipe up anytime to view real-time data',
    completedNoticeTakeOff: '<strong>Please remove headphones</strong> and relax in your seat.',
    completedNoticeReport: 'Pure-Tone Average (PTA) and audiogram data have been generated for review.',
    legendRightEar: '● Right Ear (O)',
    legendLeftEar: '✕ Left Ear (X)',
    calibSelectEarLabel: 'Select Earphone Channel:',
    calibEarLeft: 'Left Channel',
    calibEarRight: 'Right Channel',
    calibPlayRefTone: '🔊 Play 1000Hz Reference Tone',
    calibStopTone: '⏹️ Stop',
    calibSplLeftLabel: 'Left Measured (dB SPL)',
    calibSplRightLabel: 'Right Measured (dB SPL)'
  }
};

let currentLang = 'zh-TW';

export function getCurrentLanguage() {
  return currentLang;
}

export function t(key, fallback = '') {
  const dict = translations[currentLang] || translations['zh-TW'];
  return dict[key] !== undefined ? dict[key] : (fallback || key);
}

export function setLanguage(lang) {
  if (!translations[lang]) {
    lang = 'zh-TW';
  }
  currentLang = lang;
  try {
    localStorage.setItem('hearing_test_lang', lang);
  } catch (e) {
    // localStorage may be unavailable in some sandboxes
  }

  document.documentElement.lang = lang;

  // Update elements with data-i18n
  document.querySelectorAll('[data-i18n]').forEach((el) => {
    const key = el.getAttribute('data-i18n');
    if (key && translations[lang][key] !== undefined) {
      el.textContent = translations[lang][key];
    }
  });

  // Update elements with data-i18n-html
  document.querySelectorAll('[data-i18n-html]').forEach((el) => {
    const key = el.getAttribute('data-i18n-html');
    if (key && translations[lang][key] !== undefined) {
      el.innerHTML = translations[lang][key];
    }
  });

  // Update elements with data-i18n-placeholder
  document.querySelectorAll('[data-i18n-placeholder]').forEach((el) => {
    const key = el.getAttribute('data-i18n-placeholder');
    if (key && translations[lang][key] !== undefined) {
      el.setAttribute('placeholder', translations[lang][key]);
    }
  });

  // Update select dropdown value if present
  const langSelect = document.getElementById('lang-select');
  if (langSelect && langSelect.value !== lang) {
    langSelect.value = lang;
  }

  // Dispatch custom event
  window.dispatchEvent(new CustomEvent('languageChanged', { detail: { lang } }));
}

export function initI18n() {
  let saved = null;
  try {
    saved = localStorage.getItem('hearing_test_lang');
  } catch (e) {}

  if (!saved || !translations[saved]) {
    // Check browser language
    const navLang = navigator.language || navigator.userLanguage || '';
    if (navLang.startsWith('en')) {
      saved = 'en-US';
    } else if (navLang === 'zh-CN' || navLang === 'zh-Hans') {
      saved = 'zh-CN';
    } else {
      saved = 'zh-TW';
    }
  }

  setLanguage(saved);
}
