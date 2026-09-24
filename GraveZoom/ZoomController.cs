using LazyBearTechnology;

namespace GraveZoom
{
    // Zoom is a percent of the game's own zoom for the current resolution:
    // 100 = vanilla, larger = more zoomed in.
    internal static class ZoomController
    {
        // Always within [MinZoomPercent, MaxZoomPercent], even if the user types nonsense in F1.
        public static float EffectiveZoom => ZoomMath.ClampZoom(
            Plugin.ZoomFactor.Value, Plugin.MinZoomPercent.Value, Plugin.MaxZoomPercent.Value);

        public static void StepUp() => Step(higher: true);

        public static void StepDown() => Step(higher: false);

        public static void ResetToDefault() => Plugin.ZoomFactor.Value = ZoomMath.NativePercent;

        // Re-runs the game's resolution-changed handler so it picks up the new zoom.
        public static void Reapply()
        {
            CameraSystem cameraSystem = CameraSystem.Instance;
            if (cameraSystem != null)
            {
                cameraSystem.OnResolutionChanged(CurrentResolution());
            }
        }

        private static void Step(bool higher)
        {
            var stops = ZoomMath.BuildStops(
                CurrentResolution().y,
                ZoomMath.ParseHeights(Plugin.ZoomStopHeights.Value),
                Plugin.MinZoomPercent.Value,
                Plugin.MaxZoomPercent.Value,
                Plugin.ZoomFallbackStepPercent.Value);

            float? next = ZoomMath.NextStop(stops, EffectiveZoom, higher);
            if (next.HasValue)
            {
                Plugin.ZoomFactor.Value = next.Value; // SettingChanged -> Reapply()
            }
        }

        private static IntVector2 CurrentResolution() => GameSettings.Instance.GetResolutionIntVector2();
    }
}
