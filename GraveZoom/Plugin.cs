using System;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using LazyBearTechnology;
using UnityEngine;

namespace GraveZoom
{
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    public class Plugin : BaseUnityPlugin
    {
        public const string PluginGuid = "com.gravezoom.mod";
        public const string PluginName = "Grave Zoom";
        public const string PluginVersion = "1.2.1";

        internal static ConfigEntry<float> ZoomFactor;
        internal static ConfigEntry<float> MinZoomPercent;
        internal static ConfigEntry<float> MaxZoomPercent;
        internal static ConfigEntry<string> ZoomStopHeights;
        internal static ConfigEntry<float> ZoomFallbackStepPercent;

        private const float IndicatorDurationSeconds = 3f;

        private ConfigEntry<bool> showIndicator;
        private ConfigEntry<bool> disableGamepadInMenus;
        private HotkeyBinding[] bindings;
        private GUIStyle indicatorStyle;
        private float indicatorHideTime = float.NegativeInfinity;

        private void Awake()
        {
            // Ranges keep hand-typed values from breaking the camera or stalling the stop builder.
            MinZoomPercent = Config.Bind("Zoom", "MinZoomPercent", 25f,
                new ConfigDescription("Smallest zoom percent allowed.", new AcceptableValueRange<float>(10f, 100f)));
            MaxZoomPercent = Config.Bind("Zoom", "MaxZoomPercent", 400f,
                new ConfigDescription("Largest zoom percent allowed.", new AcceptableValueRange<float>(100f, 1000f)));
            ZoomStopHeights = Config.Bind("Zoom", "ZoomStopHeights", "720,900,1080,1200,1440,1600,2160",
                "Resolution heights used to compute zoom stops.");
            ZoomFallbackStepPercent = Config.Bind("Zoom", "ZoomFallbackStepPercent", 25f,
                new ConfigDescription(
                    "Extra evenly-spaced zoom stops, so you can always zoom in/out even past the resolution-based stops above. 0 disables this.",
                    new AcceptableValueRange<float>(0f, 100f)));
            ZoomFactor = Config.Bind("Zoom", "ZoomFactor", ZoomMath.NativePercent,
                "Current zoom percent. 100 = native. Higher = more zoomed in. " +
                "You can type any value here; zooming in/out from it snaps to the nearest stop.");

            showIndicator = Config.Bind("Display", "ShowZoomIndicator", true,
                "Show the zoom value on screen when it changes.");

            disableGamepadInMenus = Config.Bind("Gamepad", "DisableInMenus", true,
                "Ignore the controller buttons while a game menu or window is open (for example crafting). The keyboard keys still work.");

            bindings = new[]
            {
                BindHotkey("ZoomIn", KeyCode.PageUp, KeyCode.JoystickButton5, "Zoom in.", ZoomController.StepUp),
                BindHotkey("ZoomOut", KeyCode.PageDown, KeyCode.JoystickButton4, "Zoom out.", ZoomController.StepDown),
                BindHotkey("ResetZoom", KeyCode.Home, KeyCode.None, "Reset zoom to default.", ZoomController.ResetToDefault),
            };

            ZoomFactor.SettingChanged += (_, __) =>
            {
                indicatorHideTime = Time.unscaledTime + IndicatorDurationSeconds;
                ZoomController.Reapply();
            };

            // Zoom is relative to native, so a new resolution starts from native again.
            GameSettings.OnResolutionChanged += _ => ZoomController.ResetToDefault();

            try
            {
                new Harmony(PluginGuid).PatchAll();
            }
            catch (Exception exception)
            {
                Logger.LogError($"{PluginName} can't hook this game version and is disabled: {exception}");
                enabled = false;
                return;
            }

            Logger.LogInfo($"{PluginName} v{PluginVersion} loaded.");
        }

        private HotkeyBinding BindHotkey(string name, KeyCode defaultKey, KeyCode defaultButton, string description, Action action)
        {
            ConfigEntry<KeyboardShortcut> key = Config.Bind("Hotkeys", name, new KeyboardShortcut(defaultKey), description);
            ConfigEntry<KeyCode> button = Config.Bind("Gamepad", name + "Button", defaultButton,
                new ConfigDescription("Controller button for this action. Click it, then press a button.", null,
                    new ConfigurationManagerAttributes { CustomHotkeyDrawer = GamepadButtonDrawer.Draw }));
            return new HotkeyBinding(key, button, action);
        }

        private void Update()
        {
            bool gamepadEnabled = !(disableGamepadInMenus.Value && LazyWindowsStackController.ActiveWindow != null);

            foreach (HotkeyBinding binding in bindings)
            {
                if (binding.IsPressed(gamepadEnabled))
                {
                    binding.Action();
                    break;
                }
            }
        }

        private sealed class HotkeyBinding
        {
            private readonly ConfigEntry<KeyboardShortcut> key;
            private readonly ConfigEntry<KeyCode> button;

            public readonly Action Action;

            public HotkeyBinding(ConfigEntry<KeyboardShortcut> key, ConfigEntry<KeyCode> button, Action action)
            {
                this.key = key;
                this.button = button;
                Action = action;
            }

            public bool IsPressed(bool gamepadEnabled)
            {
                KeyCode gamepad = button.Value;
                return key.Value.IsDown() || (gamepadEnabled && gamepad != KeyCode.None && UnityInput.Current.GetKeyDown(gamepad));
            }
        }

        private void OnGUI()
        {
            if (!showIndicator.Value || Time.unscaledTime > indicatorHideTime)
            {
                return;
            }

            float scale = Mathf.Max(1f, Screen.height / 1080f);
            int scaledFontSize = Mathf.RoundToInt(16 * scale);

            if (indicatorStyle == null)
            {
                indicatorStyle = new GUIStyle(GUI.skin.label)
                {
                    fontStyle = FontStyle.Bold,
                    wordWrap = false,
                    clipping = TextClipping.Overflow,
                    alignment = TextAnchor.MiddleCenter
                };
                indicatorStyle.normal.textColor = Color.white;
            }

            indicatorStyle.fontSize = scaledFontSize;

            string text = $"Zoom: {ZoomController.EffectiveZoom:0}%";
            Vector2 size = indicatorStyle.CalcSize(new GUIContent(text));

            float padding = 6f * scale;
            float topMargin = 12f * scale;
            float boxWidth = size.x + padding * 2f;
            float boxHeight = size.y + padding * 2f;
            Rect boxRect = new Rect((Screen.width - boxWidth) / 2f, topMargin, boxWidth, boxHeight);

            GUI.Box(boxRect, GUIContent.none);
            GUI.Label(new Rect(boxRect.x, boxRect.y + padding, boxWidth, size.y), text, indicatorStyle);
        }
    }
}
