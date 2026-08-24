using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using Newtonsoft.Json;
using RateListener.Models;

namespace RateListener.Helpers;

public static class RatesArchive
{
    public const int SegmentDays = 30;
    public static readonly DateTime SegmentEpoch = new(2000, 1, 1);

    public static DateTime SegmentStart(DateTime time)
    {
        var days = (int)((time.Date - SegmentEpoch).TotalDays / SegmentDays) * SegmentDays;
        return SegmentEpoch.AddDays(days);
    }

    public static string ArchiveDirectory =>
        Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ArchivedRates");

    private static string EntryName(DateTime segmentStart) =>
        $"rates_{segmentStart:yyyy-MM-dd}.json";

    public static List<StoredRatesContainer> ReadSegment(string zipPath)
    {
        using var stream = new FileStream(zipPath, FileMode.Open, FileAccess.Read);
        using var archive = new ZipArchive(stream, ZipArchiveMode.Read);
        var entry = archive.Entries.FirstOrDefault();
        if (entry == null)
            return [];
        using var entryStream = entry.Open();
        using var reader = new StreamReader(entryStream);
        return JsonConvert.DeserializeObject<List<StoredRatesContainer>>(reader.ReadToEnd()) ?? [];
    }

    public static void WriteSegment(string zipPath, List<StoredRatesContainer> containers)
    {
        var directory = Path.GetDirectoryName(zipPath)!;
        if (!Directory.Exists(directory))
            Directory.CreateDirectory(directory);

        var tempPath = zipPath + ".tmp";
        using (var stream = new FileStream(tempPath, FileMode.Create))
        using (var archive = new ZipArchive(stream, ZipArchiveMode.Create))
        using (var entry = archive.CreateEntry("rates.json", CompressionLevel.Optimal).Open())
        using (var writer = new StreamWriter(entry))
        {
            writer.Write(JsonHelper.GetSerializedString(containers));
        }
        File.Move(tempPath, zipPath, true);
    }

    public static void MergeIntoSegment(string zipPath, DateTime segmentStart,
        Dictionary<Guid, StoredRatesContainer> activeContainers)
    {
        var merged = File.Exists(zipPath)
            ? ReadSegment(zipPath)
            : [];

        foreach (var active in activeContainers.Values)
        {
            var target = merged.FirstOrDefault(m => m.ListenerId == active.ListenerId);
            if (target == null)
            {
                target = new StoredRatesContainer { ListenerId = active.ListenerId };
                merged.Add(target);
            }

            foreach (var direction in active.Directions)
            {
                var segmentRates = direction.Rates
                    .Where(r => TryParseTime(r.Time, out var t) && SegmentStart(t) == segmentStart)
                    .ToList();
                if (segmentRates.Count == 0)
                    continue;
                var targetDirection = target.Directions.FirstOrDefault(d => d.Direction == direction.Direction);
                if (targetDirection == null)
                {
                    targetDirection = new DirectionInfo { Direction = direction.Direction };
                    target.Directions.Add(targetDirection);
                }
                var knownTimes = targetDirection.Rates.Select(r => r.Time).ToHashSet();
                targetDirection.Rates.AddRange(segmentRates.Where(r => !knownTimes.Contains(r.Time)));
            }
        }

        merged.RemoveAll(c => c.Directions.All(d => d.Rates.Count == 0));
        if (merged.Count > 0)
            WriteSegment(zipPath, merged);
    }

    public static bool TryParseTime(string source, out DateTime time) =>
        DateTime.TryParseExact(source, "dd.MM.yyyy HH:mm:ss",
            CultureInfo.InvariantCulture, DateTimeStyles.None, out time);

    public static IEnumerable<string> EnumerateArchivesCovering(DateTime from)
    {
        if (!Directory.Exists(ArchiveDirectory))
            yield break;
        foreach (var zipPath in Directory.GetFiles(ArchiveDirectory, "*.zip"))
        {
            var nameStart = Path.GetFileNameWithoutExtension(zipPath);
            if (!DateTime.TryParseExact(nameStart, "yyyy-MM-dd",
                    CultureInfo.InvariantCulture, DateTimeStyles.None, out var segmentStart))
                continue;
            if (segmentStart.AddDays(SegmentDays) <= from)
                continue;
            yield return zipPath;
        }
    }
}
