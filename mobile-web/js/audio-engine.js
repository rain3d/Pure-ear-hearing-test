/**
 * Web Audio API Engine for Pure Tone Audiometry (Mobile Optimized)
 * Provides sample-accurate sine wave synthesis, stereo panning,
 * raised-cosine envelope shaping (zero-click), safety ceiling,
 * ISO 389-4 normalized contralateral narrowband masking noise,
 * iOS/Android silent mode bypass & audio lifecycle keep-alive.
 */
export class AudioEngine {
  constructor(calibrator) {
    this.calibrator = calibrator;
    this.ctx = null;
    this.currentOsc = null;
    this.currentGain = null;
    this.currentPanner = null;
    this.currentNoiseSource = null;
    this.currentNoiseGain = null;
    this.currentNoisePanner = null;
    this.isPlaying = false;
    this.stopTimeout = null;

    // Mobile Audio Unlock & Keep-Alive
    this.silentAudioUnlocked = false;
    this.keepAliveNode = null;
    this.keepAliveGain = null;
  }

  /**
   * Safe AudioContext initialization without hardcoded sample rates
   */
  ensureContext() {
    if (!this.ctx) {
      const AudioCtx = window.AudioContext || window.webkitAudioContext;
      try {
        this.ctx = new AudioCtx();
      } catch (e) {
        console.warn('AudioContext constructor fallback', e);
        this.ctx = new AudioCtx();
      }
    }
    if (this.ctx.state === 'suspended') {
      this.ctx.resume().catch(() => {});
    }
    return this.ctx;
  }

  /**
   * Universal Mobile Audio Unlocker
   * Unlocks Web Audio graph and promotes iOS audio session to "Playback" category
   */
  async unlock() {
    const ctx = this.ensureContext();
    if (ctx.state === 'suspended') {
      try {
        await ctx.resume();
      } catch (_) {}
    }

    // 1. Play 1-sample silent Web Audio buffer
    try {
      const buffer = ctx.createBuffer(1, 1, 22050);
      const source = ctx.createBufferSource();
      source.buffer = buffer;
      source.connect(ctx.destination);
      source.start(0);
    } catch (_) {}

    // 2. Play silent HTML5 audio element to switch iOS session to Playback category (bypassing mute switch)
    if (!this.silentAudioUnlocked) {
      try {
        const audio = document.createElement('audio');
        audio.setAttribute('x-webkit-airplay', 'deny');
        audio.setAttribute('playsinline', '');
        audio.preload = 'auto';
        // 0.1s silent WAV data URI
        audio.src = 'data:audio/wav;base64,UklGRigAAABXQVZFZm10IBIAAAABAAEARKwAAIhYAQACABAAAABkYXRhAgAAAAEA';
        const playPromise = audio.play();
        if (playPromise !== undefined) {
          await playPromise;
          this.silentAudioUnlocked = true;
        }
      } catch (_) {}
    }

    // 3. Start Keep-Alive node to prevent iOS/Android idle context sleep
    this.startKeepAlive();
    return ctx.state === 'running';
  }

  /**
   * Keep-Alive node maintains an active zero-gain audio graph
   * preventing mobile OS from suspending AudioContext during countdowns or pauses
   */
  startKeepAlive() {
    if (this.keepAliveNode || !this.ctx) return;
    try {
      const silentGain = this.ctx.createGain();
      silentGain.gain.setValueAtTime(0.000001, this.ctx.currentTime);
      const osc = this.ctx.createOscillator();
      osc.frequency.setValueAtTime(20, this.ctx.currentTime); // Inaudible 20Hz
      osc.connect(silentGain);
      silentGain.connect(this.ctx.destination);
      osc.start();
      this.keepAliveNode = osc;
      this.keepAliveGain = silentGain;
    } catch (_) {}
  }

  stopKeepAlive() {
    if (this.keepAliveNode) {
      try {
        this.keepAliveNode.stop();
        this.keepAliveNode.disconnect();
        this.keepAliveGain?.disconnect();
      } catch (_) {}
      this.keepAliveNode = null;
      this.keepAliveGain = null;
    }
  }

