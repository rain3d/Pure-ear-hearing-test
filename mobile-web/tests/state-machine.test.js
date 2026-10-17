import assert from 'node:assert';
import { AudiometryStateMachine } from '../js/state-machine.js';

console.log('--- Running Mobile AudiometryStateMachine Catch Trial Unit Tests ---');

// Test 1: Initialization state
{
  const sm = new AudiometryStateMachine();
  assert.strictEqual(sm.isCurrentStimulusCatchTrial, false);
  assert.strictEqual(sm.catchTrialEnabled, true);
  assert.strictEqual(sm.catchTrialCount, 0);
  assert.strictEqual(sm.falsePositiveCount, 0);
  assert.strictEqual(sm.totalCatchTrials, 0);
  console.log('✓ Test 1: Initial state verified');
}

// Test 2: First stimulus is never a catch trial
{
  const sm = new AudiometryStateMachine();
  sm.start();
  const stim1 = sm.prepareNextStimulus(true);
  assert.strictEqual(stim1.isCatchTrial, false, 'First stimulus must never be a catch trial');
  assert.strictEqual(sm.isCurrentStimulusCatchTrial, false);
  assert.strictEqual(stim1.level, 30.0);
  console.log('✓ Test 2: First stimulus rule verified');
}

// Test 3: Catch Trial - Patient responds (False Positive blind guess)
{
  const sm = new AudiometryStateMachine();
  sm.start();
  sm.prepareNextStimulus(false); // trial 1: normal
  sm.recordResponse(true, 400); // 30 -> heard -> 20

  // Force catch trial
  sm.trialsSinceLastCatch = 5;
  sm.totalStimulusCount = 5;
  sm.isCurrentStimulusCatchTrial = true;
  sm.catchTrialCount = 1;
  sm.totalCatchTrials = 1;

  const currentLevelBefore = sm.currentLevel; // 20
  const res = sm.recordResponse(true, 350); // Patient claims to hear silence!

  assert.strictEqual(res.isCatchTrial, true);
  assert.strictEqual(res.isFalsePositive, true);
  assert.strictEqual(sm.catchTrialPresses, 1);
  assert.strictEqual(sm.falsePositiveCount, 1);
  assert.strictEqual(sm.currentLevel, currentLevelBefore, 'Catch trial must NOT alter Hughson-Westlake level');
  assert.strictEqual(sm.thresholdResults.length, 0, 'Catch trial must NOT establish threshold');
  console.log('✓ Test 3: Catch trial false positive correctly captured and level unchanged');
}

// Test 4: Catch Trial - Patient does not respond (Correct Rejection)
{
  const sm = new AudiometryStateMachine();
  sm.start();
  sm.prepareNextStimulus(false); // trial 1
  sm.recordResponse(true, 500);

  // Force catch trial
  sm.isCurrentStimulusCatchTrial = true;
  sm.catchTrialCount = 1;
  sm.totalCatchTrials = 1;

  const currentLevelBefore = sm.currentLevel;
  const res = sm.recordResponse(false, null); // 3 seconds expired, no response

  assert.strictEqual(res.isCatchTrial, true);
  assert.strictEqual(res.isFalsePositive, false);
  assert.strictEqual(sm.catchTrialPresses, 0);
  assert.strictEqual(sm.falsePositiveCount, 0);
  assert.strictEqual(sm.currentLevel, currentLevelBefore, 'Catch trial must NOT step up by 5 dB on not heard');
  console.log('✓ Test 4: Catch trial correct rejection passed without stepping level');
}

// Test 5: Spontaneous False Positive during jitter gaps
{
  const sm = new AudiometryStateMachine();
  sm.start();
  sm.recordSpontaneousFalsePositive();

  assert.strictEqual(sm.spontaneousFalsePositives, 1);
  assert.strictEqual(sm.falsePositiveCount, 1);
  assert.strictEqual(sm.totalCatchTrials, 1);
  assert.strictEqual(sm.events[0].isSpontaneous, true);
  console.log('✓ Test 5: Spontaneous jitter gap press recorded');
}

