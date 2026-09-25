using System;

namespace GraveZoom
{
    internal enum BindingKind
    {
        None,
        Button,
        Axis,
        LegacyKey,
    }

    // A controller input the user can bind, stored in the config as text:
    //   None                    not set
    //   Button:Right Shoulder   a button, by the name Rewired gives it on that controller
    //   Axis:Right Trigger:+    an analog axis pushed past PressThreshold (":-" = other direction)
    //   JoystickButton5         older Unity key code bindings, still honoured
    internal readonly struct ControllerBinding
    {
        public const float PressThreshold = 0.6f;

        // Names different controllers use for the same physical control.
        private static readonly string[][] EquivalentNames =
        {
            new[] { "Right Shoulder", "Right Shoulder 1", "Right Bumper", "RB", "R1", "R" },
            new[] { "Left Shoulder", "Left Shoulder 1", "Left Bumper", "LB", "L1", "L" },
            new[] { "Right Trigger", "Right Shoulder 2", "RT", "R2", "ZR" },
            new[] { "Left Trigger", "Left Shoulder 2", "LT", "L2", "ZL" },
            new[] { "Right Stick Button", "Right Stick Click", "R3" },
            new[] { "Left Stick Button", "Left Stick Click", "L3" },
        };

        public BindingKind Kind { get; }

        public string Name { get; }

        // Axes only: which direction counts as pressed.
        public bool Positive { get; }

        public ControllerBinding(BindingKind kind, string name, bool positive)
        {
            Kind = kind;
            Name = name;
            Positive = positive;
        }

        public static ControllerBinding None => default;

        public static ControllerBinding Parse(string text)
        {
            text = text?.Trim();
            if (string.IsNullOrEmpty(text) || text.Equals("None", StringComparison.OrdinalIgnoreCase))
            {
                return default;
            }

            if (text.StartsWith("Button:", StringComparison.OrdinalIgnoreCase))
            {
                string name = text.Substring("Button:".Length).Trim();
                return name.Length > 0 ? new ControllerBinding(BindingKind.Button, name, true) : default;
            }

            if (text.StartsWith("Axis:", StringComparison.OrdinalIgnoreCase))
            {
                string name = text.Substring("Axis:".Length);
                bool positive = true;
                if (name.EndsWith(":-", StringComparison.Ordinal))
                {
                    positive = false;
                    name = name.Substring(0, name.Length - 2);
                }
                else if (name.EndsWith(":+", StringComparison.Ordinal))
                {
                    name = name.Substring(0, name.Length - 2);
                }

                name = name.Trim();
                return name.Length > 0 ? new ControllerBinding(BindingKind.Axis, name, positive) : default;
            }

            return new ControllerBinding(BindingKind.LegacyKey, text, true);
        }

        // The config text for this binding; Parse(ToString()) gives the same binding back.
        public override string ToString()
        {
            switch (Kind)
            {
                case BindingKind.Button:
                    return "Button:" + Name;
                case BindingKind.Axis:
                    return "Axis:" + Name + (Positive ? ":+" : ":-");
                case BindingKind.LegacyKey:
                    return Name;
                default:
                    return "None";
            }
        }

        // What the F1 menu shows.
        public string Describe()
        {
            switch (Kind)
            {
                case BindingKind.Button:
                    return Name;
                case BindingKind.Axis:
                    return Positive ? Name : Name + " (-)";
                case BindingKind.LegacyKey:
                    const string prefix = "JoystickButton";
                    return Name.StartsWith(prefix, StringComparison.Ordinal) ? "Button " + Name.Substring(prefix.Length) : Name;
                default:
                    return "Not set (click to bind)";
            }
        }

        public bool Matches(string elementName)
        {
            return (Kind == BindingKind.Button || Kind == BindingKind.Axis) && NamesMatch(Name, elementName);
        }

        public static bool NamesMatch(string a, string b)
        {
            if (a == null || b == null)
            {
                return false;
            }

            a = a.Trim();
            b = b.Trim();
            if (string.Equals(a, b, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            foreach (string[] group in EquivalentNames)
            {
                if (InGroup(group, a) && InGroup(group, b))
                {
                    return true;
                }
            }

            return false;
        }

        // True on the frame an axis moves from below the threshold to at or above it.
        public static bool IsPressEdge(float previous, float current, bool positive)
        {
            float before = positive ? previous : -previous;
            float now = positive ? current : -current;
            return now >= PressThreshold && before < PressThreshold;
        }

        private static bool InGroup(string[] group, string name)
        {
            foreach (string candidate in group)
            {
                if (string.Equals(candidate, name, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
