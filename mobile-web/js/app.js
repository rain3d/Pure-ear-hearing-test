// ============================================================================
// 純音聽力測驗系統 - 手機 PWA 版 (Mobile Pure-Tone Audiometry)
// Author: rain3d (https://github.com/rain3d)
// License: Creative Commons Attribution-NonCommercial 4.0 International (CC BY-NC 4.0)
// 免費提供個人自主監測與非營利學術研究使用；嚴格禁止任何商業營利與付費銷售用途。
// ============================================================================

import { AudioEngine } from './audio-engine.js?v=12';
import { Calibrator } from './calibrator.js?v=12';
import { AudiometryStateMachine } from './state-machine.js?v=12';
import { AudiogramRenderer } from './audiogram.js?v=12';
import { t, initI18n, setLanguage, getCurrentLanguage } from './i18n.js?v=12';

class HearingTestApp {
  constructor() {
    this.calibrator = new Calibrator();
    this.audio = new AudioEngine(this.calibrator);
    this.fsm = new AudiometryStateMachine();
    this.audiogram = null;

    this.isSoundPlaying = false;
    this.isResponseWindowOpen = false;
    this.isCountdownActive = false;
    this.countdownTimer = null;
    this.countdownSeconds = 10;
    this.isMaskingEnabled = true; // Masking enabled by default
    this.isCatchTrialEnabled = true; // Silent Catch Trial enabled by default
    this.isCurrentStimulusCatch = false;
    this.stimulusTimeout = null;
    this.nextStimulusTimeout = null;
    this.progressInterval = null;
    this.stimulusStartTime = 0;
    this.wakeLock = null;

    initI18n();
    window.addEventListener('languageChanged', () => this.onLanguageChanged());

    this.initElements();
    this.bindEvents();
    this.updateCalibrationBadge();
    this.updateAudioStatus();
  }

  initElements() {
    // Views
    this.views = {
      home: document.getElementById('view-home'),
      test: document.getElementById('view-test'),
      result: document.getElementById('view-result'),
      calib: document.getElementById('view-calib')
    };

    // Navigation
    this.navItems = document.querySelectorAll('.nav-item');

    // Badges & Status
    this.calibBadge = document.getElementById('calib-badge');
    this.blindAlert = document.getElementById('blind-alert');
    this.statusText = document.getElementById('test-status-text');
    this.audioStatusText = document.getElementById('audio-status-text');

    // Quick Sound Check Buttons
    this.btnTestBoth = document.getElementById('btn-test-both');
    this.btnTestNoiseOnly = document.getElementById('btn-test-noise-only');
    this.btnTestLeftMask = document.getElementById('btn-test-left-mask');
    this.btnTestRightMask = document.getElementById('btn-test-right-mask');

    // Masking & Catch Trial Elements
    this.toggleMasking = document.getElementById('toggle-masking');
    this.toggleCatchTrial = document.getElementById('toggle-catch-trial');
    this.maskingPill = document.getElementById('test-masking-pill');
    this.maskingText = document.getElementById('test-masking-text');
    this.catchPill = document.getElementById('test-catch-pill');
    this.catchText = document.getElementById('test-catch-text');
    this.catchStatsText = document.getElementById('test-catch-stats');

    // Countdown Elements
    this.countdownCard = document.getElementById('countdown-card');
    this.countdownNum = document.getElementById('countdown-num');
    this.btnSkipCountdown = document.getElementById('btn-skip-countdown');
    this.testingCard = document.getElementById('testing-card');

    // Test Displays
    this.earBadge = document.getElementById('test-ear-badge');
    this.displayFreq = document.getElementById('display-freq');
    this.displayLevel = document.getElementById('display-level');
    this.radarContainer = document.getElementById('sound-radar');
    this.progressBar = document.getElementById('progress-bar');
    this.btnHeard = document.getElementById('btn-heard');

    // Audiogram Canvas
    const canvas = document.getElementById('audiogram-canvas');
    if (canvas) {
      this.audiogram = new AudiogramRenderer(canvas);
    }

    // Clean Patient Mode Elements
    this.patientCleanSection = document.getElementById('patient-clean-section');
    this.btnHeardClean = document.getElementById('btn-heard-clean');
    this.blindAlertClean = document.getElementById('blind-alert-clean');
    this.btnScrollToClean = document.getElementById('btn-scroll-to-clean');
    this.btnScrollToMonitor = document.getElementById('btn-scroll-to-monitor');
    this.patientCountdownBanner = document.getElementById('patient-countdown-banner');
    this.patientCountdownText = document.getElementById('patient-countdown-text');

    // Emergency Stop Elements
    this.btnCancelCountdown = document.getElementById('btn-cancel-countdown');
    this.btnEmergencyStop = document.getElementById('btn-emergency-stop');
    this.btnEmergencyStopCleanHeader = document.getElementById('btn-emergency-stop-clean-header');
    this.btnEmergencyStopCleanBottom = document.getElementById('btn-emergency-stop-clean-bottom');
    this.modalEmergencyStop = document.getElementById('modal-emergency-stop');
    this.btnModalHome = document.getElementById('btn-modal-home');
    this.btnModalRestart = document.getElementById('btn-modal-restart');

    // Language Selector
    this.langSelect = document.getElementById('lang-select');
    if (this.langSelect) {
      this.langSelect.value = getCurrentLanguage();
    }
  }

