using System.IO;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using Xunit;

namespace GraveZoom.Tests;

// The optional GK2 Framework bridge is a separate plugin that cannot share constants with
// the main mod, so these tests catch the two drifting apart.
public class VersionConsistencyTests
{
    [Fact]
    public void BridgeVersion_MatchesTheMainModVersion()
    {
        Assert.Equal(
            Constant("GraveZoom/Plugin.cs", "PluginVersion"),
            Constant("GraveZoom.Framework/FrameworkBridgePlugin.cs", "PluginVersion"));
    }

    [Fact]
    public void BridgeDependsOnTheMainModsGuid()
    {
        Assert.Equal(
            Constant("GraveZoom/Plugin.cs", "PluginGuid"),
            Constant("GraveZoom.Framework/FrameworkBridgePlugin.cs", "GraveZoomGuid"));
    }

    private static string Constant(string repoRelativeFile, string name, [CallerFilePath] string thisFile = "")
    {
        string repoRoot = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(thisFile), "..", ".."));
        string source = File.ReadAllText(Path.Combine(repoRoot, repoRelativeFile));
        Match match = Regex.Match(source, "const string " + name + " = \"([^\"]+)\"");
        Assert.True(match.Success, name + " not found in " + repoRelativeFile);
        return match.Groups[1].Value;
    }
}
