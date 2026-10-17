/**
 * Audiometry State Machine (ISO 8253-1 / ASHA Hughson-Westlake 10-down / 5-up)
 */
export class AudiometryStateMachine {
  constructor() {
    this.frequencies = [1000, 2000, 4000, 8000, 500, 250];
    this.reset();
  }

  reset() {
    this.currentEar = 'Left';
    this.frequencyIndex = 0;
    this.currentLevel = 30.0; // Start at 30 dB HL
    this.thresholdResults = []; // [{ ear, freq, threshold }]
    this.events = [];
    this.state = 'idle'; // idle, testing, completed, paused

    // Hughson-Westlake Tracking
    this.ascendingResponses = {}; // { level: count }
    this.lastResponse = null;
    this.retest1000Left = null;
    this.retest1000Done = false;

    // Reliability & Anti-Blind-Guess (Catch Trials & Jitter Gap Detection)
    this.isCurrentStimulusCatchTrial = false;
    this.catchTrialEnabled = true;
    this.catchTrialCount = 0; // Total presented silent catch trials
    this.catchTrialPresses = 0; // False presses during silent catch trials
    this.spontaneousFalsePositives = 0; // False presses during silent jitter gaps
    this.falsePositiveCount = 0; // Total false positives (catch trials + gaps)
    this.totalCatchTrials = 0; // Total opportunities to catch blind guessing
    this.trialsSinceLastCatch = 0;
    this.totalStimulusCount = 0;
    this.consecutiveNotHeard = 0;
  }

  get currentFrequency() {
    return this.frequencies[this.frequencyIndex] || 1000;
  }

  start() {
    this.reset();
    this.state = 'testing';
  }

  recordSpontaneousFalsePositive() {
    this.spontaneousFalsePositives++;
    this.falsePositiveCount++;
    this.totalCatchTrials++;
    this.events.unshift({
      time: new Date(),
      ear: this.currentEar,
      freq: this.currentFrequency,
      level: null,
      heard: true,
      latencyMs: null,
      isCatchTrial: false,
      isSpontaneous: true,
      isFalsePositive: true
    });
  }

  /**
   * Prepares the next stimulus configuration and randomly schedules catch trials
   * @param {boolean} enableCatchTrial - Whether Catch Trials are enabled
   * @returns {Object} { ear, freq, level, displayLevel, isCatchTrial }
   */
  prepareNextStimulus(enableCatchTrial = true) {
    this.totalStimulusCount++;
    this.catchTrialEnabled = enableCatchTrial;

    // ISO 8253-1 Catch Trial Determination:
    // 1. Must be enabled by setting
    // 2. Never two catch trials back-to-back
    // 3. Do not trigger on the very first stimulus of the test
    // 4. Require at least 2 regular stimuli between catch trials (trialsSinceLastCatch >= 2)
    // 5. Stepped probability with upper-bound guarantee to ensure balanced ~20-25% presentation:
    //    - trialsSinceLastCatch == 2: 25% chance
    //    - trialsSinceLastCatch == 3: 35% chance
    //    - trialsSinceLastCatch >= 4: 100% guaranteed catch trial (prevents long runs without silence)
    let shouldCatch = false;
    if (this.catchTrialEnabled && !this.isCurrentStimulusCatchTrial && this.totalStimulusCount > 1 && this.trialsSinceLastCatch >= 2) {
      if (this.trialsSinceLastCatch >= 4) {
        shouldCatch = true;
      } else {
        const catchProb = this.trialsSinceLastCatch === 3 ? 0.35 : 0.25;
        shouldCatch = Math.random() < catchProb;
      }
    }

    this.isCurrentStimulusCatchTrial = shouldCatch;

    if (shouldCatch) {
      this.catchTrialCount++;
      this.totalCatchTrials++;
      this.trialsSinceLastCatch = 0;
    } else {
      this.trialsSinceLastCatch++;
    }

    return {
      ear: this.currentEar,
      freq: this.currentFrequency,
      level: shouldCatch ? null : this.currentLevel,
      displayLevel: this.currentLevel,
      isCatchTrial: shouldCatch
    };
  }