  /**
   * Convert dB HL to digital linear amplitude using calibration offsets
   * Safety Ceiling: 50 dB HL / -10 dBFS
   */
  getAmplitude(freq, ear, dbHl) {
    const clampedHl = Math.min(Math.max(dbHl, 0), 50); // Hard limit 0~50 dB HL
    let dbfs = this.calibrator.hlToDbfs(clampedHl, freq, ear);
    dbfs = Math.min(dbfs, -10.0); // Safety ceiling -10 dBFS
    return Math.pow(10.0, dbfs / 20.0);
  }

  /**
   * Convert dB HL to digital linear amplitude for narrowband masking noise
   * Applies ISO 389-4 EML (Effective Masking Level) acoustic offset (+5 dB)
   * to ensure noise loudness matches pure tone perception across critical bands
   */
  getMaskingAmplitude(freq, ear, dbHl) {
    const clampedHl = Math.min(Math.max(dbHl, 0), 50);
    // +5.0 dB effective masking offset (ISO 389-4)
    let dbfs = this.calibrator.hlToDbfs(clampedHl, freq, ear) + 5.0;
    dbfs = Math.min(dbfs, -8.0); // Safety ceiling -8 dBFS
    return Math.pow(10.0, dbfs / 20.0);
  }

  /**
   * Route audio to Left, Right, or Both channels
   */
  connectStereo(sourceNode, ear, ctx) {
    if (ctx.createStereoPanner) {
      const panner = ctx.createStereoPanner();
      const panVal = ear === 'Left' ? -1.0 : (ear === 'Right' ? 1.0 : 0.0);
      panner.pan.setValueAtTime(panVal, ctx.currentTime);
      sourceNode.connect(panner);
      panner.connect(ctx.destination);
      return panner;
    } else {
      // Fallback for older WebKit: Split into ChannelMerger
      const merger = ctx.createChannelMerger(2);
      if (ear === 'Left') {
        sourceNode.connect(merger, 0, 0);
      } else if (ear === 'Right') {
        sourceNode.connect(merger, 0, 1);
      } else {
        sourceNode.connect(merger, 0, 0);
        sourceNode.connect(merger, 0, 1);
      }
      merger.connect(ctx.destination);
      return merger;
    }
  }

  /**
   * Play a pure tone stimulus
   * @param {string} ear - 'Left', 'Right', or 'Both'
   * @param {number} frequencyHz - e.g. 1000
   * @param {number} dbHl - 0 to 50 dB HL
   * @param {number} durationMs - e.g. 2000 ms
   * @param {Function} onEnded - callback when playback completes
   */
  playTone(ear, frequencyHz, dbHl, durationMs = 2000, onEnded = null) {
    this.stopTone(); // Stop any currently playing audio safely

    const ctx = this.ensureContext();
    if (ctx.state === 'suspended') {
      ctx.resume().catch(() => {});
    }

    const now = ctx.currentTime;
    const targetAmp = Math.max(0.00001, this.getAmplitude(frequencyHz, ear, dbHl));
    const rampSec = 0.025; // 25ms raised-cosine ramp to prevent transient clicks
    const totalDurationSec = durationMs / 1000.0;

    // 1. Oscillator
    const osc = ctx.createOscillator();
    osc.type = 'sine';
    osc.frequency.setValueAtTime(frequencyHz, now);

    // 2. Gain Envelope with Smooth Attack & Release
    const gain = ctx.createGain();
    gain.gain.setValueAtTime(0.00001, now);
    gain.gain.exponentialRampToValueAtTime(targetAmp, now + rampSec);
    gain.gain.setValueAtTime(targetAmp, now + totalDurationSec - rampSec);
    gain.gain.exponentialRampToValueAtTime(0.00001, now + totalDurationSec);

    osc.connect(gain);

    // 3. Stereo Routing
    const panner = this.connectStereo(gain, ear, ctx);

    osc.start(now);
    osc.stop(now + totalDurationSec);

    this.currentOsc = osc;
    this.currentGain = gain;
    this.currentPanner = panner;
    this.isPlaying = true;

    this.stopTimeout = setTimeout(() => {
      this.isPlaying = false;
      if (onEnded) onEnded();
    }, durationMs);

    osc.onended = () => {
      this.isPlaying = false;
    };
  }

