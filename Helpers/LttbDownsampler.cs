using System;
using System.Collections.Generic;
using System.Linq;

namespace RateListener.Helpers;

public static class LttbDownsampler
{
    /// <summary>
    /// Largest-Triangle-Three-Buckets: возвращает индексы исходного массива,
    /// сохраняющие форму кривой (включая первый и последний элементы), не более maxPoints штук.
    /// </summary>
    public static List<int> PickIndices(IReadOnlyList<double> values, int maxPoints)
    {
        var count = values.Count;
        if (count <= maxPoints)
            return Enumerable.Range(0, count).ToList();

        var result = new List<int>(maxPoints) { 0 };
        var bucketSize = (double)(count - 2) / (maxPoints - 2);
        var selectedIndex = 0;

        for (var bucket = 0; bucket < maxPoints - 2; bucket++)
        {
            var bucketStart = (int)Math.Floor((bucket + 1) * bucketSize) + 1;
            var bucketEnd = Math.Min((int)Math.Floor((bucket + 2) * bucketSize) + 1, count - 1);

            var nextEnd = Math.Min((int)Math.Floor((bucket + 3) * bucketSize) + 1, count);
            var nextLength = Math.Max(1, nextEnd - bucketEnd);
            var avgX = (bucketEnd + nextEnd - 1) / 2.0;
            double avgY = 0;
            for (var i = bucketEnd; i < nextEnd; i++)
                avgY += values[i];
            avgY /= nextLength;

            double maxArea = -1;
            var bestIndex = bucketStart;
            for (var i = bucketStart; i < bucketEnd; i++)
            {
                var area = Math.Abs(
                    (selectedIndex - avgX) * (values[i] - values[selectedIndex]) -
                    (selectedIndex - i) * (avgY - values[selectedIndex]));
                if (area > maxArea)
                {
                    maxArea = area;
                    bestIndex = i;
                }
            }

            result.Add(bestIndex);
            selectedIndex = bestIndex;
        }

        result.Add(count - 1);
        return result;
    }
}