  bindEvents() {
    if (this.langSelect) {
      this.langSelect.addEventListener('change', (e) => {
        setLanguage(e.target.value);
      });
    }

    window.addEventListener('languageChanged', () => {
      this.updateAudioStatus();
    });

    // Global First-Touch / First-Click Audio Unlocker
    const triggerGlobalUnlock = () => {
      this.audio.unlock().then(() => {
        this.updateAudioStatus();
      });
    };
    window.addEventListener('touchstart', triggerGlobalUnlock, { once: true, passive: true });
    window.addEventListener('touchend', triggerGlobalUnlock, { once: true, passive: true });
    window.addEventListener('click', triggerGlobalUnlock, { once: true });

    // Quick Sound Check Listeners
    if (this.btnTestBoth) {
      this.btnTestBoth.addEventListener('click', (e) => {
        e.preventDefault();
        this.playQuickTestTone('Both');
      });
    }
    if (this.btnTestNoiseOnly) {
      this.btnTestNoiseOnly.addEventListener('click', (e) => {
        e.preventDefault();
        this.playQuickNoiseOnly();
      });
    }
    if (this.btnTestLeftMask) {
      this.btnTestLeftMask.addEventListener('click', (e) => {
        e.preventDefault();
        this.playQuickMaskingPair('Left');
      });
    }
    if (this.btnTestRightMask) {
      this.btnTestRightMask.addEventListener('click', (e) => {
        e.preventDefault();
        this.playQuickMaskingPair('Right');
      });
    }

    // Bottom Nav
    this.navItems.forEach((btn) => {
      btn.addEventListener('click', (e) => {
        e.preventDefault();
        const targetView = btn.dataset.view;
        this.switchView(targetView);
      });
    });

    // Start Test Buttons
    const btnStart = document.getElementById('btn-start-test');
    if (btnStart) {
      btnStart.addEventListener('click', () => this.startTest());
    }

    const btnRestart = document.getElementById('btn-restart-test');
    if (btnRestart) {
      btnRestart.addEventListener('click', () => this.startTest());
    }

    // Masking Toggle Listener
    if (this.toggleMasking) {
      this.toggleMasking.addEventListener('change', (e) => {
        this.isMaskingEnabled = e.target.checked;
      });
    }

    // Catch Trial Toggle Listener
    if (this.toggleCatchTrial) {
      this.toggleCatchTrial.addEventListener('change', (e) => {
        this.isCatchTrialEnabled = e.target.checked;
      });
    }

    // Skip Countdown Button
    if (this.btnSkipCountdown) {
      this.btnSkipCountdown.addEventListener('click', () => {
        this.skipCountdown();
      });
    }

    // Response Button (Tap / Click)
    if (this.btnHeard) {
      const handleResponse = (e) => {
        e.preventDefault();
        if (this.isCountdownActive) {
          this.skipCountdown();
          return;
        }
        this.onPatientResponse();
      };
      this.btnHeard.addEventListener('touchstart', handleResponse, { passive: false });
      this.btnHeard.addEventListener('click', handleResponse);
    }

    // Clean Patient Response Button (Tap / Click)
    if (this.btnHeardClean) {
      const handleCleanResponse = (e) => {
        e.preventDefault();
        if (this.isCountdownActive) {
          this.skipCountdown();
          return;
        }
        this.onPatientResponse();
      };
      this.btnHeardClean.addEventListener('touchstart', handleCleanResponse, { passive: false });
      this.btnHeardClean.addEventListener('click', handleCleanResponse);
    }

    // Scroll Guide Buttons
    if (this.btnScrollToClean) {
      this.btnScrollToClean.addEventListener('click', (e) => {
        e.preventDefault();
        this.scrollToCleanView();
      });
    }

    if (this.btnScrollToMonitor) {
      this.btnScrollToMonitor.addEventListener('click', (e) => {
        e.preventDefault();
        window.scrollTo({ top: 0, behavior: 'smooth' });
      });
    }

    // Emergency Stop Listeners
    const handleEStop = (e) => {
      e?.preventDefault();
      this.emergencyStop();
    };

    if (this.btnCancelCountdown) this.btnCancelCountdown.addEventListener('click', handleEStop);
    if (this.btnEmergencyStop) this.btnEmergencyStop.addEventListener('click', handleEStop);
    if (this.btnEmergencyStopCleanHeader) this.btnEmergencyStopCleanHeader.addEventListener('click', handleEStop);
    if (this.btnEmergencyStopCleanBottom) this.btnEmergencyStopCleanBottom.addEventListener('click', handleEStop);

    // Emergency Stop Modal Buttons
    if (this.btnModalHome) {
      this.btnModalHome.addEventListener('click', (e) => {
        e.preventDefault();
        if (this.modalEmergencyStop) this.modalEmergencyStop.style.display = 'none';
        this.switchView('home');
      });
    }

    if (this.btnModalRestart) {
      this.btnModalRestart.addEventListener('click', (e) => {
        e.preventDefault();
        if (this.modalEmergencyStop) this.modalEmergencyStop.style.display = 'none';
        this.startTest();
      });
    }

    // Keyboard Shortcuts (Space for Response, Escape for Emergency Stop)
    window.addEventListener('keydown', (e) => {
      if (e.code === 'Escape') {
        if (this.views.test.classList.contains('active')) {
          e.preventDefault();
          this.emergencyStop();
          return;
        }
      }

      if (e.code === 'Space' && !e.repeat) {
        e.preventDefault();
        if (this.isCountdownActive) {
          this.skipCountdown();
          return;
        }
        if (this.views.test.classList.contains('active')) {
          this.onPatientResponse();
        }
      }
    });

    // Calibration View Controls
    this.bindCalibrationControls();
  }

