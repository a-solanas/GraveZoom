// Minimal copy of the tag class BepInEx.ConfigurationManager reads (by field name)
// to customise how a setting is drawn. See its ConfigurationManagerAttributes.cs template.
#pragma warning disable 0169, 0414, 0649
internal sealed class ConfigurationManagerAttributes
{
    public CustomHotkeyDrawerFunc CustomHotkeyDrawer;

    public delegate void CustomHotkeyDrawerFunc(BepInEx.Configuration.ConfigEntryBase setting, ref bool isCurrentlyAcceptingInput);
}
