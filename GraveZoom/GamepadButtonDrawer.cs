using BepInEx.Configuration;
using UnityEngine;

namespace GraveZoom
{
    // Click-to-bind editor for controller buttons and triggers in the F1 menu.
    // The binding is saved as text (see ControllerBinding), shown with the controller's own names.
    internal static class GamepadButtonDrawer
    {
        public static void Draw(ConfigEntryBase setting, ref bool isEditing)
        {
            ConfigEntry<string> entry = (ConfigEntry<string>)setting;

            if (isEditing)
            {
                GUILayout.Label("Press a button or pull a trigger", GUILayout.ExpandWidth(true));
                GUIUtility.keyboardControl = -1;

                if (GamepadInput.TryCapture(out ControllerBinding captured))
                {
                    entry.Value = captured.ToString();
                    isEditing = false;
                }

                if (GUILayout.Button("Cancel", GUILayout.ExpandWidth(false)))
                {
                    isEditing = false;
                }
            }
            else
            {
                if (GUILayout.Button(GamepadInput.Describe(ControllerBinding.Parse(entry.Value)), GUILayout.ExpandWidth(true)))
                {
                    isEditing = true;
                }

                if (GUILayout.Button("Clear", GUILayout.ExpandWidth(false)))
                {
                    entry.Value = ControllerBinding.None.ToString();
                }
            }
        }
    }
}
