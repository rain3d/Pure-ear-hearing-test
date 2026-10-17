/**
 * Calibration module with ISO 389-1 RETSPL curves and Phone SLM Anchor
 * Calibrated specifically for mobile device listening environments.
 */
export class Calibrator {
  constructor() {
    this.retSplTable = {
      250: 25.5,
      500: 11.5,
      1000: 7.0,
      2000: 9.0,
      4000: 9.5,
      8000: 13.0
    };
    this.profile = this.loadProfile();
  }

  loadProfile() {
    try {
      const saved = localStorage.getItem('hearing_test_calib_profile');
      if (saved) return JSON.parse(saved);
    } catch (_) {}
    return null;
  }

  get isCalibrated() {
    return this.profile !== null;
  }

  /**
   * Convert dB HL to digital dBFS
   */
  hlToDbfs(hl, freq, ear) {
    if (!this.profile) {
      // Uncalibrated relative scale tailored for mobile speakers & headphones:
      // 0 dB HL  -> -50.0 dBFS (whisper quiet threshold)
      // 30 dB HL -> -26.0 dBFS (comfortably audible screening start tone)
      // 50 dB HL -> -10.0 dBFS (safe ceiling)
      return -50.0 + (hl * 0.8);
    }

    const retspl = this.retSplTable[freq] || 7.0;
    const measured = ear === 'Left' ? this.profile.measuredSplLeft : this.profile.measuredSplRight;
    const retspl1000 = this.retSplTable[1000];
    
    // Frequency-dependent measured SPL based on 1000Hz anchor
    const freqMeasuredSpl = (measured - retspl1000) + retspl;
    const targetSpl = hl + retspl;
    const gain = targetSpl - freqMeasuredSpl;
    const calculated = this.profile.referenceDbfs + gain;
    return Math.max(-65.0, Math.min(-10.0, calculated));
  }

  applyPhoneCalibration(measuredLeft, measuredRight, referenceDbfs = -20.0) {
    const profile = {
      calibratedAt: new Date().toISOString(),
      referenceDbfs: referenceDbfs,
      measuredSplLeft: Math.max(30, Math.min(110, measuredLeft)),
      measuredSplRight: Math.max(30, Math.min(110, measuredRight)),
      label: '手機分貝計基準校驗 (Phone SLM)'
    };
    this.profile = profile;
    localStorage.setItem('hearing_test_calib_profile', JSON.stringify(profile));
    return profile;
  }

  reset() {
    this.profile = null;
    localStorage.removeItem('hearing_test_calib_profile');
  }
}