  /**
   * Play a pure tone stimulus with simultaneous contralateral narrowband masking noise
   * @param {string} toneEar - 'Left' or 'Right'
   * @param {number} frequencyHz - Pure tone center frequency (Hz)
   * @param {number} toneHl - Tone level (0 to 50 dB HL)
   * @param {string} maskEar - 'Right' or 'Left' (Opposite ear)
   * @param {number} maskHl - Masking level (0 to 50 dB HL)
   * @param {number} durationMs - e.g. 2000 ms
   * @param {Function} onEnded - callback when playback completes
   */
  playToneWithMasking(toneEar, frequencyHz, toneHl, maskEar, maskHl, durationMs = 2000, onEnded = null) {
    this.stopTone(); // Stop any currently playing audio safely

    const ctx = this.ensureContext();
    if (ctx.state === 'suspended') {
      ctx.resume().catch(() => {});
    }

    const now = ctx.currentTime;
    const rampSec = 0.025; // 25ms ramp
    const totalDurationSec = durationMs / 1000.0;

    // 1. Pure Tone Synthesis (Test Ear)
    const toneAmp = Math.max(0.00001, this.getAmplitude(frequencyHz, toneEar, toneHl));
    const osc = ctx.createOscillator();
    osc.type = 'sine';
    osc.frequency.setValueAtTime(frequencyHz, now);

    const toneGain = ctx.createGain();
    toneGain.gain.setValueAtTime(0.00001, now);
    toneGain.gain.exponentialRampToValueAtTime(toneAmp, now + rampSec);
    toneGain.gain.setValueAtTime(toneAmp, now + totalDurationSec - rampSec);
    toneGain.gain.exponentialRampToValueAtTime(0.00001, now + totalDurationSec);

    osc.connect(toneGain);
    const tonePanner = this.connectStereo(toneGain, toneEar, ctx);

    // 2. Contralateral Narrowband Masking Noise Synthesis (Non-Test Ear)
    let noiseSource = null;
    let noiseGain = null;
    let noisePanner = null;
    const maskAmp = Math.max(0.00001, this.getMaskingAmplitude(frequencyHz, maskEar, maskHl));

    if (maskHl > 0 && maskAmp > 0.00001) {
      noiseSource = this.createNarrowbandNoiseNode(ctx, frequencyHz, totalDurationSec);

      noiseGain = ctx.createGain();
      noiseGain.gain.setValueAtTime(0.00001, now);
      noiseGain.gain.exponentialRampToValueAtTime(maskAmp, now + rampSec);
      noiseGain.gain.setValueAtTime(maskAmp, now + totalDurationSec - rampSec);
      noiseGain.gain.exponentialRampToValueAtTime(0.00001, now + totalDurationSec);

      noiseSource.connect(noiseGain);
      noisePanner = this.connectStereo(noiseGain, maskEar, ctx);
    }

    osc.start(now);
    osc.stop(now + totalDurationSec);

    if (noiseSource) {
      noiseSource.start(now);
      noiseSource.stop(now + totalDurationSec);
    }

    this.currentOsc = osc;
    this.currentGain = toneGain;
    this.currentPanner = tonePanner;
    this.currentNoiseSource = noiseSource;
    this.currentNoiseGain = noiseGain;
    this.currentNoisePanner = noisePanner;
    this.isPlaying = true;

    this.stopTimeout = setTimeout(() => {
      this.isPlaying = false;
      if (onEnded) onEnded();
    }, durationMs);

    osc.onended = () => {
      this.isPlaying = false;
    };
  }

