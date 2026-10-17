// ============================================================================
// 純音聽力測驗系統 (Pure-Tone HearingTest)
// Author: rain3d (https://github.com/rain3d)
// License: Creative Commons Attribution-NonCommercial 4.0 International (CC BY-NC 4.0)
// 免費提供個人自主監測與非營利學術研究使用；嚴格禁止任何商業營利與付費銷售用途。
// ============================================================================

using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using HearingTest.App.Localization;
using HearingTest.Audio;
using HearingTest.Calibration;
using HearingTest.Domain;
using HearingTest.Domain.Interfaces;
using HearingTest.Domain.Models;
using HearingTest.Domain.StateMachine;
using HearingTest.Hardware;
using HearingTest.Persistence;
using HearingTest.Reporting;

namespace HearingTest.App.ViewModels;

public class MainViewModel : ViewModelBase
{
    public IReadOnlyList<LanguageOption> AvailableLanguages => LocalizationService.SupportedLanguages;

    public LanguageOption SelectedLanguage
    {
        get => LocalizationService.CurrentLanguage;
        set
        {
            if (value != null && value.Code != LocalizationService.CurrentLanguage.Code)
            {
                LocalizationService.SetLanguage(value.Code);
            }
        }
    }
    private readonly ITestSessionRepository _repository;
    private readonly ICalibrationService _calibrationService;
    private readonly IReportExporter _reportExporter;
    private readonly IAudioOutput _audioOutput;
    private readonly SimulatedPatient _simulatedPatient;

    private AudiometryStateMachine _stateMachine;
    private Patient _currentPatient;
    private string _currentViewName = "Home";

    private bool _isEmergencyStopped = false;
    private string _emergencyStopMessage = string.Empty;
    private bool _isStimulusActive = false;
    private string _stimulusStatusText = "準備中";
    private bool _useSimulatedPatientAutoResponse = false;
    private bool _autoAdvance = true;
    private bool _blindPressWarning = false;
    private string _blindPressWarningText = string.Empty;

    private readonly Random _random = new();
    private CancellationTokenSource? _stimulusCts;
    private TaskCompletionSource<bool>? _responseTcs;
    private readonly System.Diagnostics.Stopwatch _stimulusStopwatch = new();

    public ObservableCollection<ThresholdResult> ThresholdResults { get; } = [];
    public ObservableCollection<StimulusEvent> RecentEvents { get; } = [];
    public ObservableCollection<TestSession> PastSessions { get; } = [];

    public string CurrentViewName
    {
        get => _currentViewName;
        set => SetProperty(ref _currentViewName, value);
    }

    public Patient CurrentPatient
    {
        get => _currentPatient;
        set => SetProperty(ref _currentPatient, value);
    }

    public Ear CurrentEar => _stateMachine.CurrentEar;
    public string CurrentEarText => CurrentEar == Ear.Left
        ? LocalizationService.GetString("Str_EarLeft", "左耳 (Left Ear - X)")
        : LocalizationService.GetString("Str_EarRight", "右耳 (Right Ear - O)");
    public Brush CurrentEarBrush => CurrentEar == Ear.Left
        ? new SolidColorBrush(Color.FromRgb(59, 130, 246))
        : new SolidColorBrush(Color.FromRgb(239, 68, 68));

    public int CurrentFrequencyHz => _stateMachine.CurrentFrequencyHz;
    public double CurrentLevelHl => _stateMachine.CurrentLevelHl;

    public string CurrentLevelDisplay => _stateMachine.IsCurrentStimulusCatchTrial
        ? LocalizationService.GetString("Str_CatchTrialLabel", "對照靜音 (Catch Trial)")
        : $"{CurrentLevelHl:0} dB HL";

    public bool IsEmergencyStopped
    {
        get => _isEmergencyStopped;
        set => SetProperty(ref _isEmergencyStopped, value);
    }

    public string EmergencyStopMessage
    {
        get => _emergencyStopMessage;
        set => SetProperty(ref _emergencyStopMessage, value);
    }

    public bool IsStimulusActive
    {
        get => _isStimulusActive;
        set => SetProperty(ref _isStimulusActive, value);
    }

    public string StimulusStatusText
    {
        get => _stimulusStatusText;
        set => SetProperty(ref _stimulusStatusText, value);
    }

    public bool UseSimulatedPatientAutoResponse
    {
        get => _useSimulatedPatientAutoResponse;
        set => SetProperty(ref _useSimulatedPatientAutoResponse, value);
    }

    public bool AutoAdvance
    {
        get => _autoAdvance;
        set => SetProperty(ref _autoAdvance, value);
    }

    public bool BlindPressWarning
    {
        get => _blindPressWarning;
        set => SetProperty(ref _blindPressWarning, value);
    }

    public string BlindPressWarningText
    {
        get => _blindPressWarningText;
        set => SetProperty(ref _blindPressWarningText, value);
    }

    public bool IsCalibrated => _calibrationService.IsCalibrated;

    public string CalibrationBadgeText => IsCalibrated
        ? LocalizationService.GetString("Str_CalibrationBadgeValid", "校正狀態：有效 (已驗證)")
        : LocalizationService.GetString("Str_CalibrationBadgeInvalid", "校正狀態：未校正 (研究模式)");

    public Brush CalibrationBadgeBrush => IsCalibrated
        ? new SolidColorBrush(Color.FromRgb(16, 185, 129))
        : new SolidColorBrush(Color.FromRgb(245, 158, 11));

    public HardwareProfile CurrentHardware { get; }
    public ObservableCollection<CalibrationPoint> CalibrationPoints { get; } = [];
    public string CalibrationLabInfo => _calibrationService.CurrentProfile != null
        ? $"{_calibrationService.CurrentProfile.Laboratory} ({_calibrationService.CurrentProfile.Traceability})"
        : "尚未載入校正檔";

