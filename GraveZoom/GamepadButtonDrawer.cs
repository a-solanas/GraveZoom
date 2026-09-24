using System;
using System.Linq;
using BepInEx;
using BepInEx.Configuration;
using UnityEngine;

namespace GraveZoom
{
    // Click-to-bind editor for controller buttons in the F1 menu. Detection mirrors
    // ConfigurationManager's own "Set..." capture (every supported key code, on release),
    // limited to joystick codes so a stray keyboard press can't be bound by accident.
    internal static class GamepadButtonDrawer
    {
        private static KeyCode[] joystickKeys;

        public static void Draw(ConfigEntryBase setting, ref bool isEditing)
        {
            ConfigEntry<KeyCode> entry = (ConfigEntry<KeyCode>)setting;

            if (isEditing)
            {
                GUILayout.Label("Press a controller button", GUILayout.ExpandWidth(true));
                GUIUtility.keyboardControl = -1;

                if (joystickKeys == null)
                {
                    joystickKeys = UnityInput.Current.SupportedKeyCodes
                        .Where(k => k.ToString().StartsWith("Joystick", StringComparison.Ordinal))
                        .ToArray();
                }

                foreach (KeyCode key in joystickKeys)
                {
                    if (UnityInput.Current.GetKeyUp(key))
                    {
                        entry.Value = key;
                        isEditing = false;
                        break;
                    }
                }

                if (GUILayout.Button("Cancel", GUILayout.ExpandWidth(false)))
                {
                    isEditing = false;
                }
            }
            else
            {
                string label = entry.Value == KeyCode.None ? "Not set (click to bind)" : entry.Value.ToString();
                if (GUILayout.Button(label, GUILayout.ExpandWidth(true)))
                {
                    isEditing = true;
                }

                if (GUILayout.Button("Clear", GUILayout.ExpandWidth(false)))
                {
                    entry.Value = KeyCode.None;
                }
            }
        }
    }
}
