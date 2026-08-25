using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Threading.Tasks;
using LiveChartsCore;
using LiveChartsCore.Drawing;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
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
    private const int MaxRenderedPoints = 1500;

    private static readonly SKColor DirectColor = new(54, 116, 181);
    private static readonly SKColor InversedColor = new(196, 108, 44);

    private readonly ListenerSettingsViewModel listener;
    private List<(DateTime? Time, double? Direct, double? Inversed)> currentSlots = [];
    private int[] currentSlotMap = [];
    private int rebuildGeneration;

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

    private async void Rebuild()
    {
        var generation = ++rebuildGeneration;
        var rangeIndex = Math.Max(0, Math.Min(RangeDays.Length - 1, RangeComboBox.SelectedIndex));
        var cutoff = DateTime.Now - TimeSpan.FromDays(RangeDays[rangeIndex]);
        var listenerId = listener.Id;
        var direction = listener.Direction;

        Cursor = Cursors.Wait;
        List<(DateTime Time, double Rate, double Inversed)> points;
        try
        {
            var stored = await Task.Run(() => ConfigHelper.GetStoredRates(listenerId, direction, cutoff));
            points = stored
                .Select(ParseStoredRate)
                .Where(p => p.HasValue)
                .Select(p => p.Value)
                .Where(p => p.Time >= cutoff)
                .OrderBy(p => p.Time)
                .ToList();
        }
        finally
        {
            Cursor = Cursors.Arrow;
        }

        if (generation != rebuildGeneration || !IsLoaded)
            return;

        if (points.Count == 0)
        {
            currentSlots = [];
            currentSlotMap = [];
            Chart.Series =
            [
                new LineSeries<double> { Values = [0], Name = "No data" }
            ];
            return;
        }

        var slots = new List<(DateTime? Time, double? Direct, double? Inversed)>();
        foreach (var point in points)
        {
            if (slots.Count > 0 &&
                point.Time - slots[^1].Time!.Value > MaxGaps[rangeIndex])
                slots.Add((null, null, null));
            slots.Add((point.Time, point.Rate, point.Inversed));
        }
        currentSlots = slots;

        BuildChart(slots, rangeIndex);
    }

    private void BuildChart(
        List<(DateTime? Time, double? Direct, double? Inversed)> slots, int rangeIndex)
    {
        var nonNullSlotIndices = new List<int>();
        var directValues = new List<double>();
        for (var i = 0; i < slots.Count; i++)
        {
            if (slots[i].Direct == null)
                continue;
            nonNullSlotIndices.Add(i);
            directValues.Add(slots[i].Direct.Value);
        }

        var pickedSlotIndices = directValues.Count == 0
            ? Enumerable.Range(0, slots.Count).ToList()
            : LttbDownsampler.PickIndices(directValues, MaxRenderedPoints)
                .Select(p => nonNullSlotIndices[p])
                .ToList();

        var direct = new List<double?>();
        var inversed = new List<double?>();
        var slotMap = new List<int>();
        int? previousPicked = null;
        foreach (var slotIndex in pickedSlotIndices)
        {
            if (previousPicked != null &&
                slots[slotIndex].Time!.Value - slots[previousPicked.Value].Time!.Value > MaxGaps[rangeIndex])
            {
                direct.Add(null);
                inversed.Add(null);
                slotMap.Add(-1);
            }
            direct.Add(slots[slotIndex].Direct);
            inversed.Add(slots[slotIndex].Inversed);
            slotMap.Add(slotIndex);
            previousPicked = slotIndex;
        }
        currentSlotMap = [.. slotMap];

        var series = new List<ISeries>
        {
            BuildSeries([.. direct], "Direct", DirectColor)
        };
        if (InversedCheckBox.IsChecked == true)
            series.Add(BuildSeries([.. inversed], "Inversed", InversedColor));

        Chart.Series = series;
        Chart.XAxes = [BuildXAxis(slotMap, slots, rangeIndex)];
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
            GeometrySize = 0,
            Stroke = new SolidColorPaint(color),
            Fill = null
        };

    private static Axis BuildXAxis(
        List<int> slotMap,
        List<(DateTime? Time, double? Direct, double? Inversed)> slots,
        int rangeIndex)
    {
        var labelFormat = rangeIndex == 0 ? "HH:mm" : "d.MM";
        var step = Math.Max(1, slotMap.Count / 8);

        return new Axis
        {
            Labeler = value =>
            {
                var index = (int)value;
                if (index < 0 || index >= slotMap.Count || index % step != 0)
                    return string.Empty;
                var slotIndex = slotMap[index];
                return slotIndex < 0
                    ? string.Empty
                    : slots[slotIndex].Time?.ToString(labelFormat, CultureInfo.InvariantCulture) ?? string.Empty;
            },
            LabelsRotation = rangeIndex == 0 ? 0 : 45,
            UnitWidth = 1,
            MinStep = 1
        };
    }

    private void Chart_OnMouseMove(object sender, MouseEventArgs e)
    {
        var slotMap = currentSlotMap;
        var slots = currentSlots;
        if (slotMap.Length == 0 || slots.Count == 0 || !Chart.IsLoaded)
        {
            PointPopup.IsOpen = false;
            return;
        }

        var pixel = e.GetPosition(Chart);
        var dataPoint = Chart.ScalePixelsToData(new LvcPointD(pixel.X, pixel.Y));
        var index = (int)Math.Round(dataPoint.X);
        if (index < 0 || index >= slotMap.Length || slotMap[index] < 0)
        {
            PointPopup.IsOpen = false;
            return;
        }

        var slot = slots[slotMap[index]];
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

    private void Chart_OnMouseLeave(object sender, MouseEventArgs e) =>
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