  playQuickTestTone(ear) {
    this.audio.unlock().then(() => {
      this.updateAudioStatus();
      // Play a clear, pleasant 1000Hz pure tone at -20 dBFS (~0.1 amplitude) for 1.5 seconds
      this.audio.playTone(ear, 1000, 38, 1500);
    });
  }

  playQuickNoiseOnly() {
    this.audio.unlock().then(() => {
      this.updateAudioStatus();
      // Play 1000Hz narrowband masking noise on Both ears at 40 dB HL for 1.8 seconds
      this.audio.playMaskingNoise('Both', 1000, 40, 1800);
    });
  }

  playQuickMaskingPair(testEar) {
    this.audio.unlock().then(() => {
      this.updateAudioStatus();
      const oppositeEar = testEar === 'Left' ? 'Right' : 'Left';
      // Test ear: 1000Hz pure tone (38 dB HL)
      // Contralateral ear: 1000Hz narrowband noise (42 dB HL - clearly audible & protective)
      this.audio.playToneWithMasking(testEar, 1000, 38, oppositeEar, 42, 2000);
    });
  }

  updateAudioStatus() {
    if (this.audioStatusText) {
      const isRunning = this.audio.ctx && this.audio.ctx.state === 'running';
      if (isRunning) {
        this.audioStatusText.textContent = t('audioReadyPrompt', '🟢 音訊核心已就緒');
        this.audioStatusText.style.color = '#10b981';
      } else {
        this.audioStatusText.textContent = t('audioUnlockPrompt', '🟡 輕觸螢幕解鎖音訊');
        this.audioStatusText.style.color = '#f59e0b';
      }
    }
  }

  switchView(viewName) {
    window.scrollTo({ top: 0, behavior: 'instant' });

    Object.keys(this.views).forEach((name) => {
      this.views[name]?.classList.toggle('active', name === viewName);
    });

    this.navItems.forEach((item) => {
      item.classList.toggle('active', item.dataset.view === viewName);
    });

    if (viewName === 'result' && this.audiogram) {
      setTimeout(() => this.audiogram.draw(this.fsm.thresholdResults), 50);
    }
  }

  scrollToCleanView() {
    if (this.patientCleanSection) {
      this.patientCleanSection.scrollIntoView({ behavior: 'smooth', block: 'start' });
    }
  }