// Test 6: Reliability calculation and stats
{
  const sm = new AudiometryStateMachine();
  sm.start();

  // Initially no catch trials
  assert.strictEqual(sm.calculateReliability(), '良好 (Good)');

  // 4 catch trials with 0 false positives
  sm.totalCatchTrials = 4;
  sm.catchTrialCount = 4;
  sm.falsePositiveCount = 0;
  assert.strictEqual(sm.calculateReliability(), '極佳 (Excellent)');

  // 1 false positive out of 10 -> 10%
  sm.totalCatchTrials = 10;
  sm.falsePositiveCount = 1;
  assert.strictEqual(sm.calculateReliability(), '良好 (Good)');

  // 2 false positives out of 10 -> 20%
  sm.falsePositiveCount = 2;
  assert.strictEqual(sm.calculateReliability(), '尚可 (Fair)');

  // 4 false positives out of 10 -> 40%
  sm.falsePositiveCount = 4;
  assert.strictEqual(sm.calculateReliability(), '存疑 (Questionable - 盲按次數過多)');

  const stats = sm.getCatchTrialStats();
  assert.strictEqual(stats.totalCatchTrials, 10);
  assert.strictEqual(stats.falsePositiveCount, 4);
  assert.strictEqual(stats.ratePercentage, 40);
  console.log('✓ Test 6: Reliability calculation and stats verified');
}

// Test 7: Multi-trial simulation to verify spacing and no back-to-back catch trials
{
  const sm = new AudiometryStateMachine();
  sm.start();
  let lastWasCatch = false;
  let intervalSinceLastCatch = 0;
  let catchCount = 0;

  for (let i = 1; i <= 200; i++) {
    const stim = sm.prepareNextStimulus(true);
    if (i === 1) {
      assert.strictEqual(stim.isCatchTrial, false, 'Trial 1 cannot be catch trial');
    }
    if (stim.isCatchTrial) {
      assert.strictEqual(lastWasCatch, false, 'Cannot have consecutive catch trials');
      assert.ok(intervalSinceLastCatch >= 2, `Interval must be >= 2, got ${intervalSinceLastCatch}`);
      catchCount++;
      lastWasCatch = true;
      intervalSinceLastCatch = 0;
      sm.recordResponse(false); // patient didn't press
    } else {
      lastWasCatch = false;
      intervalSinceLastCatch++;
      sm.recordResponse(true, 300); // patient pressed
    }
  }

  assert.ok(catchCount >= 30 && catchCount <= 60, `Catch trials should appear in reasonable clinical range (~20-25%), got ${catchCount}`);
  assert.strictEqual(sm.catchTrialCount, catchCount);
  assert.strictEqual(sm.catchTrialPresses, 0);
  assert.strictEqual(sm.calculateReliability(), '極佳 (Excellent)');
  console.log(`✓ Test 7: 200-stimulus simulation passed (Generated ${catchCount} catch trials with strict >=2 spacing)`);
}

// Test 8: Left Ear 1000 Hz descends to 0 dB HL and advances to next frequency
{
  const sm = new AudiometryStateMachine();
  sm.start();

  // 1. 30 dB HL -> heard -> down 10 to 20 dB HL
  sm.prepareNextStimulus(false);
  let res = sm.recordResponse(true, 300);
  assert.strictEqual(sm.currentEar, 'Left');
  assert.strictEqual(sm.currentFrequency, 1000);
  assert.strictEqual(sm.currentLevel, 20.0);
  assert.strictEqual(res.thresholdFound, false);

  // 2. 20 dB HL -> heard -> down 10 to 10 dB HL
  sm.prepareNextStimulus(false);
  res = sm.recordResponse(true, 300);
  assert.strictEqual(sm.currentLevel, 10.0);
  assert.strictEqual(res.thresholdFound, false);

  // 3. 10 dB HL -> heard -> down 10 to 0 dB HL (Floor)
  sm.prepareNextStimulus(false);
  res = sm.recordResponse(true, 300);
  assert.strictEqual(sm.currentLevel, 0.0);
  assert.strictEqual(res.thresholdFound, false);

  // 4. 0 dB HL -> heard -> 1st floor response, level stays 0 dB HL for confirmation
  sm.prepareNextStimulus(false);
  res = sm.recordResponse(true, 300);
  assert.strictEqual(sm.currentLevel, 0.0);
  assert.strictEqual(res.thresholdFound, false, '1st response at 0 dB HL requires confirmation');

  // 5. 0 dB HL -> heard -> 2nd floor response! Threshold established at 0 dB HL!
  sm.prepareNextStimulus(false);
  res = sm.recordResponse(true, 300);
  assert.strictEqual(res.thresholdFound, true, '2nd response at 0 dB HL must establish threshold');
  assert.strictEqual(sm.thresholdResults.length, 1);
  assert.strictEqual(sm.thresholdResults[0].ear, 'Left');
  assert.strictEqual(sm.thresholdResults[0].freq, 1000);
  assert.strictEqual(sm.thresholdResults[0].threshold, 0.0);
  assert.strictEqual(sm.thresholdResults[0].isNoResponse, false);

  // Must advance to NEXT frequency: 2000 Hz, with level reset to 30 dB HL!
  assert.strictEqual(sm.currentFrequency, 2000, 'Must advance to 2000 Hz after 1000 Hz completes');
  assert.strictEqual(sm.currentLevel, 30.0, 'Level must reset to 30 dB HL for new frequency');
  console.log('✓ Test 8: Left Ear 1000 Hz 0 dB HL bug fixed and successfully advances to 2000 Hz');
}

