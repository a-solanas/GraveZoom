using System;
using System.Collections.Generic;
using System.Linq;

namespace GraveZoom
{
    internal static class ZoomMath
    {
        public const float NativePercent = 100f;
        public const float StepTolerance = 0.01f;

        public static List<float> BuildStops(int nativeHeight, IEnumerable<int> referenceHeights, float min, float max, float fallbackStep)
        {
            // Swap min and max if reversed
            if (min > max)
            {
                float temp = min;
                min = max;
                max = temp;
            }

            // Treat fallbackStep between 0 and 1 as 1
            if (fallbackStep > 0f && fallbackStep < 1f)
            {
                fallbackStep = 1f;
            }

            var candidates = new List<float>();

            // Add boundary and native candidates
            candidates.Add(NativePercent);
            candidates.Add(min);
            candidates.Add(max);

            // Add reference height stops
            foreach (int refHeight in referenceHeights)
            {
                if (refHeight > 0)
                {
                    candidates.Add((float)Math.Round(NativePercent * nativeHeight / refHeight));
                }
            }

            // Add fallback step grid
            if (fallbackStep > 0f)
            {
                for (int i = 1; NativePercent + i * fallbackStep <= max; i++)
                {
                    candidates.Add((float)Math.Round(NativePercent + i * fallbackStep));
                }
                for (int i = 1; NativePercent - i * fallbackStep >= min; i++)
                {
                    candidates.Add((float)Math.Round(NativePercent - i * fallbackStep));
                }
            }

            // Filter, deduplicate, and sort
            return candidates
                .Where(stop => stop >= min && stop <= max)
                .Distinct()
                .OrderBy(stop => stop)
                .ToList();
        }

        public static float? NextStop(IReadOnlyList<float> sortedStops, float current, bool higher)
        {
            if (higher)
            {
                // Find smallest stop > current + StepTolerance
                float threshold = current + StepTolerance;
                foreach (float stop in sortedStops)
                {
                    if (stop > threshold)
                    {
                        return stop;
                    }
                }
            }
            else
            {
                // Find largest stop < current - StepTolerance
                float threshold = current - StepTolerance;
                for (int i = sortedStops.Count - 1; i >= 0; i--)
                {
                    if (sortedStops[i] < threshold)
                    {
                        return sortedStops[i];
                    }
                }
            }

            return null;
        }

        public static float ClampZoom(float value, float min, float max)
        {
            // Treat NaN or <= 0 as NativePercent
            if (float.IsNaN(value) || value <= 0f)
            {
                value = NativePercent;
            }

            // Swap min/max if reversed
            if (min > max)
            {
                float temp = min;
                min = max;
                max = temp;
            }

            // Clamp to [min, max]
            return value < min ? min : (value > max ? max : value);
        }

        public static float OrthoScale(float zoomPercent)
        {
            return NativePercent / zoomPercent;
        }

        public static List<int> ParseHeights(string csv)
        {
            if (string.IsNullOrEmpty(csv))
            {
                return new List<int>();
            }

            var result = new List<int>();
            foreach (string token in csv.Split(','))
            {
                if (int.TryParse(token.Trim(), out int value) && value > 0)
                {
                    result.Add(value);
                }
            }
            return result;
        }
    }
}