  setBlindAlert(show, monitorMsg = '', cleanMsg = '') {
    if (this.blindAlert) {
      if (show) {
        this.blindAlert.textContent = monitorMsg;
        this.blindAlert.classList.add('show');
      } else {
        this.blindAlert.classList.remove('show');
      }
    }
    if (this.blindAlertClean) {
      if (show) {
        this.blindAlertClean.textContent = cleanMsg || monitorMsg;
        this.blindAlertClean.classList.add('show');
      } else {
        this.blindAlertClean.classList.remove('show');
      }
    }
  }

  hideBlindAlert() {
    this.setBlindAlert(false);
  }

  emergencyStop() {
    // 1. Immediately cut all audio output
    this.audio.stopTone();
    this.audio.stopKeepAlive();

    // 2. Clear all timers & progress intervals
    if (this.stimulusTimeout) {
      clearTimeout(this.stimulusTimeout);
      this.stimulusTimeout = null;
    }
    if (this.nextStimulusTimeout) {
      clearTimeout(this.nextStimulusTimeout);
      this.nextStimulusTimeout = null;
    }
    if (this.countdownTimer) {
      clearInterval(this.countdownTimer);
      this.countdownTimer = null;
    }
    this.stopProgressBar();

    this.isSoundPlaying = false;
    this.isResponseWindowOpen = false;
    this.isCountdownActive = false;

    if (this.radarContainer) {
      this.radarContainer.classList.remove('is-playing');
    }

    // 3. Reset State Machine
    this.fsm.reset();

    // 4. Release Wake Lock
    if (this.wakeLock) {
      try {
        this.wakeLock.release();
        this.wakeLock = null;
      } catch (_) {}
    }

    // 5. Tactile feedback
    if (navigator.vibrate) {
      navigator.vibrate([150, 80, 150]);
    }

    // 6. Reset test card views and show Emergency Stop Modal
    window.scrollTo({ top: 0, behavior: 'instant' });
    if (this.countdownCard) this.countdownCard.style.display = 'none';
    if (this.testingCard) this.testingCard.style.display = 'none';
    if (this.patientCleanSection) this.patientCleanSection.style.display = 'none';
    if (this.patientCountdownBanner) this.patientCountdownBanner.style.display = 'none';

    if (this.modalEmergencyStop) {
      this.modalEmergencyStop.style.display = 'flex';
    } else {
      this.switchView('home');
    }
  }

  updateCalibrationBadge() {
    if (this.calibrator.isCalibrated) {
      this.calibBadge.textContent = t('calibValid', '✓ 已校正 (手機基準)');
      this.calibBadge.className = 'badge-calib';
    } else {
      this.calibBadge.textContent = t('calibUncalib', '未校正 (相對刻度)');
      this.calibBadge.className = 'badge-calib badge-uncalib';
    }
  }

  onLanguageChanged() {
    this.updateCalibrationBadge();
    this.updateAudioStatus();
    if (this.audiogram) {
      this.audiogram.draw(this.fsm.getResults());
    }
    if (this.views.result && this.views.result.classList.contains('active')) {
      this.renderResults();
    }
    if (this.views.test && this.views.test.classList.contains('active')) {
      this.updateTestDisplay();
    }
  }

  async requestWakeLock() {
    try {
      if ('wakeLock' in navigator) {
        this.wakeLock = await navigator.wakeLock.request('screen');
      }
    } catch (_) {}
  }

  // --- Hearing Test Sequence ---

  startTest() {
    if (this.stimulusTimeout) clearTimeout(this.stimulusTimeout);
    if (this.nextStimulusTimeout) clearTimeout(this.nextStimulusTimeout);
    if (this.countdownTimer) clearInterval(this.countdownTimer);

    this.isResponseWindowOpen = false;
    this.isSoundPlaying = false;

    // Explicitly synchronize toggle states with UI DOM
    if (this.toggleCatchTrial) {
      this.isCatchTrialEnabled = this.toggleCatchTrial.checked;
    }
    if (this.toggleMasking) {
      this.isMaskingEnabled = this.toggleMasking.checked;
    }

    this.requestWakeLock();
    this.audio.unlock();
    this.audio.startKeepAlive();
    this.updateAudioStatus();

    this.fsm.start();
    this.switchView('test');
    this.hideBlindAlert();
    if (this.modalEmergencyStop) this.modalEmergencyStop.style.display = 'none';

    // Start 10-second Countdown UI
    this.startCountdown(10);
  }