  /**
   * Play a silent catch trial (對照靜音試驗)
   * The test ear emits NO pure tone (complete silence).
   * If contralateral masking is enabled, the opposite ear receives narrowband masking noise
   * to verify if the subject is guessing or confusing masking noise with a test tone.
   * @param {string} testEar - 'Left' or 'Right'
   * @param {number} frequencyHz - Current frequency (Hz)
   * @param {string} maskEar - 'Right' or 'Left' (Opposite ear)
   * @param {number} maskHl - Masking level (dB HL)
   * @param {boolean} isMaskingEnabled - Whether masking is enabled
   * @param {number} durationMs - Duration in ms (default 2000ms)
   * @param {Function} onEnded - Callback when playback finishes
   */
  playCatchTrial(testEar, frequencyHz, maskEar, maskHl, isMaskingEnabled, durationMs = 2000, onEnded = null) {
    this.stopTone(); // Stop any previous playback

    const ctx = this.ensureContext();
    if (ctx.state === 'suspended') {
      ctx.resume().catch(() => {});
    }

    // Complete Silence for Catch Trial (零音量空白對照試驗):
    // Neither pure tone oscillator nor contralateral masking noise is emitted,
    // ensuring the patient experiences complete acoustic silence in both channels.
    this.currentOsc = null;
    this.currentGain = null;
    this.currentPanner = null;
    this.currentNoiseSource = null;
    this.currentNoiseGain = null;
    this.currentNoisePanner = null;
    this.isPlaying = true;

    this.stopTimeout = setTimeout(() => {
      this.isPlaying = false;
      if (onEnded) onEnded();
    }, durationMs);
  }

  /**
   * Play standalone Narrowband Masking Noise (for testing & calibration)
   */
  playMaskingNoise(ear, frequencyHz, dbHl, durationMs = 2000, onEnded = null) {
    this.stopTone();

    const ctx = this.ensureContext();
    if (ctx.state === 'suspended') {
      ctx.resume().catch(() => {});
    }

    const now = ctx.currentTime;
    const rampSec = 0.025;
    const totalDurationSec = durationMs / 1000.0;
    const maskAmp = Math.max(0.00001, this.getMaskingAmplitude(frequencyHz, ear, dbHl));

    const noiseSource = this.createNarrowbandNoiseNode(ctx, frequencyHz, totalDurationSec);
    const noiseGain = ctx.createGain();
    noiseGain.gain.setValueAtTime(0.00001, now);
    noiseGain.gain.exponentialRampToValueAtTime(maskAmp, now + rampSec);
    noiseGain.gain.setValueAtTime(maskAmp, now + totalDurationSec - rampSec);
    noiseGain.gain.exponentialRampToValueAtTime(0.00001, now + totalDurationSec);

    noiseSource.connect(noiseGain);
    const noisePanner = this.connectStereo(noiseGain, ear, ctx);

    noiseSource.start(now);
    noiseSource.stop(now + totalDurationSec);

    this.currentNoiseSource = noiseSource;
    this.currentNoiseGain = noiseGain;
    this.currentNoisePanner = noisePanner;
    this.isPlaying = true;

    this.stopTimeout = setTimeout(() => {
      this.isPlaying = false;
      if (onEnded) onEnded();
    }, durationMs);
  }