    public string CalibrationExpiryInfo => _calibrationService.CurrentProfile != null
        ? $"{_calibrationService.CurrentProfile.ExpiresAt:yyyy-MM-dd} (有效期限)"
        : "無效 / 未校正";

    public string HardwareFingerprintDisplay => CurrentHardware.Fingerprint;
    public string CalibrationFingerprintDisplay => _calibrationService.CurrentProfile?.HardwareFingerprint ?? "未指定";

    public string CalibrationStatusDescription => IsCalibrated
        ? "校正檔完整通過 SHA-256 簽章驗證與本機硬體指紋比對，具備臨床參考效力。"
        : "目前處於研究模式（未校正或指紋不符），系統強度強制鎖定為相對研究刻度。";

    public string ReliabilityText => $"可靠度評估：{_stateMachine.CalculateCurrentReliability()} (假陽性: {_stateMachine.FalsePositiveCount}/{_stateMachine.TotalCatchTrials})";

    public TestProgressState TestState => _stateMachine.State;
    public bool IsTestCompleted => TestState == TestProgressState.Completed;
    public bool IsTestActive => TestState == TestProgressState.Testing;

    private bool _isCompletionOverlayVisible = false;
    public bool IsCompletionOverlayVisible
    {
        get => _isCompletionOverlayVisible;
        set => SetProperty(ref _isCompletionOverlayVisible, value);
    }

    private bool _isCountdownActive = false;
    private int _countdownSeconds = 10;
    private CancellationTokenSource? _countdownCts;

    public bool IsCountdownActive
    {
        get => _isCountdownActive;
        set
        {
            if (SetProperty(ref _isCountdownActive, value))
            {
                OnPropertyChanged(nameof(IsPatientTestingActive));
            }
        }
    }

    public int CountdownSeconds
    {
        get => _countdownSeconds;
        set
        {
            if (SetProperty(ref _countdownSeconds, value))
            {
                OnPropertyChanged(nameof(CountdownText));
            }
        }
    }

    public string CountdownText => CountdownSeconds > 0 ? CountdownSeconds.ToString() : "1";

    public bool IsPatientTestingActive => !IsTestCompleted && !IsCountdownActive;

    public double? CalculatePta(Ear ear)
    {
        var targetFreqs = new[] { 500, 1000, 2000, 4000 };
        var matched = ThresholdResults
            .Where(r => r.Ear == ear && targetFreqs.Contains(r.FrequencyHz) && r.ThresholdHl.HasValue)
            .ToList();
        if (matched.Count > 0)
        {
            return Math.Round(matched.Average(r => r.ThresholdHl!.Value), 1);
        }
        var allForEar = ThresholdResults.Where(r => r.Ear == ear && r.ThresholdHl.HasValue).ToList();
        if (allForEar.Count > 0)
        {
            return Math.Round(allForEar.Average(r => r.ThresholdHl!.Value), 1);
        }
        return null;
    }

    public static string GetHearingGrade(double? pta)
    {
        if (!pta.HasValue) return LocalizationService.GetString("Str_GradeNone", "未測得有效聽閾");
        double val = pta.Value;
        if (val <= 25.0) return LocalizationService.GetString("Str_GradeNormal", "正常聽力 (Normal)");
        if (val <= 40.0) return LocalizationService.GetString("Str_GradeMild", "輕度聽損 (Mild)");
        if (val <= 55.0) return LocalizationService.GetString("Str_GradeModerate", "中度聽損 (Moderate)");
        if (val <= 70.0) return LocalizationService.GetString("Str_GradeModeratelySevere", "中重度聽損 (Moderately Severe)");
        if (val <= 90.0) return LocalizationService.GetString("Str_GradeSevere", "重度聽損 (Severe)");
        return LocalizationService.GetString("Str_GradeProfound", "極重度聽損 (Profound)");
    }

    public string LeftEarPtaDisplay
    {
        get
        {
            var pta = CalculatePta(Ear.Left);
            return pta.HasValue ? $"{pta.Value:F1} dB HL" : "-- dB HL";
        }
    }

    public string LeftEarGradeDisplay => GetHearingGrade(CalculatePta(Ear.Left));

    public string RightEarPtaDisplay
    {
        get
        {
            var pta = CalculatePta(Ear.Right);
            return pta.HasValue ? $"{pta.Value:F1} dB HL" : "-- dB HL";
        }
    }

    public string RightEarGradeDisplay => GetHearingGrade(CalculatePta(Ear.Right));

    public string OverallReliabilityDisplay => _stateMachine.CalculateCurrentReliability() switch
    {
        ReliabilityRating.Good => LocalizationService.GetString("Str_ReliabilityGood", "良好 (Good) - 假陽性率低，測試數據具備高信賴度"),
        ReliabilityRating.Questionable => LocalizationService.GetString("Str_ReliabilityQuestionable", "可疑 (Questionable) - 假陽性或 1000Hz 複測差異稍高"),
        ReliabilityRating.Poor => LocalizationService.GetString("Str_ReliabilityPoor", "不佳 (Poor) - 建議重新施測"),
        _ => LocalizationService.GetString("Str_ReliabilityAssessing", "評估中")
    };

    private readonly CalibrationProfileManager _calibrationProfileManager;
    private readonly IMaskingProtocol _maskingProtocol = new StandardMaskingProtocol();
    private bool _isMaskingEnabled = false;
    private double _maskingLevelHl = 30.0;
    private string _maskingRecommendationText = "未達對側遮蔽標準 (IA < 40 dB)";
    private Views.PatientWindow? _patientWindow;

    public bool IsMaskingEnabled
    {
        get => _isMaskingEnabled;
        set
        {
            SetProperty(ref _isMaskingEnabled, value);
            UpdateMaskingRecommendation();
            OnPropertyChanged(nameof(MaskingStatusDescription));
        }
    }

    public double MaskingLevelHl
    {
        get => _maskingLevelHl;
        set
        {
            SetProperty(ref _maskingLevelHl, value);
            OnPropertyChanged(nameof(MaskingStatusDescription));
        }
    }

