using System;
using BepInEx;
using Rewired;
using UnityEngine;

namespace GraveZoom
{
    // Watches one controller binding. Keeps the parsed binding and the controller element it
    // resolved to, so the per-frame check is just an array lookup and doesn't allocate.
    internal sealed class GamepadTrigger
    {
        private string text;
        private ControllerBinding binding;
        private KeyCode legacyKey;
        private Joystick joystick;
        private int index = -1;
        private bool isButton;
        private int joystickCount = -1;

        // True on the frame the bound button is pressed or the bound axis crosses its threshold.
        public bool IsPressed(string configText)
        {
            if (!string.Equals(configText, text))
            {
                Reset(configText);
            }

            switch (binding.Kind)
            {
                case BindingKind.Button:
                case BindingKind.Axis:
                    return IsRewiredPressed();
                case BindingKind.LegacyKey:
                    return legacyKey != KeyCode.None && UnityInput.Current.GetKeyDown(legacyKey);
                default:
                    return false;
            }
        }

        private void Reset(string configText)
        {
            text = configText;
            binding = ControllerBinding.Parse(configText);
            legacyKey = KeyCode.None;
            if (binding.Kind == BindingKind.LegacyKey && !Enum.TryParse(binding.Name, out legacyKey))
            {
                legacyKey = KeyCode.None;
            }

            joystick = null;
            index = -1;
            joystickCount = -1;
        }

        private bool IsRewiredPressed()
        {
            if (!ReInput.isReady)
            {
                return false;
            }

            // Look the element up again only when controllers are plugged in or removed.
            int count = ReInput.controllers.joystickCount;
            if (count != joystickCount || (joystick != null && !joystick.isConnected))
            {
                joystickCount = count;
                GamepadInput.TryFind(binding, out joystick, out index, out isButton);
            }

            if (joystick == null)
            {
                return false;
            }

            if (isButton)
            {
                return joystick.GetButtonDown(index);
            }

            return ControllerBinding.IsPressEdge(joystick.GetAxisPrev(index), joystick.GetAxis(index), binding.Positive);
        }
    }
}
