using System.Collections.Generic;
using System.Linq;
using BepInEx;
using Rewired;
using UnityEngine;

namespace GraveZoom
{
    // Controller access through Rewired, the game's own input library. Rewired knows each
    // controller's real layout, so buttons and triggers carry names like "Square", "B" or
    // "Right Trigger" that match what is printed on the pad.
    internal static class GamepadInput
    {
        private static int loggedJoystickCount = -1;
        private static KeyCode[] legacyJoystickKeys;

        public static bool HasJoystick => ReInput.isReady && ReInput.controllers.joystickCount > 0;

        // Finds the button or axis a binding refers to, on the first connected controller that has it.
        // An axis binding (a trigger) falls back to a button with the same name, because some
        // controllers, such as the Switch Pro, have digital triggers (ZL/ZR) instead of analog ones.
        public static bool TryFind(ControllerBinding binding, out Joystick joystick, out int index, out bool isButton)
        {
            joystick = null;
            index = -1;
            isButton = false;
            if (!ReInput.isReady)
            {
                return false;
            }

            IList<Joystick> joysticks = ReInput.controllers.Joysticks;
            LogControllers(joysticks);

            if (binding.Kind == BindingKind.Axis)
            {
                for (int j = 0; j < joysticks.Count; j++)
                {
                    int axis = FindAxis(joysticks[j], binding);
                    if (axis >= 0)
                    {
                        joystick = joysticks[j];
                        index = axis;
                        return true;
                    }
                }
            }

            for (int j = 0; j < joysticks.Count; j++)
            {
                int button = FindButton(joysticks[j], binding);
                if (button >= 0)
                {
                    joystick = joysticks[j];
                    index = button;
                    isButton = true;
                    return true;
                }
            }

            return false;
        }

        // The name to show for a binding: the connected controller's own name for that control
        // ("Right Shoulder", "Square"...), or the saved text if no controller has it right now.
        public static string Describe(ControllerBinding binding)
        {
            if ((binding.Kind == BindingKind.Button || binding.Kind == BindingKind.Axis)
                && TryFind(binding, out Joystick joystick, out int index, out bool isButton))
            {
                Controller.Element element = isButton ? (Controller.Element)joystick.Buttons[index] : joystick.Axes[index];
                string name = NameOf(element);
                return binding.Kind == BindingKind.Axis && !isButton && !binding.Positive ? name + " (-)" : name;
            }

            return binding.Describe();
        }

        // Used by the F1 menu: returns the first button pressed or axis pushed since the last frame.
        public static bool TryCapture(out ControllerBinding binding)
        {
            binding = default;
            return HasJoystick ? TryCaptureFromRewired(out binding) : TryCaptureLegacyKey(out binding);
        }

        private static bool TryCaptureFromRewired(out ControllerBinding binding)
        {
            binding = default;
            IList<Joystick> joysticks = ReInput.controllers.Joysticks;

            foreach (Joystick joystick in joysticks)
            {
                IList<Controller.Button> buttons = joystick.Buttons;
                for (int i = 0; i < buttons.Count; i++)
                {
                    if (joystick.GetButtonDown(i))
                    {
                        binding = new ControllerBinding(BindingKind.Button, NameOf(buttons[i]), true);
                        return true;
                    }
                }

                IList<Controller.Axis> axes = joystick.Axes;
                for (int i = 0; i < axes.Count; i++)
                {
                    float previous = joystick.GetAxisPrev(i);
                    float current = joystick.GetAxis(i);
                    if (ControllerBinding.IsPressEdge(previous, current, positive: true))
                    {
                        binding = new ControllerBinding(BindingKind.Axis, NameOf(axes[i]), true);
                        return true;
                    }

                    if (ControllerBinding.IsPressEdge(previous, current, positive: false))
                    {
                        binding = new ControllerBinding(BindingKind.Axis, NameOf(axes[i]), false);
                        return true;
                    }
                }
            }

            return false;
        }

        // Fallback for setups where Rewired doesn't see a controller but Unity does.
        private static bool TryCaptureLegacyKey(out ControllerBinding binding)
        {
            binding = default;
            if (legacyJoystickKeys == null)
            {
                legacyJoystickKeys = UnityInput.Current.SupportedKeyCodes
                    .Where(k => k.ToString().StartsWith("Joystick", System.StringComparison.Ordinal))
                    .ToArray();
            }

            foreach (KeyCode key in legacyJoystickKeys)
            {
                if (UnityInput.Current.GetKeyUp(key))
                {
                    binding = ControllerBinding.Parse(key.ToString());
                    return true;
                }
            }

            return false;
        }

        private static int FindButton(Joystick joystick, ControllerBinding binding)
        {
            IList<Controller.Button> buttons = joystick.Buttons;
            for (int i = 0; i < buttons.Count; i++)
            {
                if (IsElement(buttons[i], binding))
                {
                    return i;
                }
            }

            return -1;
        }

        private static int FindAxis(Joystick joystick, ControllerBinding binding)
        {
            IList<Controller.Axis> axes = joystick.Axes;
            for (int i = 0; i < axes.Count; i++)
            {
                if (IsElement(axes[i], binding))
                {
                    return i;
                }
            }

            return -1;
        }

        // Rewired keeps the friendly hardware name ("Right Shoulder") on the element's identifier;
        // Element.name is only a generic "Button 5". Bindings saved with either name still match.
        private static string NameOf(Controller.Element element)
        {
            string friendly = element.elementIdentifier?.name;
            return string.IsNullOrEmpty(friendly) ? element.name : friendly;
        }

        private static bool IsElement(Controller.Element element, ControllerBinding binding)
        {
            return binding.Matches(NameOf(element)) || binding.Matches(element.name);
        }

        // One line per controller in the BepInEx log, so a wrong or missing name is easy to diagnose.
        private static void LogControllers(IList<Joystick> joysticks)
        {
            if (joysticks.Count == loggedJoystickCount)
            {
                return;
            }

            loggedJoystickCount = joysticks.Count;
            foreach (Joystick joystick in joysticks)
            {
                Plugin.Log.LogInfo($"Controller '{joystick.name}': " +
                    $"buttons [{string.Join(", ", joystick.Buttons.Select(b => b.name + "=" + NameOf(b)))}], " +
                    $"axes [{string.Join(", ", joystick.Axes.Select(a => a.name + "=" + NameOf(a)))}]");
            }
        }
    }
}