    public ObservableCollection<double> AvailableMaskingLevels { get; } = [0, 5, 10, 15, 20, 25, 30, 35, 40, 45, 50];

    public string MaskingRecommendationText
    {
        get => _maskingRecommendationText;
        set => SetProperty(ref _maskingRecommendationText, value);
    }

    public string MaskingStatusDescription => IsMaskingEnabled
        ? $"對側耳 ({(CurrentEar == Ear.Left ? "右耳" : "左耳")}) 輸出 {MaskingLevelHl:0} dB HL 窄頻雜音"
        : "對側遮蔽未啟用";

    // Smartphone Calibrator Properties
    private int _calibratorSelectedEarIndex = 0; // 0: Left, 1: Right
    private double _calibratorMeasuredSplLeft = 70.0;
    private double _calibratorMeasuredSplRight = 70.0;
    private bool _isCalibratorTonePlaying = false;
    private string _calibratorStatusText = "準備就緒。請將耳機貼緊手機麥克風，點擊【播放 1000 Hz 參考音】以量測聲壓。";
    private CancellationTokenSource? _calibratorCts;

    public int CalibratorSelectedEarIndex
    {
        get => _calibratorSelectedEarIndex;
        set => SetProperty(ref _calibratorSelectedEarIndex, value);
    }

    public double CalibratorMeasuredSplLeft
    {
        get => _calibratorMeasuredSplLeft;
        set => SetProperty(ref _calibratorMeasuredSplLeft, value);
    }

    public double CalibratorMeasuredSplRight
    {
        get => _calibratorMeasuredSplRight;
        set => SetProperty(ref _calibratorMeasuredSplRight, value);
    }

    public bool IsCalibratorTonePlaying
    {
        get => _isCalibratorTonePlaying;
        set => SetProperty(ref _isCalibratorTonePlaying, value);
    }

    public string CalibratorStatusText
    {
        get => _calibratorStatusText;
        set => SetProperty(ref _calibratorStatusText, value);
    }

    // Commands
    public ICommand NavigateCommand { get; }
    public ICommand StartSixFreqTestCommand { get; }
    public ICommand StartQuickVerificationCommand { get; }
    public ICommand NextStimulusCommand { get; }
    public ICommand HeardResponseCommand { get; }
    public ICommand NotHeardResponseCommand { get; }
    public ICommand StepLevelUpCommand { get; }
    public ICommand StepLevelDownCommand { get; }
    public ICommand EmergencyStopCommand { get; }
    public ICommand ResetEmergencyStopCommand { get; }
    public ICommand PauseResumeCommand { get; }
    public ICommand RollbackCommand { get; }
    public ICommand ExportJsonCommand { get; }
    public ICommand ExportCsvCommand { get; }
    public ICommand RefreshHistoryCommand { get; }
    public ICommand ImportCalibrationFileCommand { get; }
    public ICommand GenerateSampleCalibrationCommand { get; }
    public ICommand UnloadCalibrationCommand { get; }
    public ICommand OpenPatientWindowCommand { get; }
    public ICommand ClosePatientWindowCommand { get; }
    public ICommand AutoSetMaskingCommand { get; }
    public ICommand PlayCalibrationToneCommand { get; }
    public ICommand StopCalibrationToneCommand { get; }
    public ICommand ApplyPhoneCalibrationCommand { get; }
    public ICommand DismissCompletionOverlayCommand { get; }
    public ICommand ShowCompletionOverlayCommand { get; }
    public ICommand RestartTestCommand { get; }
    public ICommand SkipCountdownCommand { get; }
    public ICommand CopyLeftToRightSplCommand { get; }

