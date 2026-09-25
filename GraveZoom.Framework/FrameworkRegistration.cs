using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using BepInEx;
using BepInEx.Configuration;
using GK2.Framework;
using UnityEngine;

namespace GraveZoom.FrameworkBridge
{
    // Everything that touches GK2 Mod Framework types is here. It is only called after the plugin
    // has confirmed the framework is installed, so the framework DLL is never loaded otherwise.
    internal static class FrameworkRegistration
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void Register(FrameworkBridgePlugin owner, PluginInfo main)
        {
            FrameworkApi.RegisterMod(new GraveZoomMod(owner, main), main.Instance.Config);
        }

        private sealed class GraveZoomMod : Gk2ModBase
        {
            private readonly FrameworkBridgePlugin owner;
            private readonly Gk2ModMetadata metadata;

            public GraveZoomMod(FrameworkBridgePlugin owner, PluginInfo main)
            {
                this.owner = owner;
                metadata = new Gk2ModMetadata(
                    FrameworkBridgePlugin.GraveZoomGuid,
                    main.Metadata.Name,
                    "Grave Zoom project",
                    main.Metadata.Version.ToString(),
                    "Zoom the camera independently of your screen resolution.",
                    supportsRuntimeToggle: false,
                    requiresKnownBuild: false,
                    frameworkManagesEnabledState: true);
            }

            public override Gk2ModMetadata Metadata => metadata;

            public override void OnRegister(Gk2ModContext context)
            {
                Gk2Settings settings = context.Settings;

                settings.AddFloatSlider("Zoom", "ZoomFactor", 100f, 10f, 1000f,
                    "Zoom", "Current zoom percent. 100 = native. Higher = more zoomed in.", step: 1f);

                settings.AddKeybind("Hotkeys", "ZoomIn", new KeyboardShortcut(KeyCode.PageUp),
                    "Zoom in key", "Zoom in.");
                settings.AddKeybind("Hotkeys", "ZoomOut", new KeyboardShortcut(KeyCode.PageDown),
                    "Zoom out key", "Zoom out.");
                settings.AddKeybind("Hotkeys", "ResetZoom", new KeyboardShortcut(KeyCode.Home),
                    "Reset zoom key", "Reset zoom to default.");

                settings.AddToggle("Gamepad", "DisableInMenus", true,
                    "Ignore controller in menus",
                    "The controller buttons do nothing while a game menu is open. The keyboard keys still work.");
                AddControllerButton(settings, "ZoomInButton", "Zoom in button");
                AddControllerButton(settings, "ZoomOutButton", "Zoom out button");
                AddControllerButton(settings, "ResetZoomButton", "Reset zoom button");

                settings.AddToggle("Display", "ShowZoomIndicator", true,
                    "Show zoom on screen", "Show the zoom value on screen when it changes.");
            }

            // Newer framework versions have a clickable row (AddButton) that lets the player press a
            // controller button to rebind. Older versions do not, so it is looked up at runtime and
            // the row falls back to a read-only text that points to the F1 menu.
            private void AddControllerButton(Gk2Settings settings, string key, string name)
            {
                MethodInfo addButton = typeof(Gk2Settings).GetMethod("AddButton");
                if (addButton != null)
                {
                    addButton.Invoke(settings, new object[]
                    {
                        "Gamepad", key, name,
                        "Click, then press a button or pull a trigger on your controller.",
                        (Func<string>)(() => owner.Describe(key)),
                        (Action)(() => owner.BeginCapture(key)),
                        0
                    });
                    return;
                }

                settings.AddReadOnly("Gamepad", key, name,
                    "To change it, press F1 in game, then click the setting and press a button or pull a trigger.",
                    () => owner.Describe(key));
            }
        }
    }
}