  startCountdown(seconds = 10) {
    this.isCountdownActive = true;
    this.countdownSeconds = seconds;

    if (this.countdownCard) this.countdownCard.style.display = 'block';
    if (this.testingCard) this.testingCard.style.display = 'none';
    if (this.patientCleanSection) this.patientCleanSection.style.display = 'none';
    if (this.patientCountdownBanner) this.patientCountdownBanner.style.display = 'none';
    if (this.countdownNum) this.countdownNum.textContent = this.countdownSeconds.toString();

    if (this.countdownTimer) clearInterval(this.countdownTimer);

    this.countdownTimer = setInterval(() => {
      this.countdownSeconds--;

      // When reaching 5 seconds remaining, automatically switch to the clean patient responder view!
      if (this.countdownSeconds === 5) {
        this.transitionToCleanViewForCountdown();
      }

      if (this.countdownSeconds <= 0) {
        clearInterval(this.countdownTimer);
        this.countdownTimer = null;
        this.beginTestingActual();
      } else {
        if (this.countdownNum) this.countdownNum.textContent = this.countdownSeconds.toString();
        if (this.patientCountdownText && this.countdownSeconds <= 5) {
          this.patientCountdownText.textContent = `即將開始... 倒數 ${this.countdownSeconds} 秒 (請手指放好預備)`;
        }
      }
    }, 1000);
  }

  transitionToCleanViewForCountdown() {
    // Reveal test card and patient clean section
    if (this.countdownCard) this.countdownCard.style.display = 'none';
    if (this.testingCard) this.testingCard.style.display = 'block';
    if (this.patientCleanSection) this.patientCleanSection.style.display = 'flex';

    this.statusText.textContent = '⏳ 受測者預備中... 倒數 5 秒後自動播放第 1 題！';
    this.updateTestDisplay();

    // Show countdown banner on clean patient card
    if (this.patientCountdownBanner) {
      this.patientCountdownBanner.style.display = 'inline-flex';
    }
    if (this.patientCountdownText) {
      this.patientCountdownText.textContent = `即將開始... 倒數 5 秒 (請手指放好預備)`;
    }

    // Automatically smooth scroll down to clean patient section so user doesn't need to manually swipe
    setTimeout(() => {
      this.scrollToCleanView(true);
    }, 50);

    // Gentle haptic buzz to alert user page has switched
    if (navigator.vibrate) {
      navigator.vibrate(40);
    }
  }

  skipCountdown() {
    if (!this.isCountdownActive) return;
    this.audio.unlock();
    if (this.countdownTimer) {
      clearInterval(this.countdownTimer);
      this.countdownTimer = null;
    }
    this.countdownSeconds = 0;
    this.beginTestingActual();
  }

  beginTestingActual() {
    this.isCountdownActive = false;
    if (this.countdownCard) this.countdownCard.style.display = 'none';
    if (this.testingCard) this.testingCard.style.display = 'block';
    if (this.patientCleanSection) this.patientCleanSection.style.display = 'flex';

    // Update countdown banner on clean view to active test status
    if (this.patientCountdownBanner) {
      this.patientCountdownBanner.style.display = 'inline-flex';
    }
    if (this.patientCountdownText) {
      this.patientCountdownText.textContent = '🎧 測驗進行中 • 聽到聲音請立即點擊按鈕！';
    }

    // Ensure we are in clean view
    this.scrollToCleanView(true);

    this.statusText.textContent = '🎧 測驗已啟動！即將隨機延遲播放第 1 個測試音... 聽到請按按鈕！';
    this.updateTestDisplay();

    // 初始隨機緩衝 1.0 ~ 2.0 秒
    const initialDelayMs = Math.floor(Math.random() * 1000) + 1000;
    if (this.nextStimulusTimeout) clearTimeout(this.nextStimulusTimeout);
    this.nextStimulusTimeout = setTimeout(() => {
      if (this.fsm.state === 'testing') {
        this.presentNextStimulus();
      }
    }, initialDelayMs);
  }

