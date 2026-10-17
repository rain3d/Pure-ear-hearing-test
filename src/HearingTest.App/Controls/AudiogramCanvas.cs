using System.Globalization;
using System.Windows;
using System.Windows.Media;
using HearingTest.App.Localization;
using HearingTest.Domain;
using HearingTest.Domain.Models;

namespace HearingTest.App.Controls;

public class AudiogramCanvas : FrameworkElement
{
    public AudiogramCanvas()
    {
        LocalizationService.LanguageChanged += () => Dispatcher.Invoke(InvalidateVisual);
    }
    public static readonly DependencyProperty ThresholdResultsProperty =
        DependencyProperty.Register(
            nameof(ThresholdResults),
            typeof(IReadOnlyList<ThresholdResult>),
            typeof(AudiogramCanvas),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty ActiveFrequencyHzProperty =
        DependencyProperty.Register(
            nameof(ActiveFrequencyHz),
            typeof(int?),
            typeof(AudiogramCanvas),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty ActiveEarProperty =
        DependencyProperty.Register(
            nameof(ActiveEar),
            typeof(Ear?),
            typeof(AudiogramCanvas),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty IsCalibratedProperty =
        DependencyProperty.Register(
            nameof(IsCalibrated),
            typeof(bool),
            typeof(AudiogramCanvas),
            new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.AffectsRender));

    public IReadOnlyList<ThresholdResult>? ThresholdResults
    {
        get => (IReadOnlyList<ThresholdResult>?)GetValue(ThresholdResultsProperty);
        set => SetValue(ThresholdResultsProperty, value);
    }

    public int? ActiveFrequencyHz
    {
        get => (int?)GetValue(ActiveFrequencyHzProperty);
        set => SetValue(ActiveFrequencyHzProperty, value);
    }

    public Ear? ActiveEar
    {
        get => (Ear?)GetValue(ActiveEarProperty);
        set => SetValue(ActiveEarProperty, value);
    }

    public bool IsCalibrated
    {
        get => (bool)GetValue(IsCalibratedProperty);
        set => SetValue(IsCalibratedProperty, value);
    }

    private static readonly int[] Frequencies = [250, 500, 1000, 2000, 4000, 8000];
    private static readonly int[] DbLevels = [-10, 0, 10, 20, 30, 40, 50, 60, 70, 80, 90, 100, 110, 120];

    protected override void OnRender(DrawingContext dc)
    {
        base.OnRender(dc);

        double w = ActualWidth;
        double h = ActualHeight;
        if (w < 100 || h < 100) return;

        double padL = 50.0;
        double padR = 30.0;
        double padT = 35.0;
        double padB = 40.0;

        double plotW = w - padL - padR;
        double plotH = h - padT - padB;

        // Background
        var bgBrush = new SolidColorBrush(Color.FromRgb(15, 23, 42)); // #0F172A
        dc.DrawRectangle(bgBrush, null, new Rect(0, 0, w, h));

        // Normal hearing shaded band (0 to 25 dB HL)
        double yBandTop = padT + (plotH * (10.0 / 130.0)); // -10 to 0 is 10 units
        double yBandBottom = padT + (plotH * (35.0 / 130.0)); // -10 to 25 is 35 units
        var normalBandBrush = new SolidColorBrush(Color.FromArgb(20, 16, 185, 129));
        dc.DrawRectangle(normalBandBrush, null, new Rect(padL, yBandTop, plotW, yBandBottom - yBandTop));

        // Grid pens
        var gridPen = new Pen(new SolidColorBrush(Color.FromRgb(30, 41, 59)), 1.0);
        gridPen.Freeze();
        var zeroPen = new Pen(new SolidColorBrush(Color.FromRgb(71, 85, 105)), 1.5);
        zeroPen.Freeze();

        var typeface = new Typeface(new FontFamily("Segoe UI"), FontStyles.Normal, FontWeights.Normal, FontStretches.Normal);
        var boldTypeface = new Typeface(new FontFamily("Segoe UI"), FontStyles.Normal, FontWeights.SemiBold, FontStretches.Normal);
        var labelBrush = new SolidColorBrush(Color.FromRgb(148, 163, 184));
        labelBrush.Freeze();

        // Draw horizontal dB lines
        for (int i = 0; i < DbLevels.Length; i++)
        {
            int db = DbLevels[i];
            double y = padT + (plotH * (i / (double)(DbLevels.Length - 1)));
            var pen = (db == 0) ? zeroPen : gridPen;
            dc.DrawLine(pen, new Point(padL, y), new Point(padL + plotW, y));

            var ft = new FormattedText(
                $"{db}",
                CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight,
                typeface,
                11,
                labelBrush,
                VisualTreeHelper.GetDpi(this).PixelsPerDip);

            dc.DrawText(ft, new Point(padL - ft.Width - 8, y - ft.Height / 2));
        }

        // Draw vertical frequency lines (Log scale: octave intervals are equally spaced)
        double[] freqX = new double[Frequencies.Length];
        for (int i = 0; i < Frequencies.Length; i++)
        {
            int f = Frequencies[i];
            double x = padL + (plotW * (i / (double)(Frequencies.Length - 1)));
            freqX[i] = x;

            bool isActive = ActiveFrequencyHz == f;
            var pen = isActive ? new Pen(new SolidColorBrush(Color.FromRgb(14, 165, 233)), 2.0) : gridPen;
            dc.DrawLine(pen, new Point(x, padT), new Point(x, padT + plotH));

            string label = f >= 1000 ? $"{f / 1000}k" : $"{f}";
            var ft = new FormattedText(
                label,
                CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight,
                isActive ? boldTypeface : typeface,
                11,
                isActive ? new SolidColorBrush(Color.FromRgb(56, 189, 248)) : labelBrush,
                VisualTreeHelper.GetDpi(this).PixelsPerDip);

            dc.DrawText(ft, new Point(x - ft.Width / 2, padT + plotH + 8));
        }

        // Title / Header: dB HL / Frequency (Hz)
        var titleFt = new FormattedText(
            LocalizationService.GetString("Str_AudiogramFreqAxis", "頻率 Frequency (Hz)"),
            CultureInfo.InvariantCulture,
            FlowDirection.LeftToRight,
            boldTypeface,
            12,
            labelBrush,
            VisualTreeHelper.GetDpi(this).PixelsPerDip);
        dc.DrawText(titleFt, new Point(padL + plotW / 2 - titleFt.Width / 2, 8));

        string yLabelText = IsCalibrated
            ? LocalizationService.GetString("Str_AudiogramDbHlAxis", "聽力級 (dB HL)")
            : LocalizationService.GetString("Str_AudiogramDbRelAxis", "相對強度 (dBFS/Rel)");

        var yLabelFt = new FormattedText(
            yLabelText,
            CultureInfo.InvariantCulture,
            FlowDirection.LeftToRight,
            boldTypeface,
            11,
            labelBrush,
            VisualTreeHelper.GetDpi(this).PixelsPerDip);
        dc.DrawText(yLabelFt, new Point(padL, padT - 22));

        // Watermark if uncalibrated
        if (!IsCalibrated)
        {
            var wmFt = new FormattedText(
                LocalizationService.GetString("Str_AudiogramWatermark", "非醫療診斷 / 未校正研究模式"),
                CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight,
                boldTypeface,
                24,
                new SolidColorBrush(Color.FromArgb(35, 248, 250, 252)),
                VisualTreeHelper.GetDpi(this).PixelsPerDip);
            dc.DrawText(wmFt, new Point(padL + (plotW - wmFt.Width) / 2, padT + (plotH - wmFt.Height) / 2));
        }

        // Plot Results: Left Ear (Blue X) and Right Ear (Red O)
        if (ThresholdResults != null && ThresholdResults.Count > 0)
        {
            DrawEarCurve(dc, Ear.Left, ThresholdResults, freqX, padT, plotH);
            DrawEarCurve(dc, Ear.Right, ThresholdResults, freqX, padT, plotH);
        }
    }

    private void DrawEarCurve(
        DrawingContext dc,
        Ear ear,
        IReadOnlyList<ThresholdResult> results,
        double[] freqX,
        double padT,
        double plotH)
    {
        var earResults = results
            .Where(r => r.Ear == ear && r.ThresholdHl.HasValue)
            .OrderBy(r => r.FrequencyHz)
            .ToList();

        if (earResults.Count == 0) return;

        Color color = ear == Ear.Left ? Color.FromRgb(59, 130, 246) : Color.FromRgb(239, 68, 68);
        var pen = new Pen(new SolidColorBrush(color), 2.0);
        pen.Freeze();

        Point? prevPt = null;
        var points = new List<(Point pt, bool noResponse)>();

        foreach (var r in earResults)
        {
            int fIdx = Array.IndexOf(Frequencies, r.FrequencyHz);
            if (fIdx < 0) continue;

            double x = freqX[fIdx];
            double hl = r.ThresholdHl!.Value;
            // Map -10 dB HL (top) to 120 dB HL (bottom)
            double normY = (hl - (-10.0)) / (120.0 - (-10.0));
            double y = padT + (plotH * Math.Clamp(normY, 0.0, 1.0));

            var pt = new Point(x, y);
            points.Add((pt, r.NoResponse));

            if (prevPt.HasValue)
            {
                // Connect with line
                dc.DrawLine(pen, prevPt.Value, pt);
            }
            prevPt = pt;
        }

        // Draw Markers
        double s = 7.0; // Marker radius
        foreach (var (pt, noResponse) in points)
        {
            if (ear == Ear.Left)
            {
                // Blue 'X'
                dc.DrawLine(pen, new Point(pt.X - s, pt.Y - s), new Point(pt.X + s, pt.Y + s));
                dc.DrawLine(pen, new Point(pt.X - s, pt.Y + s), new Point(pt.X + s, pt.Y - s));
            }
            else
            {
                // Red 'O'
                dc.DrawEllipse(null, pen, pt, s, s);
            }

            // If NoResponse (NR), draw downward-pointing 45° arrow
            if (noResponse)
            {
                var arrowPen = new Pen(new SolidColorBrush(color), 1.5);
                double arrowDx = ear == Ear.Left ? -12 : 12;
                double arrowDy = 12;
                Point arrowEnd = new Point(pt.X + arrowDx, pt.Y + arrowDy);
                dc.DrawLine(arrowPen, pt, arrowEnd);
                // Arrow tip
                dc.DrawLine(arrowPen, arrowEnd, new Point(arrowEnd.X - (ear == Ear.Left ? -4 : 4), arrowEnd.Y - 5));
                dc.DrawLine(arrowPen, arrowEnd, new Point(arrowEnd.X - 5, arrowEnd.Y - 1));
            }
        }
    }
}
