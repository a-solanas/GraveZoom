using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Configuration;
using UnityEngine;

namespace GraveZoom
{
    // The shared GamepadInput.cs writes its diagnostics through Plugin.Log. Inside the bridge this
    // stands in for the main plugin's class of the same name.
    internal static class Plugin
    {
        internal static BepInEx.Logging.ManualLogSource Log;
    }
}

namespace GraveZoom.FrameworkBridge
{
    // Optional add-on: when GK2 Mod Framework is installed, lists Grave Zoom in its Mods menu.
    // Grave Zoom itself never references the framework, so it works the same without it.
    //
    // The framework is a soft dependency: this plugin always loads, and only touches framework code
    // (in FrameworkRegistration) when the framework is really there. A hard dependency would make
    // BepInEx log an error for everyone who does not use the framework.
    //
    // Only the everyday settings are shown here. The advanced zoom limits and step lists stay in the
    // F1 menu, to keep this menu short and easy to use with a controller.
    //
    // The Mods menu's "Disable after restart" button writes Framework/Enabled in Grave Zoom's config.
    // Grave Zoom binds that same entry itself and skips loading when it is false.
    //
    // The settings are registered against Grave Zoom's own config file with the same section, key
    // and type, so the Mods menu edits the very same values Grave Zoom reads.
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    [BepInDependency(GraveZoomGuid, BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency(FrameworkGuid, BepInDependency.DependencyFlags.SoftDependency)]
    public sealed class FrameworkBridgePlugin : BaseUnityPlugin
    {
        public const string GraveZoomGuid = "com.gravezoom.mod";
        public const string FrameworkGuid = "ru.superman4eg.gk2.framework";
        public const string PluginGuid = "com.gravezoom.mod.framework";
        public const string PluginName = "Grave Zoom - GK2 Framework Integration";
        public const string PluginVersion = "1.3.0";

        private const float CaptureTimeoutSeconds = 8f;

        private ConfigFile config;
        private string capturingKey;
        private float captureStartTime;
        private int captureStartFrame;

        private void Awake()
        {
            GraveZoom.Plugin.Log = Logger;

            // Without the framework there is nothing to do. Grave Zoom itself already logs that it is
            // falling back to the F1 menu.
            if (!Chainloader.PluginInfos.ContainsKey(FrameworkGuid)
                || !Chainloader.PluginInfos.TryGetValue(GraveZoomGuid, out PluginInfo main)
                || main.Instance == null)
            {
                enabled = false;
                return;
            }

            config = main.Instance.Config;
            FrameworkRegistration.Register(this, main);
        }

        // Remapping lives here, in Grave Zoom's own code. The framework only draws a clickable row.
        internal void BeginCapture(string key)
        {
            capturingKey = key;
            captureStartTime = Time.unscaledTime;
            captureStartFrame = Time.frameCount;
        }

        private void Update()
        {
            if (capturingKey == null)
            {
                return;
            }

            // Skip the frame of the click itself: the same controller press that clicked the row
            // would otherwise be captured as the new binding.
            if (Time.frameCount <= captureStartFrame + 1)
            {
                return;
            }

            bool cancelled = UnityInput.Current.GetKeyDown(KeyCode.Escape)
                || Time.unscaledTime - captureStartTime > CaptureTimeoutSeconds;
            if (cancelled)
            {
                capturingKey = null;
            }
            else if (GamepadInput.TryCapture(out ControllerBinding captured))
            {
                if (config.TryGetEntry("Gamepad", capturingKey, out ConfigEntry<string> entry))
                {
                    entry.Value = captured.ToString();
                }

                capturingKey = null;
            }
        }

        internal string Describe(string key)
        {
            if (capturingKey == key)
            {
                return "Press a button or pull a trigger (Esc cancels)";
            }

            if (!config.TryGetEntry("Gamepad", key, out ConfigEntry<string> entry))
            {
                return "-";
            }

            ControllerBinding binding = ControllerBinding.Parse(entry.Value);
            return binding.Kind == BindingKind.None ? "Not set" : GamepadInput.Describe(binding);
        }
    }
}