  updateTestDisplay() {
    const ear = this.fsm.currentEar;
    this.earBadge.textContent = ear === 'Left' ? '左耳 (Left Channel)' : '右耳 (Right Channel)';
    this.earBadge.className = `test-ear-badge ${ear === 'Left' ? 'ear-left' : 'ear-right'}`;
    this.displayFreq.textContent = this.fsm.currentFrequency;
    this.displayLevel.textContent = this.fsm.currentLevel;

    // Update Contralateral Masking Pill
    if (this.maskingPill && this.maskingText) {
      if (this.isMaskingEnabled) {
        const oppositeEarName = ear === 'Left' ? '右耳' : '左耳';
        const maskLevel = this.calculateMaskingLevel(this.fsm.currentLevel);
        this.maskingPill.classList.remove('disabled');
        this.maskingText.textContent = `對側 (${oppositeEarName}) 窄頻遮蔽：${maskLevel} dB HL`;
      } else {
        this.maskingPill.classList.add('disabled');
        this.maskingText.textContent = '對側窄頻遮蔽：已停用 (單音模式)';
      }
    }

    // Update Catch Trial Pill (Live Anti-Blind-Guess Stats)
    if (this.catchPill && this.catchText) {
      if (this.isCatchTrialEnabled) {
        this.catchPill.classList.remove('disabled');
        const stats = this.fsm.getCatchTrialStats();
        if (stats.totalCatchTrials === 0) {
          this.catchPill.classList.remove('success');
          this.catchText.textContent = '對照靜音防盲猜：已啟用 (隨機抽測中)';
        } else {
          this.catchPill.classList.add('success');
          const passed = stats.totalCatchTrials - stats.falsePositiveCount;
          this.catchText.textContent = `對照靜音：已抽測 ${stats.totalCatchTrials} 次 (通過 ${passed} 次 / 盲猜 ${stats.falsePositiveCount} 次)`;
        }
      } else {
        this.catchPill.classList.add('disabled');
        this.catchPill.classList.remove('success');
        this.catchText.textContent = '對照靜音防盲猜：已停用';
      }
    }
  }

  /**
   * Calculate clinical masking level based on test tone level
   * ISO 8253-1 Contralateral Masking: Effective masking level must exceed tone level (tone + 5dB, min 35 dB HL)
   */
  calculateMaskingLevel(toneLevel) {
    return Math.min(50, Math.max(35, toneLevel + 5));
  }

  presentNextStimulus() {
    if (this.fsm.state !== 'testing') return;

    // Prepare stimulus and decide if this trial is a silent Catch Trial
    const stim = this.fsm.prepareNextStimulus(this.isCatchTrialEnabled);
    this.isCurrentStimulusCatch = stim.isCatchTrial;

    this.updateTestDisplay();
    this.isResponseWindowOpen = true;
    this.isSoundPlaying = true;
    this.hideBlindAlert();
    this.radarContainer.classList.add('is-playing');
    this.statusText.textContent = '🔊 聲音播放中 (2 秒)... 聽到請立即按下按鈕！';

    const ear = stim.ear;
    const freq = stim.freq;
    const level = stim.level;
    const oppositeEar = ear === 'Left' ? 'Right' : 'Left';
    const maskLevel = this.calculateMaskingLevel(stim.displayLevel);
    const soundDurationMs = 2000;   // 實際發聲 2 秒，避免節奏盲猜
    const responseWindowMs = 3000;  // 總反應視窗 3 秒 (聲音結束後仍有 1 秒緩衝期可按鍵)

    this.stimulusStartTime = performance.now();
    this.startProgressBar(responseWindowMs);

    const onToneEnded = () => {
      this.isSoundPlaying = false;
      this.radarContainer.classList.remove('is-playing');
      if (this.isResponseWindowOpen) {
        this.statusText.textContent = '⏱️ 發聲結束，反應視窗剩餘 1 秒... 聽到請按下！';
      }
    };

    if (this.isCurrentStimulusCatch) {
      // 对照静音試驗 (Catch Trial)：純音完全無聲！若有遮蔽則對側播放雜音
      this.audio.playCatchTrial(ear, freq, oppositeEar, maskLevel, this.isMaskingEnabled, soundDurationMs, onToneEnded);
    } else if (this.isMaskingEnabled) {
      this.audio.playToneWithMasking(ear, freq, level, oppositeEar, maskLevel, soundDurationMs, onToneEnded);
    } else {
      this.audio.playTone(ear, freq, level, soundDurationMs, onToneEnded);
    }

    // 3 秒反應視窗超時定時器
    if (this.stimulusTimeout) clearTimeout(this.stimulusTimeout);
    this.stimulusTimeout = setTimeout(() => {
      if (this.isResponseWindowOpen) {
        this.finishStimulus(false, null);
      }
    }, responseWindowMs);
  }

  startProgressBar(durationMs) {
    const start = performance.now();
    if (this.progressInterval) clearInterval(this.progressInterval);

    this.progressInterval = setInterval(() => {
      const elapsed = performance.now() - start;
      const pct = Math.min(100, (elapsed / durationMs) * 100);
      this.progressBar.style.width = `${pct}%`;
      if (pct >= 100) {
        clearInterval(this.progressInterval);
      }
    }, 30);
  }

