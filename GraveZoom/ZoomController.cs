using System.Collections.Generic;
using UnityEngine;

namespace GraveZoom
{
    // Central place that owns "what should the zoom percent be right now" and
    // how to push a changed value back into the game's own camera system.
    // 100% == native for the current resolution. Larger == more zoomed in
    // (same direction as browser/image-viewer zoom).
    internal static class ZoomController
    {
        public static void StepUp()
        {
            EnsureInitialized();
            Step(higher: true);
        }

        public static void StepDown()
        {
            EnsureInitialized();
            Step(higher: false);
        }

        private static void Step(bool higher)
        {
            float current = Plugin.ZoomFactor.Value;
            float? next = null;

            foreach (float stop in BuildStops())
            {
                bool isCandidate = higher ? stop > current + 0.5f : stop < current - 0.5f;
                if (!isCandidate)
                {
                    continue;
                }
                if (!next.HasValue || (higher ? stop < next.Value : stop > next.Value))
                {
                    next = stop;
                }
            }

            if (next.HasValue)
            {
                Plugin.ZoomFactor.Value = next.Value; // fires SettingChanged -> Reapply()
            }
        }

        // Zoom stops: one per configured reference resolution height, each computed
        // as a single direct ratio (100 * nativeHeight / refHeight) so it lands
        // exactly on the equivalent zoom of that resolution, with no drift.
        //
        // Those alone aren't enough: if the user's native height is >= the largest
        // configured reference height (e.g. 4K with the default list topping out at
        // 2160), every resulting stop is >= 100, so Zoom Out has nowhere to go. To
        // guarantee there's always a next stop in both directions, we also add
        // evenly-spaced stops out to MinZoomPercent/MaxZoomPercent, and the two
        // bounds themselves.
        private static List<float> BuildStops()
        {
            List<float> stops = new List<float>();
            float min = Plugin.MinZoomPercent.Value;
            float max = Plugin.MaxZoomPercent.Value;

            AddStop(stops, 100f, min, max);
            AddStop(stops, min, min, max);
            AddStop(stops, max, min, max);

            int nativeHeight = CurrentNativeHeight();
            foreach (string token in Plugin.ZoomStopHeights.Value.Split(','))
            {
                if (!int.TryParse(token.Trim(), out int refHeight) || refHeight <= 0)
                {
                    continue;
                }
                AddStop(stops, Mathf.Round(100f * nativeHeight / refHeight), min, max);
            }

            float step = Plugin.ZoomFallbackStepPercent.Value;
            if (step > 0f)
            {
                for (int i = 1; 100f + i * step <= max; i++)
                {
                    AddStop(stops, Mathf.Round(100f + i * step), min, max);
                }
                for (int i = 1; 100f - i * step >= min; i++)
                {
                    AddStop(stops, Mathf.Round(100f - i * step), min, max);
                }
            }

            stops.Sort();
            return stops;
        }

        private static void AddStop(List<float> stops, float percent, float min, float max)
        {
            if (percent >= min && percent <= max && !stops.Contains(percent))
            {
                stops.Add(percent);
            }
        }

        private static int CurrentNativeHeight()
        {
            return GameSettings.Instance != null ? GameSettings.Instance.GetResolutionIntVector2().y : Screen.height;
        }

        public static void EnsureInitialized()
        {
            if (Plugin.ZoomFactor.Value > 0f)
            {
                return;
            }
            Plugin.ZoomFactor.Value = 100f;
        }

        public static void ResetToDefault()
        {
            Plugin.ZoomFactor.Value = 100f;
        }

        // What the camera should actually use right now, without mutating state.
        // Shared by the Harmony patch and the on-screen indicator so they can never disagree.
        public static float EffectiveZoom => Plugin.ZoomFactor.Value > 0f ? Plugin.ZoomFactor.Value : 100f;

        public static void Reapply()
        {
            EnsureInitialized();
            CameraSystem cameraSystem = CameraSystem.Instance;
            if (cameraSystem == null || GameSettings.Instance == null)
            {
                return;
            }
            // Re-runs the game's own resolution-changed pipeline (now patched),
            // exactly as if the resolution changed, so all downstream cameras
            // and render targets pick up the new zoom consistently.
            cameraSystem.OnResolutionChanged(GameSettings.Instance.GetResolutionIntVector2());
        }
    }
}