    public MainViewModel()
    {
        _repository = new SqliteSessionRepository();
        _calibrationService = new CalibrationService();
        _calibrationProfileManager = new CalibrationProfileManager();
        _reportExporter = new ReportExporter();

        CurrentHardware = new HardwareProfile
        {
            Computer = Environment.MachineName,
            AudioDevice = "WASAPI Exclusive (48 kHz / 24-bit)",
            HeadphoneModel = "Sennheiser HDA 300 / Research Closed Monitor",
            HeadphoneSerialLeft = "SN-2026-L",
            HeadphoneSerialRight = "SN-2026-R"
        };
        CurrentHardware.Fingerprint = HardwareFingerprint.Generate(CurrentHardware);

        try
        {
            _audioOutput = new WasapiAudioOutput();
        }
        catch
        {
            _audioOutput = new SimulatedAudioOutput();
        }
        _audioOutput.ErrorOccurred += (s, err) =>
        {
            Application.Current?.Dispatcher.Invoke(() =>
            {
                StimulusStatusText = $"音訊警報: {err}";
            });
        };
        _simulatedPatient = new SimulatedPatient(seed: 2026);
        _simulatedPatient.SetLeftEarVerificationProfile();

        _currentPatient = Patient.CreateAnonymous();
        _stateMachine = new AudiometryStateMachine();

        _ = AutoLoadDefaultCalibrationAsync();

        // Wire up commands
        NavigateCommand = new RelayCommand(param =>
        {
            if (param is string view) CurrentViewName = view;
        });

        StartSixFreqTestCommand = new RelayCommand(StartSixFrequencyTest);
        StartQuickVerificationCommand = new RelayCommand(StartQuickVerificationTest);
        NextStimulusCommand = new RelayCommand(async () => await PresentNextStimulusAsync());
        HeardResponseCommand = new RelayCommand(OnSpaceOrResponseClicked);
        NotHeardResponseCommand = new RelayCommand(OnNotHeardClicked);

        StepLevelUpCommand = new RelayCommand(() =>
        {
            if (CurrentLevelHl + 5.0 <= 50.0)
            {
            }
        });

        StepLevelDownCommand = new RelayCommand(() =>
        {
            if (CurrentLevelHl - 5.0 >= 0.0)
            {
            }
        });

        EmergencyStopCommand = new RelayCommand(() => TriggerEmergencyStop("操作者觸發緊急停止"));
        ResetEmergencyStopCommand = new RelayCommand(ResetEmergencyStop);
        PauseResumeCommand = new RelayCommand(TogglePauseResume);
        RollbackCommand = new RelayCommand(RollbackStep);
        ExportJsonCommand = new RelayCommand(async () => await ExportCurrentSessionJsonAsync());
        ExportCsvCommand = new RelayCommand(async () => await ExportCurrentSessionCsvAsync());
        RefreshHistoryCommand = new RelayCommand(async () => await LoadHistoryAsync());

        ImportCalibrationFileCommand = new RelayCommand(async () => await ImportCalibrationFileAsync());
        GenerateSampleCalibrationCommand = new RelayCommand(async () => await GenerateSampleCalibrationAsync());
        UnloadCalibrationCommand = new RelayCommand(UnloadCalibration);
        OpenPatientWindowCommand = new RelayCommand(OpenPatientWindow);
        ClosePatientWindowCommand = new RelayCommand(ClosePatientWindow);
        AutoSetMaskingCommand = new RelayCommand(ApplyAutoMaskingLevel);
        PlayCalibrationToneCommand = new RelayCommand(async () => await PlayCalibrationToneAsync());
        StopCalibrationToneCommand = new RelayCommand(StopCalibrationTone);
        ApplyPhoneCalibrationCommand = new RelayCommand(async () => await ApplyPhoneCalibrationAsync());
        DismissCompletionOverlayCommand = new RelayCommand(() => IsCompletionOverlayVisible = false);
        ShowCompletionOverlayCommand = new RelayCommand(() => IsCompletionOverlayVisible = true);
        RestartTestCommand = new RelayCommand(StartSixFrequencyTest);
        SkipCountdownCommand = new RelayCommand(SkipCountdown);
        CopyLeftToRightSplCommand = new RelayCommand(() =>
        {
            CalibratorMeasuredSplRight = CalibratorMeasuredSplLeft;
            CalibratorStatusText = "已將左耳實測讀數複製至右耳。";
        });

        _ = LoadHistoryAsync();

        LocalizationService.LanguageChanged += OnLanguageChanged;
    }

    private void OnLanguageChanged()
    {
        OnPropertyChanged(nameof(SelectedLanguage));
        OnPropertyChanged(nameof(CurrentEarText));
        OnPropertyChanged(nameof(CurrentLevelDisplay));
        OnPropertyChanged(nameof(CalibrationBadgeText));
        OnPropertyChanged(nameof(CalibrationExpiryInfo));
        OnPropertyChanged(nameof(CalibrationStatusDescription));
        OnPropertyChanged(nameof(LeftEarGradeDisplay));
        OnPropertyChanged(nameof(RightEarGradeDisplay));
        OnPropertyChanged(nameof(OverallReliabilityDisplay));
        OnPropertyChanged(nameof(ReliabilityText));
        OnPropertyChanged(nameof(MaskingStatusDescription));
        OnPropertyChanged(nameof(MaskingRecommendationText));
        OnPropertyChanged(nameof(StimulusStatusText));
        OnPropertyChanged(string.Empty);
    }