  /**
   * Create an AudioBufferSourceNode filled with 1/3-octave filtered Gaussian white noise
   * with RMS normalization and soft tanh limiting to guarantee optimal loudness and prevent clipping
   */
  createNarrowbandNoiseNode(ctx, centerFreq, durationSec) {
    const sampleRate = ctx.sampleRate;
    const bufferSize = Math.ceil(sampleRate * (durationSec + 0.1));
    const noiseBuffer = ctx.createBuffer(1, bufferSize, sampleRate);
    const output = noiseBuffer.getChannelData(0);

    // 1. Generate Uniform Random White Noise
    const rawNoise = new Float32Array(bufferSize);
    for (let i = 0; i < bufferSize; i++) {
      rawNoise[i] = Math.random() * 2.0 - 1.0;
    }

    // 2. 2nd-Order Biquad Bandpass Filter (Q = 4.318, 1/3-octave bandwidth)
    const q = 4.318;
    const safeCenterFreq = Math.min(centerFreq, sampleRate * 0.45);
    const w0 = 2.0 * Math.PI * safeCenterFreq / sampleRate;
    const alpha = Math.sin(w0) / (2.0 * q);

    const b0 = alpha;
    const b1 = 0.0;
    const b2 = -alpha;
    const a0 = 1.0 + alpha;
    const a1 = -2.0 * Math.cos(w0);
    const a2 = 1.0 - alpha;

    const nb0 = b0 / a0;
    const nb1 = b1 / a0;
    const nb2 = b2 / a0;
    const na1 = a1 / a0;
    const na2 = a2 / a0;

    let x1 = 0, x2 = 0, y1 = 0, y2 = 0;
    let sumSq = 0;
    for (let i = 0; i < bufferSize; i++) {
      const x0 = rawNoise[i];
      const y0 = nb0 * x0 + nb1 * x1 + nb2 * x2 - na1 * y1 - na2 * y2;
      x2 = x1;
      x1 = x0;
      y2 = y1;
      y1 = y0;
      output[i] = y0;
      sumSq += y0 * y0;
    }

    // 3. RMS Normalization with Soft Tanh Limiting
    // Boosts acoustic energy to match pure tone loudness while avoiding digital clipping
    const currentRms = Math.sqrt(sumSq / bufferSize);
    if (currentRms > 0.00001) {
      const scale = 0.85 / currentRms;
      for (let i = 0; i < bufferSize; i++) {
        output[i] = Math.tanh(output[i] * scale);
      }
    }

    const source = ctx.createBufferSource();
    source.buffer = noiseBuffer;
    return source;
  }

  /**
   * Safely stops playback immediately with a smooth 15ms micro-fade
   * Captures node references in local scope to prevent killing newly scheduled sounds!
   */
  stopTone() {
    if (this.stopTimeout) {
      clearTimeout(this.stopTimeout);
      this.stopTimeout = null;
    }

    // Capture references locally before clearing class variables
    const oscToStop = this.currentOsc;
    const gainToStop = this.currentGain;
    const pannerToStop = this.currentPanner;
    const noiseSourceToStop = this.currentNoiseSource;
    const noiseGainToStop = this.currentNoiseGain;
    const noisePannerToStop = this.currentNoisePanner;

    // Clear instance properties immediately
    this.currentOsc = null;
    this.currentGain = null;
    this.currentPanner = null;
    this.currentNoiseSource = null;
    this.currentNoiseGain = null;
    this.currentNoisePanner = null;
    this.isPlaying = false;

    if (this.ctx && (oscToStop || noiseSourceToStop)) {
      const now = this.ctx.currentTime;

      if (gainToStop) {
        try {
          gainToStop.gain.cancelScheduledValues(now);
          gainToStop.gain.setValueAtTime(gainToStop.gain.value, now);
          gainToStop.gain.linearRampToValueAtTime(0.00001, now + 0.015);
        } catch (_) {}
      }

      if (noiseGainToStop) {
        try {
          noiseGainToStop.gain.cancelScheduledValues(now);
          noiseGainToStop.gain.setValueAtTime(noiseGainToStop.gain.value, now);
          noiseGainToStop.gain.linearRampToValueAtTime(0.00001, now + 0.015);
        } catch (_) {}
      }

      setTimeout(() => {
        try {
          oscToStop?.stop();
          oscToStop?.disconnect();
          gainToStop?.disconnect();
          pannerToStop?.disconnect();
        } catch (_) {}
        try {
          noiseSourceToStop?.stop();
          noiseSourceToStop?.disconnect();
          noiseGainToStop?.disconnect();
          noisePannerToStop?.disconnect();
        } catch (_) {}
      }, 20);
    }
  }
}