  stopProgressBar() {
    if (this.progressInterval) {
      clearInterval(this.progressInterval);
      this.progressInterval = null;
    }
    this.progressBar.style.width = '0%';
  }

  onPatientResponse() {
    if (this.isResponseWindowOpen) {
      // 有效反應視窗內按下
      const latency = Math.round(performance.now() - this.stimulusStartTime);
      this.audio.stopTone(); // 若仍在發聲立即中斷

      if (this.isCurrentStimulusCatch) {
        // 在對照靜音期間猜測按鍵（被捕捉到假陽性！）
        this.setBlindAlert(
          true,
          '⚠️ 盲猜警示！剛才為【對照靜音 (Catch Trial)】，系統未播放測試音，請確實聽見再按鍵！（已記錄假陽性）',
          '⚠️ 提醒：剛才為無聲對照試驗，系統未發出聲音，請在真正聽見聲音時再點擊！'
        );
        if (navigator.vibrate) navigator.vibrate([100, 50, 100]);
      } else {
        // 輕微觸覺震動回饋
        if (navigator.vibrate) navigator.vibrate(30);
      }

      this.finishStimulus(true, latency);
    } else {
      // 盲目瞎猜警示：在隨機 1~3 秒空白停留期間按鍵
      this.fsm.recordSpontaneousFalsePositive();
      this.setBlindAlert(
        true,
        '⚠️ 盲目按鍵警示！目前為靜音空白期間，請勿盲猜，等聽到聲音再按！',
        '⚠️ 提醒：目前為靜音停留期間，請勿盲猜，等實際聽見聲音再點擊！'
      );

      // 警告長震動
      if (navigator.vibrate) navigator.vibrate([80, 40, 80]);
    }
  }

  finishStimulus(heard, latencyMs) {
    this.isResponseWindowOpen = false;
    this.isSoundPlaying = false;
    if (this.stimulusTimeout) {
      clearTimeout(this.stimulusTimeout);
      this.stimulusTimeout = null;
    }
    this.stopProgressBar();
    this.radarContainer.classList.remove('is-playing');

    const res = this.fsm.recordResponse(heard, latencyMs);
    this.updateTestDisplay();

    if (res.completed) {
      this.audio.stopKeepAlive();
      this.statusText.textContent = '🎉 測驗全數完成！已生成聽力圖報告。';
      setTimeout(() => this.showResults(), 800);
      return;
    }

    if (res.isCatchTrial) {
      if (res.isFalsePositive) {
        this.statusText.textContent = '⚠️ 對照靜音試驗：判定盲猜 (假陽性 +1)，該頻率保持原刻度繼續測驗';
      } else {
        this.statusText.textContent = '✓ 成功通過對照靜音試驗 (未盲猜，信度良好)';
      }
    } else if (res.thresholdFound) {
      this.statusText.textContent = `✓ 已確立此頻率聽閾！即將進入下一頻率...`;
    } else {
      this.statusText.textContent = heard
        ? `✓ 已聽到 (${latencyMs} ms) ➔ 降 10 dB`
        : `✕ 3 秒內未聽見 ➔ 升 5 dB`;
    }

    // 空白停留隨機 1.0 ~ 3.0 秒，有效降低盲猜與規律算拍子的可能性
    const jitterMs = Math.floor(Math.random() * 2000) + 1000; // 1000 ~ 3000 ms
    if (this.nextStimulusTimeout) clearTimeout(this.nextStimulusTimeout);
    this.nextStimulusTimeout = setTimeout(() => {
      if (this.fsm.state === 'testing') {
        this.presentNextStimulus();
      }
    }, jitterMs);
  }

