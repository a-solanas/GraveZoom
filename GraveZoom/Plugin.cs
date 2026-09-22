using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;

namespace GraveZoom
{
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    public class Plugin : BaseUnityPlugin
    {
        public const string PluginGuid = "com.gravezoom.mod";
        public const string PluginName = "Grave Zoom";
        public const string PluginVersion = "1.1.0";

        public static ConfigEntry<float> ZoomFactor;
        public static ConfigEntry<float> MinZoomPercent;
        public static ConfigEntry<float> MaxZoomPercent;
        public static ConfigEntry<string> ZoomStopHeights;
        public static ConfigEntry<float> ZoomFallbackStepPercent;
        public static ConfigEntry<KeyboardShortcut> ZoomInKey;
        public static ConfigEntry<KeyboardShortcut> ZoomOutKey;
        public static ConfigEntry<KeyboardShortcut> ResetZoomKey;
        public static ConfigEntry<bool> ShowIndicator;

        private const float IndicatorDurationSeconds = 3f;

        private Harmony harmony;
        private GUIStyle indicatorStyle;
        private float indicatorHideTime = float.NegativeInfinity;

        private void Awake()
        {
            MinZoomPercent = Config.Bind("Zoom", "MinZoomPercent", 25f,
                "Smallest zoom percent allowed.");
            MaxZoomPercent = Config.Bind("Zoom", "MaxZoomPercent", 400f,
                "Largest zoom percent allowed.");
            ZoomStopHeights = Config.Bind("Zoom", "ZoomStopHeights", "720,900,1080,1200,1440,1600,2160",
                "Resolution heights used to compute zoom stops.");
            ZoomFallbackStepPercent = Config.Bind("Zoom", "ZoomFallbackStepPercent", 25f,
                "Extra evenly-spaced zoom stops, so you can always zoom in/out even past the resolution-based stops above. 0 disables this.");
            ZoomFactor = Config.Bind("Zoom", "ZoomFactor", -1f,
                "Current zoom percent. 100 = native. Higher = more zoomed in. -1 = auto-detect. " +
                "You can type any custom value here; zooming in/out from it snaps to the nearest stop.");

            ZoomInKey = Config.Bind("Hotkeys", "ZoomIn", new KeyboardShortcut(KeyCode.PageUp),
                "Zoom in.");
            ZoomOutKey = Config.Bind("Hotkeys", "ZoomOut", new KeyboardShortcut(KeyCode.PageDown),
                "Zoom out.");
            ResetZoomKey = Config.Bind("Hotkeys", "ResetZoom", new KeyboardShortcut(KeyCode.Home),
                "Reset zoom to default.");

            ShowIndicator = Config.Bind("Display", "ShowZoomIndicator", true,
                "Show the zoom value on screen when it changes.");

            ZoomFactor.SettingChanged += (_, __) =>
            {
                indicatorHideTime = Time.unscaledTime + IndicatorDurationSeconds;
                ZoomController.Reapply();
            };

            harmony = new Harmony(PluginGuid);
            harmony.PatchAll();

            Logger.LogInfo($"{PluginName} v{PluginVersion} loaded.");
        }

        private void Update()
        {
            if (ZoomInKey.Value.IsDown())
            {
                ZoomController.StepUp();
            }
            else if (ZoomOutKey.Value.IsDown())
            {
                ZoomController.StepDown();
            }
            else if (ResetZoomKey.Value.IsDown())
            {
                ZoomController.ResetToDefault();
            }
        }

        private void OnGUI()
        {
            if (!ShowIndicator.Value || Time.unscaledTime > indicatorHideTime)
            {
                return;
            }

            if (indicatorStyle == null)
            {
                indicatorStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 16,
                    fontStyle = FontStyle.Bold,
                    wordWrap = false,
                    clipping = TextClipping.Overflow,
                    alignment = TextAnchor.MiddleCenter
                };
                indicatorStyle.normal.textColor = Color.white;
            }

            string text = $"Zoom: {ZoomController.EffectiveZoom:0}%";
            Vector2 size = indicatorStyle.CalcSize(new GUIContent(text));

            const float padding = 6f;
            const float topMargin = 12f;
            const float safety = 8f; // guard against font metric rounding cutting off the last glyphs
            float boxWidth = size.x + padding * 2f + safety;
            float boxHeight = size.y + padding * 2f;
            Rect boxRect = new Rect((Screen.width - boxWidth) / 2f, topMargin, boxWidth, boxHeight);

            GUI.Box(boxRect, GUIContent.none);
            GUI.Label(new Rect(boxRect.x, boxRect.y + padding, boxWidth, size.y), text, indicatorStyle);
        }
    }
}