    public void OpenPatientWindow()
    {
        try
        {
            if (_patientWindow == null || !_patientWindow.IsLoaded)
            {
                _patientWindow = new Views.PatientWindow { DataContext = this };
                _patientWindow.Closed += (s, e) => _patientWindow = null;
                _patientWindow.Show();
            }
            else
            {
                _patientWindow.Activate();
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"開啟受測者副螢幕視窗失敗：\n{ex.Message}", "副螢幕開啟失敗", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    public void ClosePatientWindow()
    {
        _patientWindow?.Close();
        _patientWindow = null;
    }

    private void UpdateMaskingRecommendation()
    {
        Ear oppositeEar = CurrentEar == Ear.Left ? Ear.Right : Ear.Left;
        var oppThreshold = _stateMachine.CurrentSession.ThresholdResults
            .FirstOrDefault(r => r.Ear == oppositeEar && r.FrequencyHz == CurrentFrequencyHz)?.ThresholdHl;

        bool prompt = _maskingProtocol.ShouldPromptMasking(CurrentEar, CurrentFrequencyHz, CurrentLevelHl, oppThreshold, null);
        if (prompt)
        {
            double rec = _maskingProtocol.CalculateInitialMaskingLevel(oppositeEar, CurrentFrequencyHz, CurrentLevelHl);
            MaskingRecommendationText = $"⚠️ 跨耳衰減警示！建議開啟對側遮蔽 (推薦: {rec:0} dB HL)";
        }
        else
        {
            MaskingRecommendationText = "未達對側遮蔽標準 (IA < 40 dB)";
        }
        OnPropertyChanged(nameof(MaskingStatusDescription));
    }

    private void ApplyAutoMaskingLevel()
    {
        Ear oppositeEar = CurrentEar == Ear.Left ? Ear.Right : Ear.Left;
        double rec = _maskingProtocol.CalculateInitialMaskingLevel(oppositeEar, CurrentFrequencyHz, CurrentLevelHl);
        MaskingLevelHl = rec;
        IsMaskingEnabled = true;
    }

    public void SkipCountdown()
    {
        if (IsCountdownActive)
        {
            _countdownCts?.Cancel();
        }
    }

    public void StartSixFrequencyTest()
    {
        if (IsEmergencyStopped) return;

        _stimulusCts?.Cancel();
        _countdownCts?.Cancel();
        _stateMachine = new AudiometryStateMachine(new StandardThresholdProtocol());
        _stateMachine.StartSession(CurrentPatient, Ear.Left, bothEars: true, startingLevel: 30.0);
        _stateMachine.CurrentSession.HardwareProfileId = CurrentHardware.Fingerprint;
        _stateMachine.CurrentSession.CalibrationProfileId = _calibrationService.CurrentProfile?.HardwareFingerprint ?? "UNVERIFIED_RESEARCH";
        _stateMachine.CurrentSession.Purpose = IsCalibrated ? SessionPurpose.ClinicalDiagnostic : SessionPurpose.ResearchTracking;

        ThresholdResults.Clear();
        RecentEvents.Clear();
        CurrentViewName = "OperatorTest";
        IsCountdownActive = true;
        CountdownSeconds = 10;
        StimulusStatusText = $"⏳ 測驗即將開始，倒數 {CountdownSeconds} 秒... 請戴妥耳機並保持安靜。";
        BlindPressWarning = false;
        BlindPressWarningText = string.Empty;
        IsCompletionOverlayVisible = false;

        NotifyTestProperties();

        _countdownCts = new CancellationTokenSource();
        var token = _countdownCts.Token;

        _ = Task.Run(async () =>
        {
            try
            {
                for (int s = 10; s >= 1; s--)
                {
                    if (token.IsCancellationRequested || IsEmergencyStopped || _stateMachine.State != TestProgressState.Testing)
                        break;

                    await Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        CountdownSeconds = s;
                        StimulusStatusText = $"⏳ 測驗即將開始，倒數 {s} 秒... 請戴妥耳機並保持安靜。";
                        NotifyTestProperties();
                    });

                    await Task.Delay(1000, token);
                }
            }
            catch (OperationCanceledException)
            {
                // User pressed Space or clicked Skip to start immediately
            }

            if (IsEmergencyStopped || _stateMachine.State != TestProgressState.Testing)
            {
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    IsCountdownActive = false;
                    NotifyTestProperties();
                });
                return;
            }

            await Application.Current.Dispatcher.InvokeAsync(async () =>
            {
                IsCountdownActive = false;
                StimulusStatusText = "🎧 測驗正式開始！播放第 1 個測試音 (1000 Hz, 30 dB HL)... 聽到請按【空白鍵】！";
                NotifyTestProperties();

                if (AutoAdvance && !IsStimulusActive)
                {
                    await PresentNextStimulusAsync();
                }
            });
        });
    }

    public void StartQuickVerificationTest()
    {
        if (IsEmergencyStopped) return;

        UseSimulatedPatientAutoResponse = true;
        _simulatedPatient.SetLeftEarVerificationProfile();
        StartSixFrequencyTest();
    }

    public void OnSpaceOrResponseClicked()
    {
        if (IsEmergencyStopped) return;

        if (IsCountdownActive)
        {
            // 倒數期間按下反應鍵或空白鍵 -> 立即結束倒數開始測驗
            SkipCountdown();
            return;
        }

        if (IsStimulusActive && _responseTcs != null && !_responseTcs.Task.IsCompleted)
        {
            // 受測者於 3 秒聲音播放期間按下反應鍵 -> 有聽到！
            BlindPressWarning = false;
            BlindPressWarningText = string.Empty;
            _responseTcs.TrySetResult(true);
        }
        else
        {
            // 受測者於靜音期間盲目按下反應鍵 -> 警告並記錄假陽性
            BlindPressWarning = true;
            BlindPressWarningText = "⚠️ 盲目按鍵警示！目前為靜音期間，請勿猜測，等實際聽到聲音再按【空白鍵】(已記錄假陽性)";
            _stateMachine.RecordSpontaneousFalsePositive();

            var spEvt = _stateMachine.CurrentSession.StimulusEvents.LastOrDefault();
            if (spEvt != null)
            {
                Application.Current?.Dispatcher.Invoke(() =>
                {
                    RecentEvents.Insert(0, spEvt);
                    if (RecentEvents.Count > 30) RecentEvents.RemoveAt(RecentEvents.Count - 1);
                });
            }

            NotifyTestProperties();
        }
    }

    public void OnNotHeardClicked()
    {
        if (IsEmergencyStopped) return;

        if (IsStimulusActive && _responseTcs != null && !_responseTcs.Task.IsCompleted)
        {
            // 操作者手動標記沒聽到，直接結束 3 秒等待
            _responseTcs.TrySetResult(false);
        }
        else if (_stateMachine.State == TestProgressState.Testing && RecentEvents.Count > 0)
        {
            ProcessUserResponse(false);
        }
    }

    public async Task PresentNextStimulusAsync()
    {
        if (IsEmergencyStopped || _stateMachine.State != TestProgressState.Testing) return;

        _stimulusCts?.Cancel();
        _stimulusCts = new CancellationTokenSource();
        _responseTcs = new TaskCompletionSource<bool>();
        BlindPressWarning = false;
        BlindPressWarningText = string.Empty;

        var stimulus = _stateMachine.NextStimulus();
        NotifyTestProperties();

        IsStimulusActive = true;
        StimulusStatusText = "🔊 聲音播放中 (最多 3 秒)... 聽到請立即按下【空白鍵】！";

        if (IsMaskingEnabled)
        {
            Ear oppositeEar = stimulus.Ear == Ear.Left ? Ear.Right : Ear.Left;
            stimulus.MaskingEar = oppositeEar;
            stimulus.MaskingType = MaskingType.NarrowbandNoise;
            stimulus.MaskingLevel = MaskingLevelHl;

            double toneDbfs = stimulus.ToneLevelHl.HasValue
                ? _calibrationService.HlToDbfs(stimulus.ToneLevelHl.Value, stimulus.FrequencyHz, stimulus.Ear)
                : -100.0;
            double maskDbfs = _calibrationService.HlToDbfs(stimulus.MaskingLevel, stimulus.FrequencyHz, oppositeEar);

            const int soundDurationMs = 2000; // 實際發聲 2 秒，總反應視窗給予 3 秒
            _ = _audioOutput.PlayToneWithMaskingAsync(
                stimulus.Ear,
                stimulus.FrequencyHz,
                toneDbfs,
                oppositeEar,
                MaskingType.NarrowbandNoise,
                maskDbfs,
                stimulus.Waveform,
                soundDurationMs,
                _stimulusCts.Token);
        }
        else
        {
            double dbfs = stimulus.ToneLevelHl.HasValue
                ? _calibrationService.HlToDbfs(stimulus.ToneLevelHl.Value, stimulus.FrequencyHz, stimulus.Ear)
                : -100.0;

            const int soundDurationMs = 2000; // 實際發聲 2 秒，總反應視窗給予 3 秒
            _ = _audioOutput.PlayToneAsync(stimulus.Ear, stimulus.FrequencyHz, dbfs, stimulus.Waveform, soundDurationMs, _stimulusCts.Token);
        }

        _stimulusStopwatch.Restart();

        // 若啟用模擬受測者模式 (演算法自檢)
        if (UseSimulatedPatientAutoResponse)
        {
            _ = Task.Run(async () =>
            {
                var (simHeard, simLatency) = _simulatedPatient.EvaluateStimulus(stimulus);
                if (simHeard)
                {
                    int delay = Math.Clamp(simLatency, 150, 2500);
                    await Task.Delay(delay);
                    _responseTcs?.TrySetResult(true);
                }
            });
        }

        // 等待受測者按鍵反應 OR 3 秒播放結束
        var timeoutTask = Task.Delay(stimulus.DurationMs, _stimulusCts.Token);
        var finishedTask = await Task.WhenAny(_responseTcs.Task, timeoutTask);

        bool heard = false;
        int? latencyMs = null;

        if (finishedTask == _responseTcs.Task && await _responseTcs.Task)
        {
            // 受測者於 3 秒內按下空白鍵反應！
            _stimulusStopwatch.Stop();
            latencyMs = (int)_stimulusStopwatch.ElapsedMilliseconds;
            heard = true;

            // 即刻中斷發聲（受測者確認聽到後不需等待剩餘時間）
            _stimulusCts.Cancel();
            _audioOutput.Stop();
        }
        else
        {
            // 3 秒結束無反應 -> 未聽到
            _stimulusStopwatch.Stop();
            heard = false;
            latencyMs = null;
            _audioOutput.Stop();
        }

        IsStimulusActive = false;
        ProcessStimulusResult(stimulus, heard, latencyMs);
    }

    public void ProcessUserResponse(bool responded)
    {
        if (_stateMachine.State != TestProgressState.Testing || RecentEvents.Count == 0) return;
        var last = RecentEvents[0];
        ProcessStimulusResult(last, responded, latencyMs: 350);
    }

    private void ProcessStimulusResult(StimulusEvent stimulus, bool heard, int? latencyMs)
    {
        bool thresholdReached = _stateMachine.RecordResponse(stimulus, heard, latencyMs);

        void UpdateUiState()
        {
            RecentEvents.Insert(0, stimulus);
            if (RecentEvents.Count > 30) RecentEvents.RemoveAt(RecentEvents.Count - 1);

            ThresholdResults.Clear();
            foreach (var r in _stateMachine.CurrentSession.ThresholdResults)
            {
                ThresholdResults.Add(r);
            }

            IsStimulusActive = false;
            if (_stateMachine.State == TestProgressState.Completed)
            {
                StimulusStatusText = "🎉 測驗已全數完成！聽閾結果已自動保存。";
                IsCompletionOverlayVisible = true;
                try
                {
                    System.Media.SystemSounds.Asterisk.Play();
                }
                catch
                {
                }
                _ = SaveCurrentSessionAsync();
            }
            else if (thresholdReached)
            {
                StimulusStatusText = $"✓ 已確立 {stimulus.FrequencyHz} Hz 聽閾！繼續下一頻率。";
            }
            else
            {
                StimulusStatusText = heard 
                    ? $"✓ 受測者已聽到 (反應延遲: {latencyMs} ms) -> 降 10 dB" 
                    : "✕ 3 秒內未反應 (未聽到) -> 升 5 dB";
            }

            NotifyTestProperties();
        }

        if (Application.Current?.Dispatcher != null && !Application.Current.Dispatcher.CheckAccess())
        {
            Application.Current.Dispatcher.Invoke(UpdateUiState);
        }
        else
        {
            UpdateUiState();
        }

        // 若啟用連續自動推進，且測驗仍在進行中
        if (AutoAdvance && _stateMachine.State == TestProgressState.Testing && !IsEmergencyStopped)
        {
            _ = AutoAdvanceToNextStimulusAsync();
        }
    }

    private async Task AutoAdvanceToNextStimulusAsync()
    {
        // 隨機間隔 1.0 ~ 3.0 秒空白停留靜音期，避免受測者抓節奏盲猜
        int jitterPauseMs = _random.Next(1000, 3000);
        StimulusStatusText += $" (間隔 {jitterPauseMs / 1000.0:0.1} 秒後播放下一音)";
        await Task.Delay(jitterPauseMs);

        if (_stateMachine.State == TestProgressState.Testing && !IsEmergencyStopped && !IsStimulusActive)
        {
            await PresentNextStimulusAsync();
        }
    }

    public void TriggerEmergencyStop(string reason)
    {
        _countdownCts?.Cancel();
        IsCountdownActive = false;
        IsEmergencyStopped = true;
        EmergencyStopMessage = $"【緊急停止已觸發】{reason}。音訊輸出已即時歸零強制靜音。";
        _audioOutput.MuteImmediate();
        _stateMachine.Abort(reason);
        StimulusStatusText = "緊急停止已鎖定";
        NotifyTestProperties();
    }

    public void ResetEmergencyStop()
    {
        IsEmergencyStopped = false;
        EmergencyStopMessage = string.Empty;
        StimulusStatusText = "緊急停止已重設。請重新開始測驗。";
        NotifyTestProperties();
    }

    public void TogglePauseResume()
    {
        if (_stateMachine.State == TestProgressState.Testing)
        {
            _stateMachine.Pause();
            StimulusStatusText = "測驗已暫停";
        }
        else if (_stateMachine.State == TestProgressState.Paused)
        {
            _stateMachine.Resume();
            StimulusStatusText = "測驗已恢復，即將播放下一音...";
            if (AutoAdvance && !IsStimulusActive)
            {
                _ = AutoAdvanceToNextStimulusAsync();
            }
        }
        NotifyTestProperties();
    }

    public void RollbackStep()
    {
        if (_stateMachine.RollbackLastStep("Operator"))
        {
            if (RecentEvents.Count > 0) RecentEvents.RemoveAt(0);
            StimulusStatusText = "已回退上一步刺激";
            NotifyTestProperties();
        }
    }

    private async Task SaveCurrentSessionAsync()
    {
        try
        {
            await _repository.SavePatientAsync(CurrentPatient);
            await _repository.SaveSessionAsync(_stateMachine.CurrentSession);
            foreach (var log in _stateMachine.AuditLogs)
            {
                await _repository.SaveAuditLogAsync(log);
            }
            await LoadHistoryAsync();
        }
        catch (Exception ex)
        {
            StimulusStatusText = $"存檔錯誤: {ex.Message}";
        }
    }

    public async Task LoadHistoryAsync()
    {
        try
        {
            var sessions = await _repository.GetAllSessionsAsync();
            Application.Current?.Dispatcher.Invoke(() =>
            {
                PastSessions.Clear();
                foreach (var s in sessions) PastSessions.Add(s);
            });
        }
        catch
        {
        }
    }

    private async Task ExportCurrentSessionJsonAsync()
    {
        string dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "HearingTest_Reports");
        string path = Path.Combine(dir, $"HearingReport_{_stateMachine.CurrentSession.Id[..8]}.json");
        await _reportExporter.ExportJsonAsync(_stateMachine.CurrentSession, CurrentPatient, path, anonymized: false);
        MessageBox.Show($"JSON 報告已成功匯出至：\n{path}", "匯出成功", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private async Task ExportCurrentSessionCsvAsync()
    {
        string dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "HearingTest_Reports");
        string path = Path.Combine(dir, $"HearingReport_{_stateMachine.CurrentSession.Id[..8]}.csv");
        await _reportExporter.ExportCsvAsync(_stateMachine.CurrentSession, CurrentPatient, path, anonymized: false);
        MessageBox.Show($"CSV 報告已成功匯出至：\n{path}", "匯出成功", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private async Task AutoLoadDefaultCalibrationAsync()
    {
        string defaultFile = _calibrationProfileManager.GetDefaultProfilePath();
        if (File.Exists(defaultFile))
        {
            var (p, ok, _) = await _calibrationService.LoadProfileFromFileAsync(defaultFile, CurrentHardware);
            if (ok && p != null)
            {
                UpdateCalibrationUI(p);
            }
        }
    }

    private async Task ImportCalibrationFileAsync()
    {
        var dlg = new Microsoft.Win32.OpenFileDialog
        {
            Filter = "校正檔 (*.json)|*.json|所有檔案 (*.*)|*.*",
            Title = "選取校正檔 (.json)"
        };
        if (dlg.ShowDialog() == true)
        {
            var (profile, success, reason) = await _calibrationService.LoadProfileFromFileAsync(dlg.FileName, CurrentHardware);
            if (success && profile != null)
            {
                await _calibrationService.SaveProfileToFileAsync(profile, _calibrationProfileManager.GetDefaultProfilePath());
                UpdateCalibrationUI(profile);
                MessageBox.Show($"校正檔載入成功！\n{reason}", "校正驗證成功", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                UpdateCalibrationUI(null);
                MessageBox.Show($"校正檔無效或驗證失敗：\n{reason}\n\n系統已強制保持在【未校正研究模式】。", "校正驗證拒絕", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
    }

    private async Task GenerateSampleCalibrationAsync()
    {
        var sample = _calibrationService.CreateSampleProfile(CurrentHardware);
        string dest = _calibrationProfileManager.GetDefaultProfilePath();
        await _calibrationService.SaveProfileToFileAsync(sample, dest);
        var (loaded, success, reason) = await _calibrationService.LoadProfileFromFileAsync(dest, CurrentHardware);
        if (success && loaded != null)
        {
            UpdateCalibrationUI(loaded);
            MessageBox.Show($"已成功為當前硬體指紋生成並簽署專屬校正檔！\n儲存路徑：{dest}\n\n目前系統已切換至【已校正模式 (臨床參考)】。", "產生專屬校正檔成功", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    private void UnloadCalibration()
    {
        _calibrationService.SetProfile(null);
        UpdateCalibrationUI(null);
        string defaultFile = _calibrationProfileManager.GetDefaultProfilePath();
        if (File.Exists(defaultFile)) File.Delete(defaultFile);
        MessageBox.Show("已重設校正狀態，系統恢復為【未校正研究模式】。", "校正已重設", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void UpdateCalibrationUI(CalibrationProfile? p)
    {
        Application.Current?.Dispatcher.Invoke(() =>
        {
            CalibrationPoints.Clear();
            if (p != null)
            {
                foreach (var pt in p.CalibrationPoints.OrderBy(x => x.FrequencyHz).ThenBy(x => x.Ear))
                {
                    CalibrationPoints.Add(pt);
                }
            }
            OnPropertyChanged(nameof(IsCalibrated));
            OnPropertyChanged(nameof(CalibrationBadgeText));
            OnPropertyChanged(nameof(CalibrationBadgeBrush));
            OnPropertyChanged(nameof(CalibrationLabInfo));
            OnPropertyChanged(nameof(CalibrationExpiryInfo));
            OnPropertyChanged(nameof(CalibrationFingerprintDisplay));
            OnPropertyChanged(nameof(CalibrationStatusDescription));
        });
    }

    public async Task PlayCalibrationToneAsync()
    {
        if (IsCalibratorTonePlaying)
        {
            StopCalibrationTone();
            return;
        }

        try
        {
            _calibratorCts?.Cancel();
            _calibratorCts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
            IsCalibratorTonePlaying = true;
            Ear ear = CalibratorSelectedEarIndex == 0 ? Ear.Left : Ear.Right;
            string earName = ear == Ear.Left ? "左耳 (Left)" : "右耳 (Right)";
            CalibratorStatusText = $"🔊 正在為【{earName}】播放 1000 Hz (-20 dBFS) 參考音... 請將耳機密貼手機麥克風觀察讀數 (15秒後自動停止或按停止)";

            await _audioOutput.PlayToneAsync(
                ear,
                1000,
                -20.0,
                WaveformType.Continuous,
                15000,
                _calibratorCts.Token);

            IsCalibratorTonePlaying = false;
            CalibratorStatusText = "校正音播放結束。請在下方欄位輸入手機實測數值 (dB SPL)，然後點擊【計算並套用校準檔】。";
        }
        catch (OperationCanceledException)
        {
            IsCalibratorTonePlaying = false;
            CalibratorStatusText = "校正音已停止發聲。";
        }
        catch (Exception ex)
        {
            IsCalibratorTonePlaying = false;
            CalibratorStatusText = $"播放失敗: {ex.Message}";
        }
    }

    public void StopCalibrationTone()
    {
        try
        {
            _calibratorCts?.Cancel();
            _audioOutput.Stop();
        }
        catch
        {
        }
        finally
        {
            IsCalibratorTonePlaying = false;
            CalibratorStatusText = "校正音已由使用者手動停止。請輸入實測數值。";
        }
    }

    public async Task ApplyPhoneCalibrationAsync()
    {
        try
        {
            StopCalibrationTone();

            var profile = _calibrationService.CreatePhoneCalibrationProfile(
                CurrentHardware,
                CalibratorMeasuredSplLeft,
                CalibratorMeasuredSplRight,
                -20.0);

            string dest = _calibrationProfileManager.GetDefaultProfilePath();
            await _calibrationService.SaveProfileToFileAsync(profile, dest);

            var (loaded, success, reason) = await _calibrationService.LoadProfileFromFileAsync(dest, CurrentHardware);
            if (success && loaded != null)
            {
                UpdateCalibrationUI(loaded);
                CalibratorStatusText = $"✅ 校正成功！已依手機實測值 (左: {CalibratorMeasuredSplLeft:F1} dB, 右: {CalibratorMeasuredSplRight:F1} dB) 建立並套用專屬校正檔。";
                MessageBox.Show(
                    $"手機分貝計校準成功！\n\n" +
                    $"• 左耳 1000 Hz 實測基準：{CalibratorMeasuredSplLeft:F1} dB SPL\n" +
                    $"• 右耳 1000 Hz 實測基準：{CalibratorMeasuredSplRight:F1} dB SPL\n" +
                    $"• 基準數位振幅：-20.0 dBFS\n" +
                    $"• 已根據 ISO 389-1 自動計算各頻率 RETSPL 補償曲線。\n\n" +
                    $"系統已全面啟用【已校正模式 (手機分貝計基準)】！",
                    "手機分貝計校準成功",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            else
            {
                UpdateCalibrationUI(null);
                CalibratorStatusText = $"❌ 校正失敗：{reason}";
                MessageBox.Show($"套用校正檔失敗：\n{reason}", "校正失敗", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
        catch (Exception ex)
        {
            CalibratorStatusText = $"❌ 發生錯誤：{ex.Message}";
            MessageBox.Show($"校正過程發生錯誤：\n{ex.Message}", "錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void NotifyTestProperties()
    {
        void DoNotify()
        {
            if (_stateMachine.State == TestProgressState.Testing)
            {
                UpdateMaskingRecommendation();
            }

            OnPropertyChanged(nameof(CurrentEar));
            OnPropertyChanged(nameof(CurrentEarText));
            OnPropertyChanged(nameof(CurrentEarBrush));
            OnPropertyChanged(nameof(CurrentFrequencyHz));
            OnPropertyChanged(nameof(CurrentLevelHl));
            OnPropertyChanged(nameof(CurrentLevelDisplay));
            OnPropertyChanged(nameof(ReliabilityText));
            OnPropertyChanged(nameof(TestState));
            OnPropertyChanged(nameof(IsCalibrated));
            OnPropertyChanged(nameof(MaskingRecommendationText));
            OnPropertyChanged(nameof(MaskingStatusDescription));
            OnPropertyChanged(nameof(BlindPressWarning));
            OnPropertyChanged(nameof(BlindPressWarningText));
            OnPropertyChanged(nameof(AutoAdvance));
            OnPropertyChanged(nameof(UseSimulatedPatientAutoResponse));
            OnPropertyChanged(nameof(IsTestCompleted));
            OnPropertyChanged(nameof(IsTestActive));
            OnPropertyChanged(nameof(IsCompletionOverlayVisible));
            OnPropertyChanged(nameof(LeftEarPtaDisplay));
            OnPropertyChanged(nameof(LeftEarGradeDisplay));
            OnPropertyChanged(nameof(RightEarPtaDisplay));
            OnPropertyChanged(nameof(RightEarGradeDisplay));
            OnPropertyChanged(nameof(OverallReliabilityDisplay));
            OnPropertyChanged(nameof(IsCountdownActive));
            OnPropertyChanged(nameof(CountdownSeconds));
            OnPropertyChanged(nameof(CountdownText));
            OnPropertyChanged(nameof(IsPatientTestingActive));
        }

        if (Application.Current?.Dispatcher != null && !Application.Current.Dispatcher.CheckAccess())
        {
            Application.Current.Dispatcher.Invoke(DoNotify);
        }
        else
        {
            DoNotify();
        }
    }
}