// Test 9: 0 dB HL with not heard -> steps to 5 dB HL -> steps back down to 0 dB HL
{
  const sm = new AudiometryStateMachine();
  sm.start();

  // Descend to 0 dB
  sm.prepareNextStimulus(false); sm.recordResponse(true, 300); // 30 -> 20
  sm.prepareNextStimulus(false); sm.recordResponse(true, 300); // 20 -> 10
  sm.prepareNextStimulus(false); sm.recordResponse(true, 300); // 10 -> 0

  // 1st time at 0 dB: Patient does NOT hear -> steps up 5 dB to 5 dB HL
  sm.prepareNextStimulus(false);
  let res = sm.recordResponse(false);
  assert.strictEqual(sm.currentLevel, 5.0);
  assert.strictEqual(res.thresholdFound, false);

  // At 5 dB HL: Patient hears -> ascending response at 5 dB (count 1), steps down 10 to 0 dB HL
  sm.prepareNextStimulus(false);
  res = sm.recordResponse(true, 300);
  assert.strictEqual(sm.currentLevel, 0.0);
  assert.strictEqual(res.thresholdFound, false);

  // At 0 dB HL: Patient hears (1st response at 0 dB)
  sm.prepareNextStimulus(false);
  res = sm.recordResponse(true, 300);
  assert.strictEqual(sm.currentLevel, 0.0);
  assert.strictEqual(res.thresholdFound, false);

  // At 0 dB HL: Patient hears again (2nd response at 0 dB) -> Threshold 0 dB established!
  sm.prepareNextStimulus(false);
  res = sm.recordResponse(true, 300);
  assert.strictEqual(res.thresholdFound, true);
  assert.strictEqual(sm.thresholdResults[0].threshold, 0.0);
  assert.strictEqual(sm.currentFrequency, 2000);
  console.log('✓ Test 9: 0 dB HL recovery from 5 dB HL verified');
}

// Test 10: 100-run short session simulation (10 stimuli each) ensuring 0% zero-catch runs
{
  let zeroCatchRuns = 0;
  let totalCatches = 0;
  const runs = 100;

  for (let r = 0; r < runs; r++) {
    const sm = new AudiometryStateMachine();
    sm.start();
    for (let s = 0; s < 10; s++) {
      const stim = sm.prepareNextStimulus(true);
      sm.recordResponse(!stim.isCatchTrial);
    }
    if (sm.catchTrialCount === 0) zeroCatchRuns++;
    totalCatches += sm.catchTrialCount;
  }

  assert.strictEqual(zeroCatchRuns, 0, 'Zero-catch runs must be 0% with upper-bound guarantee');
  const avgCatches = totalCatches / runs;
  assert.ok(avgCatches >= 1.8 && avgCatches <= 2.5, `Average catches per 10 stimuli should be ~2 (got ${avgCatches})`);
  console.log(`✓ Test 10: 100 short sessions verified (0% zero-catch runs, average ${avgCatches} silent catch trials per 10 stimuli)`);
}

console.log('🎉 All mobile state-machine Catch Trial & Floor tests passed successfully!');
