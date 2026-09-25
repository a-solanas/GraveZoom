using GraveZoom;
using Xunit;

namespace GraveZoom.Tests;

public class ControllerBindingTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("None")]
    [InlineData("none")]
    [InlineData("Button:")]
    [InlineData("Axis:")]
    [InlineData("Axis::+")]
    public void Parse_EmptyOrIncomplete_IsNone(string text)
    {
        Assert.Equal(BindingKind.None, ControllerBinding.Parse(text).Kind);
    }

    [Fact]
    public void None_IsUnbound_AndSavesAsNone()
    {
        Assert.Equal(BindingKind.None, ControllerBinding.None.Kind);
        Assert.Equal("None", ControllerBinding.None.ToString());
    }

    [Fact]
    public void Parse_Button_KeepsTheElementName()
    {
        var binding = ControllerBinding.Parse("Button:Right Shoulder");

        Assert.Equal(BindingKind.Button, binding.Kind);
        Assert.Equal("Right Shoulder", binding.Name);
    }

    [Theory]
    [InlineData("Axis:Right Trigger:+", true)]
    [InlineData("Axis:Right Trigger:-", false)]
    [InlineData("Axis:Right Trigger", true)]
    public void Parse_Axis_ReadsTheDirection(string text, bool positive)
    {
        var binding = ControllerBinding.Parse(text);

        Assert.Equal(BindingKind.Axis, binding.Kind);
        Assert.Equal("Right Trigger", binding.Name);
        Assert.Equal(positive, binding.Positive);
    }

    [Fact]
    public void Parse_OldUnityKeyCode_IsALegacyBinding()
    {
        var binding = ControllerBinding.Parse("JoystickButton5");

        Assert.Equal(BindingKind.LegacyKey, binding.Kind);
        Assert.Equal("JoystickButton5", binding.Name);
    }

    [Theory]
    [InlineData("None")]
    [InlineData("Button:Square")]
    [InlineData("Axis:Left Trigger:+")]
    [InlineData("Axis:Left Stick X:-")]
    [InlineData("JoystickButton4")]
    public void ToString_RoundTrips(string text)
    {
        Assert.Equal(text, ControllerBinding.Parse(text).ToString());
    }

    [Theory]
    [InlineData("None", "Not set (click to bind)")]
    [InlineData("Button:Square", "Square")]
    [InlineData("Axis:Right Trigger:+", "Right Trigger")]
    [InlineData("Axis:Left Stick X:-", "Left Stick X (-)")]
    [InlineData("JoystickButton5", "Button 5")]
    [InlineData("Joystick1Button3", "Joystick1Button3")]
    public void Describe_ShowsAFriendlyLabel(string text, string expected)
    {
        Assert.Equal(expected, ControllerBinding.Parse(text).Describe());
    }

    [Theory]
    [InlineData("Right Shoulder", "R1")]
    [InlineData("R1", "RB")]
    [InlineData("Right Bumper", "right shoulder")]
    [InlineData("Left Trigger", "L2")]
    [InlineData("Right Trigger", "rt")]
    [InlineData("Right Shoulder 1", "RB")]
    [InlineData("Left Shoulder 1", "Left Shoulder")]
    [InlineData("Right Shoulder 2", "Right Trigger")]
    [InlineData("Left Shoulder 2", "L2")]
    [InlineData("Right Trigger", "ZR")]
    [InlineData("zl", "Left Trigger")]
    [InlineData("R", "Right Shoulder")]
    [InlineData("L", "L1")]
    [InlineData("A", "a")]
    [InlineData("  Square ", "square")]
    public void NamesMatch_TreatsEquivalentNamesAsTheSameControl(string a, string b)
    {
        Assert.True(ControllerBinding.NamesMatch(a, b));
        Assert.True(ControllerBinding.NamesMatch(b, a));
    }

    [Theory]
    [InlineData("Right Shoulder", "Left Shoulder")]
    [InlineData("Right Shoulder", "Right Trigger")]
    [InlineData("Right Shoulder", "Right Shoulder 2")]
    [InlineData("R", "ZR")]
    [InlineData("ZL", "ZR")]
    [InlineData("R1", "L1")]
    [InlineData("A", "B")]
    [InlineData(null, "A")]
    [InlineData("A", null)]
    public void NamesMatch_KeepsDifferentControlsApart(string a, string b)
    {
        Assert.False(ControllerBinding.NamesMatch(a, b));
    }

    [Fact]
    public void Matches_OnlyAppliesToButtonAndAxisBindings()
    {
        Assert.True(ControllerBinding.Parse("Button:R1").Matches("Right Shoulder"));
        Assert.True(ControllerBinding.Parse("Axis:Right Trigger:+").Matches("R2"));
        Assert.False(ControllerBinding.Parse("JoystickButton5").Matches("JoystickButton5"));
        Assert.False(ControllerBinding.Parse("None").Matches("A"));
    }

    [Theory]
    [InlineData(0f, 0.7f, true, true)]
    [InlineData(0f, 0.6f, true, true)]
    [InlineData(0f, 0.59f, true, false)]
    [InlineData(0.7f, 0.9f, true, false)]
    [InlineData(-1f, 0.7f, true, true)]
    [InlineData(0f, -0.7f, false, true)]
    [InlineData(0f, -0.7f, true, false)]
    [InlineData(-0.7f, -0.9f, false, false)]
    public void IsPressEdge_FiresOnlyOnTheFrameTheThresholdIsCrossed(float previous, float current, bool positive, bool expected)
    {
        Assert.Equal(expected, ControllerBinding.IsPressEdge(previous, current, positive));
    }
}
