using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using LiveChartsCore;
using LiveChartsCore.Drawing;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using RateListener.ExtensionMethods;
using RateListener.Helpers;
using RateListener.Models;
using RateListener.ViewModels;
using SkiaSharp;

namespace RateListener;

public partial class RateHistoryWindow : Window
{
    private static readonly TimeSpan[] MaxGaps =
    [
        TimeSpan.FromHours(1),
        TimeSpan.FromHours(6),
        TimeSpan.FromDays(1),
        TimeSpan.FromDays(7)
    ];

    private static readonly int[] RangeDays = [1, 7, 30, 365];
    private static readonly string[] RangeNames = ["Day", "Week", "Month", "Year"];
    private const string StoredTimeFormat = "dd.MM.yyyy HH:mm:ss";

    private readonly ListenerSettingsViewModel listener;
    private List<(DateTime? Time, double? Direct, double? Inversed)> currentSlots = [];
    private int currentRangeIndex;

    public RateHistoryWindow(ListenerSettingsViewModel listener)
    {
        this.listener = listener;
        DataContext = listener;
        InitializeComponent();
        RangeComboBox.ItemsSource = RangeNames;
        RangeComboBox.SelectedIndex = Math.Min(1, RangeNames.Length - 1);
        Loaded += (_, _) => Rebuild();
    }

    private void RangeComboBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e) =>
        Rebuild();

    private void InversedCheckBox_OnChanged(object sender, RoutedEventArgs e) =>
        Rebuild();

    private void Rebuild()
    {
        var rangeIndex = Math.Max(0, Math.Min(RangeDays.Length - 1, RangeComboBox.SelectedIndex));
        currentRangeIndex = rangeIndex;
        var cutoff = DateTime.Now - TimeSpan.FromDays(RangeDays[rangeIndex]);
        var points = ConfigHelper.GetStoredRates(listener.Id, listener.Direction, cutoff)
            .Select(ParseStoredRate)
            .Where(p => p.HasValue)
            .Select(p => p.Value)
            .Where(p => p.Time >= cutoff)
            .OrderBy(p => p.Time)
            .ToList();

        if (points.Count == 0)
        {
            currentSlots = [];
            Chart.Series =
            [
                new LineSeries<double> { Values = [0], Name = "No data" }
            ];
            return;
        }

        var maxGap = MaxGaps[rangeIndex];
        var slots = new List<(DateTime? Time, double? Direct, double? Inversed)>();
        foreach (var point in points)
        {
            if (slots.Count > 0 &&
                point.Time - slots[^1].Time!.Value > maxGap)
                slots.Add((null, null, null));
            slots.Add((point.Time, point.Rate, point.Inversed));
        }
        currentSlots = slots;

        var showInversed = InversedCheckBox.IsChecked == true;
        var series = new List<ISeries>
        {
            BuildSeries(slots.Select(s => s.Direct).ToArray(), "Direct",
                new SKColor(54, 116, 181))
        };
        if (showInversed)
            series.Add(BuildSeries(slots.Select(s => s.Inversed).ToArray(), "Inversed",
                new SKColor(196, 108, 44)));

        Chart.Series = series;
        Chart.XAxes = [BuildXAxis(slots, rangeIndex)];
        Chart.YAxes =
        [
            new Axis { Labeler = value => value.ToString("0.####") }
        ];
    }

    private static LineSeries<double?> BuildSeries(double?[] values, string name, SKColor color) =>
        new()
        {
            Values = values,
            Name = name,
            LineSmoothness = 0,
            GeometrySize = 6,
            Stroke = new SolidColorPaint(color),
            GeometryStroke = new SolidColorPaint(color),
            Fill = null
        };

    private static Axis BuildXAxis(List<(DateTime? Time, double? Direct, double? Inversed)> slots, int rangeIndex)
    {
        var labelFormat = rangeIndex == 0 ? "HH:mm" : "d.MM";
        var labels = slots.Select(slot =>
                slot.Time?.ToString(labelFormat, CultureInfo.InvariantCulture) ?? string.Empty)
            .ToList();
        var step = Math.Max(1, slots.Count / 8);

        return new Axis
        {
            Labeler = value =>
            {
                var index = (int)value;
                return index >= 0 && index < labels.Count && index % step == 0
                    ? labels[index]
                    : string.Empty;
            },
            LabelsRotation = rangeIndex == 0 ? 0 : 45,
            UnitWidth = 1,
            MinStep = 1
        };
    }

    private void Chart_OnMouseMove(object sender, System.Windows.Input.MouseEventArgs e)
    {
        var slots = currentSlots;
        if (slots.Count == 0 || !Chart.IsLoaded)
        {
            PointPopup.IsOpen = false;
            return;
        }

        var pixel = e.GetPosition(Chart);
        var dataPoint = Chart.ScalePixelsToData(new LvcPointD(pixel.X, pixel.Y));
        var index = (int)Math.Round(dataPoint.X);
        if (index < 0 || index >= slots.Count || slots[index].Time == null)
        {
            PointPopup.IsOpen = false;
            return;
        }

        var slot = slots[index];
        var text = $"{slot.Time:dd.MM.yyyy HH:mm:ss}";
        if (slot.Direct != null)
            text += $"\nDirect: {slot.Direct:0.#####}";
        if (slot.Inversed != null)
            text += $"\nInversed: {slot.Inversed:0.#####}";
        PointPopupText.Text = text;
        PointPopup.HorizontalOffset = pixel.X + 14;
        PointPopup.VerticalOffset = pixel.Y - 10;
        PointPopup.IsOpen = true;
    }

    private void Chart_OnMouseLeave(object sender, System.Windows.Input.MouseEventArgs e) =>
        PointPopup.IsOpen = false;

    private static (DateTime Time, double Rate, double Inversed)? ParseStoredRate(StoredRate stored)
    {
        if (!DateTime.TryParseExact(stored.Time, StoredTimeFormat,
                CultureInfo.InvariantCulture, DateTimeStyles.None, out var time))
            return null;
        if (!TryParseNumber(stored.Rate, out var rate))
            return null;
        TryParseNumber(stored.InversedRate, out var inversed);
        return (time, rate, inversed);
    }

    private static bool TryParseNumber(string source, out double value) =>
        double.TryParse(source?
                .Replace(" ", string.Empty)
                .Replace("\u00A0", string.Empty)
                .Replace(',', '.'),
            NumberStyles.Any, CultureInfo.InvariantCulture, out value) && value > 0;
}