  /**
   * Process user response to stimulus
   * @param {boolean} heard - Did the patient hear the sound?
   * @param {number|null} latencyMs - Milliseconds elapsed before response
   * @returns {Object} result metadata { thresholdFound, completed, ear, freq, level, isCatchTrial, isFalsePositive }
   */
  recordResponse(heard, latencyMs = null) {
    if (this.state === 'completed') {
      return {
        thresholdFound: false,
        completed: true,
        ear: this.currentEar,
        freq: this.currentFrequency,
        level: this.currentLevel,
        isCatchTrial: false,
        isFalsePositive: false
      };
    }

    const ear = this.currentEar;
    const freq = this.currentFrequency;
    const level = this.currentLevel;

    // Handle Silent Catch Trial response
    if (this.isCurrentStimulusCatchTrial) {
      let isFalsePositive = false;
      if (heard) {
        // Patient falsely claimed to hear a completely silent catch trial!
        this.catchTrialPresses++;
        this.falsePositiveCount++;
        isFalsePositive = true;
      }

      this.events.unshift({
        time: new Date(),
        ear,
        freq,
        level: null,
        heard,
        latencyMs,
        isCatchTrial: true,
        isFalsePositive
      });

      this.isCurrentStimulusCatchTrial = false;

      // Catch trials DO NOT alter Hughson-Westlake searching levels or thresholds
      return {
        thresholdFound: false,
        completed: this.state === 'completed',
        ear: this.currentEar,
        freq: this.currentFrequency,
        level: this.currentLevel,
        isCatchTrial: true,
        isFalsePositive
      };
    }

    this.events.unshift({
      time: new Date(),
      ear,
      freq,
      level,
      heard,
      latencyMs,
      isCatchTrial: false,
      isFalsePositive: false
    });

    let thresholdFound = false;

    if (heard) {
      this.consecutiveNotHeard = 0;
      // If previous was not heard and current is heard, this is an ascending response!
      if (this.lastResponse === false) {
        this.ascendingResponses[level] = (this.ascendingResponses[level] || 0) + 1;
        // 2-of-3 Criterion: Two responses at the same level on ascending runs!
        if (this.ascendingResponses[level] >= 2) {
          thresholdFound = true;
        }
      } else if (level <= 0) {
        // Floor limit (0 dB HL) handling:
        // When at minimum test limit (0 dB HL), descending lower is impossible.
        // If patient responds to 0 dB HL twice (2-of-3 confirmation), threshold is established at 0 dB HL.
        this.ascendingResponses[0] = (this.ascendingResponses[0] || 0) + 1;
        if (this.ascendingResponses[0] >= 2) {
          thresholdFound = true;
        }
      }

      if (!thresholdFound) {
        // Standard rule: Down 10 dB on heard
        this.currentLevel = Math.max(0, this.currentLevel - 10);
      }
    } else {
      this.consecutiveNotHeard++;
      // Standard rule: Up 5 dB on not heard
      this.currentLevel = Math.min(50, this.currentLevel + 5);

      // If patient doesn't hear even at max safe level (50 dB HL) repeatedly
      if (this.currentLevel >= 50 && this.consecutiveNotHeard >= 3) {
        thresholdFound = true; // Mark as NR (No Response) at 50 dB HL
      }
    }

    this.lastResponse = heard;

    if (thresholdFound) {
      // Record threshold
      this.thresholdResults.push({
        ear,
        freq,
        threshold: this.currentLevel,
        isNoResponse: this.currentLevel >= 50 && !heard
      });

      // Reset frequency trackers
      this.ascendingResponses = {};
      this.lastResponse = null;
      this.consecutiveNotHeard = 0;
      this.currentLevel = 30.0;

      // Advance to next frequency or next ear
      this.frequencyIndex++;
      if (this.frequencyIndex >= this.frequencies.length) {
        if (this.currentEar === 'Left') {
          // Switch to Right Ear
          this.currentEar = 'Right';
          this.frequencyIndex = 0;
        } else {
          // Both ears completed!
          this.state = 'completed';
        }
      }
    }

    return {
      thresholdFound,
      completed: this.state === 'completed',
      ear: this.currentEar,
      freq: this.currentFrequency,
      level: this.currentLevel,
      isCatchTrial: false,
      isFalsePositive: false
    };
  }

  calculateReliability() {
    if (this.totalCatchTrials === 0) return '良好 (Good)';
    const fpRate = this.falsePositiveCount / this.totalCatchTrials;
    if (fpRate === 0) return '極佳 (Excellent)';
    if (fpRate <= 0.15) return '良好 (Good)';
    if (fpRate <= 0.30) return '尚可 (Fair)';
    return '存疑 (Questionable - 盲按次數過多)';
  }

  getCatchTrialStats() {
    const total = this.totalCatchTrials;
    const fp = this.falsePositiveCount;
    const rate = total > 0 ? (fp / total) : 0;
    return {
      totalCatchTrials: total,
      catchTrialCount: this.catchTrialCount,
      catchTrialPresses: this.catchTrialPresses,
      spontaneousFalsePositives: this.spontaneousFalsePositives,
      falsePositiveCount: fp,
      falsePositiveRate: rate,
      ratePercentage: Math.round(rate * 100)
    };
  }

  getPureToneAverage(ear) {
    const freqs = [500, 1000, 2000, 4000];
    const matched = this.thresholdResults.filter(r => r.ear === ear && freqs.includes(r.freq));
    if (matched.length === 0) return null;
    const sum = matched.reduce((acc, curr) => acc + curr.threshold, 0);
    return Math.round((sum / matched.length) * 10) / 10;
  }
}