  showResults() {
    if (this.patientCountdownBanner) this.patientCountdownBanner.style.display = 'none';
    this.switchView('result');
    if (this.audiogram) {
      this.audiogram.draw(this.fsm.thresholdResults);
    }

    // Update PTA Statistics
    const ptaLeft = this.fsm.getPureToneAverage('Left');
    const ptaRight = this.fsm.getPureToneAverage('Right');

    const elLeft = document.getElementById('pta-left');
    const elRight = document.getElementById('pta-right');
    const elGradeLeft = document.getElementById('grade-left');
    const elGradeRight = document.getElementById('grade-right');
    const elReliability = document.getElementById('test-reliability');

    if (elLeft) elLeft.textContent = ptaLeft !== null ? `${ptaLeft.toFixed(1)} dB HL` : '-- dB HL';
    if (elGradeLeft) elGradeLeft.textContent = this.getHearingLossClass(ptaLeft);

    if (elRight) elRight.textContent = ptaRight !== null ? `${ptaRight.toFixed(1)} dB HL` : '-- dB HL';
    if (elGradeRight) elGradeRight.textContent = this.getHearingLossClass(ptaRight);

    if (elReliability) {
      const rel = this.fsm.calculateReliability();
      if (rel.includes('良好') || rel.includes('Good') || rel.includes('極佳')) {
        elReliability.textContent = t('reliabilityGood', '良好 (Good) - 數據可信度高');
        elReliability.style.color = '#10b981';
      } else if (rel.includes('可疑') || rel.includes('Questionable') || rel.includes('尚可')) {
        elReliability.textContent = t('reliabilityQuestionable', '可疑 (Questionable) - 假陽性偏高');
        elReliability.style.color = '#f59e0b';
      } else {
        elReliability.textContent = t('reliabilityPoor', '不佳 (Poor) - 建議重新施測');
        elReliability.style.color = '#ef4444';
      }
    }

    if (this.catchStatsText) {
      const stats = this.fsm.getCatchTrialStats();
      if (stats.totalCatchTrials === 0) {
        this.catchStatsText.textContent = '未觸發 (對照靜音已停用)';
        this.catchStatsText.style.color = '#94a3b8';
      } else {
        this.catchStatsText.textContent = `總試驗 ${stats.totalCatchTrials} 次 | 盲猜假陽性 ${stats.falsePositiveCount} 次 (${stats.ratePercentage}%)`;
        if (stats.falsePositiveCount === 0) {
          this.catchStatsText.style.color = '#10b981';
        } else if (stats.ratePercentage <= 20) {
          this.catchStatsText.style.color = '#f59e0b';
        } else {
          this.catchStatsText.style.color = '#ef4444';
        }
      }
    }
  }

  getHearingLossClass(pta) {
    if (pta === null || pta === undefined || isNaN(pta)) return t('gradeNone', '未測得有效聽閾');
    if (pta <= 25.0) return t('gradeNormal', '正常聽力 (Normal)');
    if (pta <= 40.0) return t('gradeMild', '輕度聽損 (Mild)');
    if (pta <= 55.0) return t('gradeModerate', '中度聽損 (Moderate)');
    if (pta <= 70.0) return t('gradeModeratelySevere', '中重度聽損 (Moderately Severe)');
    if (pta <= 90.0) return t('gradeSevere', '重度聽損 (Severe)');
    return t('gradeProfound', '極重度聽損 (Profound)');
  }

  // --- Calibration Tab Controls ---

  bindCalibrationControls() {
    const btnPlayCalib = document.getElementById('btn-play-calib');
    const btnStopCalib = document.getElementById('btn-stop-calib');
    const btnApplyCalib = document.getElementById('btn-apply-calib');
    const inputSplL = document.getElementById('input-spl-left');
    const inputSplR = document.getElementById('input-spl-right');

    if (btnPlayCalib) {
      btnPlayCalib.addEventListener('click', () => {
        this.audio.unlock().then(() => {
          const earSelect = document.getElementById('calib-ear-select');
          const ear = earSelect ? earSelect.value : 'Left';
          // Play 1000 Hz reference tone at standard -20 dBFS (38 dB HL) for 15s
          this.audio.playTone(ear, 1000, 38, 15000);
        });
      });
    }

    if (btnStopCalib) {
      btnStopCalib.addEventListener('click', () => {
        this.audio.stopTone();
      });
    }

    if (btnApplyCalib) {
      btnApplyCalib.addEventListener('click', () => {
        const leftVal = parseFloat(inputSplL.value) || 70.0;
        const rightVal = parseFloat(inputSplR.value) || 70.0;
        this.calibrator.applyPhoneCalibration(leftVal, rightVal);
        this.updateCalibrationBadge();
        alert(`校正成功！\n已套用手機實測聲壓 (左: ${leftVal} dB, 右: ${rightVal} dB) 作為物理基準。`);
      });
    }

    const btnResetCalib = document.getElementById('btn-reset-calib');
    if (btnResetCalib) {
      btnResetCalib.addEventListener('click', () => {
        this.calibrator.reset();
        this.updateCalibrationBadge();
        alert('校正狀態已重設為【相對刻度模式】。');
      });
    }
  }
}

// Bootstrap
window.addEventListener('DOMContentLoaded', () => {
  window.app = new HearingTestApp();

  // Register PWA Service Worker
  if ('serviceWorker' in navigator) {
    navigator.serviceWorker.register('./sw.js').catch(() => {});
  }
});
