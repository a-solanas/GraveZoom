using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace GraveZoom.Tests;

public class ZoomMathTests
{
    [Fact]
    public void BuildStops_Native1440_WithSpecifiedHeights_MatchesExpected()
    {
        // Test case: native 1440, heights "720,900,1080,1200,1440,1600,2160", min 25, max 400, step 25
        var stops = ZoomMath.BuildStops(1440, new[] { 720, 900, 1080, 1200, 1440, 1600, 2160 }, 25, 400, 25);

        var expected = new[] { 25f, 50f, 67f, 75f, 90f, 100f, 120f, 125f, 133f, 150f, 160f, 175f, 200f, 225f, 250f, 275f, 300f, 325f, 350f, 375f, 400f };

        Assert.Equal(expected.Length, stops.Count);
        for (int i = 0; i < expected.Length; i++)
        {
            Assert.True(Math.Abs(stops[i] - expected[i]) < ZoomMath.StepTolerance,
                $"Index {i}: expected {expected[i]}, got {stops[i]}");
        }
    }

    [Fact]
    public void BuildStops_Native2160_4K_ContainsZoomOut()
    {
        // REGRESSION: with native 2160 (4K), list must contain values below 100 (75, 50, 25)
        var stops = ZoomMath.BuildStops(2160, new[] { 720, 900, 1080, 1200, 1440, 1600, 2160 }, 25, 400, 25);

        Assert.Contains(25f, stops);
        Assert.Contains(50f, stops);
        Assert.Contains(75f, stops);
        Assert.True(stops.All(s => s >= 25 && s <= 400), "All stops must be within [min, max]");
    }

    [Fact]
    public void NextStop_From100_GoingDown_Returns75()
    {
        var stops = ZoomMath.BuildStops(2160, new[] { 720, 900, 1080, 1200, 1440, 1600, 2160 }, 25, 400, 25);
        var nextStop = ZoomMath.NextStop(stops, 100, higher: false);

        Assert.Equal(75, nextStop);
    }

    [Fact]
    public void NextStop_From150_3_GoingUp_Returns160()
    {
        var stops = ZoomMath.BuildStops(1440, new[] { 720, 900, 1080, 1200, 1440, 1600, 2160 }, 25, 400, 25);
        var nextStop = ZoomMath.NextStop(stops, 150.3f, higher: true);

        Assert.Equal(160, nextStop);
    }

    [Fact]
    public void NextStop_From150_3_GoingDown_Returns150()
    {
        var stops = ZoomMath.BuildStops(1440, new[] { 720, 900, 1080, 1200, 1440, 1600, 2160 }, 25, 400, 25);
        var nextStop = ZoomMath.NextStop(stops, 150.3f, higher: false);

        Assert.Equal(150, nextStop);
    }

    [Fact]
    public void NextStop_ExactlyOnAStop_MovesToTheNeighbour()
    {
        var stops = ZoomMath.BuildStops(1440, new[] { 720, 900, 1080, 1200, 1440, 1600, 2160 }, 25, 400, 25);

        Assert.Equal(133, ZoomMath.NextStop(stops, 150, higher: false));
        Assert.Equal(160, ZoomMath.NextStop(stops, 150, higher: true));
    }

    [Fact]
    public void NextStop_WithinToleranceOfAStop_TreatsItAsThatStop()
    {
        var stops = ZoomMath.BuildStops(1440, new[] { 720, 900, 1080, 1200, 1440, 1600, 2160 }, 25, 400, 25);

        Assert.Equal(133, ZoomMath.NextStop(stops, 150.005f, higher: false));
        Assert.Equal(160, ZoomMath.NextStop(stops, 149.995f, higher: true));
    }

    [Fact]
    public void NextStop_AtMaxGoingUp_ReturnsNull()
    {
        var stops = ZoomMath.BuildStops(1440, new[] { 720, 900, 1080, 1200, 1440, 1600, 2160 }, 25, 400, 25);
        var nextStop = ZoomMath.NextStop(stops, 400, higher: true);

        Assert.Null(nextStop);
    }

    [Fact]
    public void NextStop_AtMinGoingDown_ReturnsNull()
    {
        var stops = ZoomMath.BuildStops(1440, new[] { 720, 900, 1080, 1200, 1440, 1600, 2160 }, 25, 400, 25);
        var nextStop = ZoomMath.NextStop(stops, 25, higher: false);

        Assert.Null(nextStop);
    }

    [Fact]
    public void NextStop_BelowMinGoingUp_Returns25()
    {
        var stops = ZoomMath.BuildStops(1440, new[] { 720, 900, 1080, 1200, 1440, 1600, 2160 }, 25, 400, 25);
        var nextStop = ZoomMath.NextStop(stops, 10, higher: true);

        Assert.Equal(25, nextStop);
    }

    [Fact]
    public void BuildStops_Step0_DisablesGrid()
    {
        // Step 0 disables the grid (only tier stops + 100/min/max)
        var stops = ZoomMath.BuildStops(1440, new[] { 720, 900, 1080, 1200, 1440, 1600, 2160 }, 25, 400, 0);

        // Should contain tier stops, 100, min, and max
        Assert.Contains(100, stops);
        Assert.Contains(25, stops);
        Assert.Contains(400, stops);
        // Should not have many grid stops
        Assert.True(stops.Count <= 20, "Step 0 should have few stops (tier + 100/min/max)");
    }

    [Fact]
    public void BuildStops_TinyStep_BehavesLikeStep1()
    {
        // Tiny step 0.01 behaves like step 1 and finishes fast
        var stops = ZoomMath.BuildStops(1440, new[] { 720, 900, 1080, 1200, 1440, 1600, 2160 }, 25, 400, 0.01f);

        // Should be treated like step 1 and complete quickly
        Assert.NotEmpty(stops);
        Assert.True(stops.All(s => s >= 25 && s <= 400));
    }

    [Fact]
    public void BuildStops_MinGreaterThanMax_Swapped()
    {
        // min>max are swapped
        var stops1 = ZoomMath.BuildStops(1440, new[] { 720, 900, 1080 }, 25, 400, 25);
        var stops2 = ZoomMath.BuildStops(1440, new[] { 720, 900, 1080 }, 400, 25, 25);

        Assert.Equal(stops1.Count, stops2.Count);
        for (int i = 0; i < stops1.Count; i++)
        {
            Assert.Equal(stops1[i], stops2[i]);
        }
    }

    [Fact]
    public void BuildStops_IsSorted()
    {
        var stops = ZoomMath.BuildStops(1440, new[] { 720, 900, 1080, 1200, 1440, 1600, 2160 }, 25, 400, 25);

        for (int i = 1; i < stops.Count; i++)
        {
            Assert.True(stops[i] > stops[i - 1], $"Stops not sorted at index {i}: {stops[i - 1]} >= {stops[i]}");
        }
    }

    [Fact]
    public void BuildStops_IsDistinct()
    {
        var stops = ZoomMath.BuildStops(1440, new[] { 720, 900, 1080, 1200, 1440, 1600, 2160 }, 25, 400, 25);
        var uniqueStops = new HashSet<float>(stops);

        Assert.Equal(stops.Count, uniqueStops.Count);
    }

    [Fact]
    public void BuildStops_AllWithinMinMax()
    {
        var stops = ZoomMath.BuildStops(1440, new[] { 720, 900, 1080, 1200, 1440, 1600, 2160 }, 25, 400, 25);

        Assert.True(stops.All(s => s >= 25 && s <= 400), "All stops must be within [min, max]");
    }

    [Fact]
    public void ClampZoom_NaN_Returns100()
    {
        var result = ZoomMath.ClampZoom(float.NaN, 25, 400);

        Assert.Equal(100, result);
    }

    [Fact]
    public void ClampZoom_Negative_Returns100()
    {
        var result = ZoomMath.ClampZoom(-5, 25, 400);

        Assert.Equal(100, result);
    }

    [Fact]
    public void ClampZoom_Zero_Returns100()
    {
        var result = ZoomMath.ClampZoom(0, 25, 400);

        Assert.Equal(100, result);
    }

    [Fact]
    public void ClampZoom_BelowMin_ClampsToMin()
    {
        var result = ZoomMath.ClampZoom(1, 25, 400);

        Assert.Equal(25, result);
    }

    [Fact]
    public void ClampZoom_AboveMax_ClampsToMax()
    {
        var result = ZoomMath.ClampZoom(9999, 25, 400);

        Assert.Equal(400, result);
    }

    [Fact]
    public void ClampZoom_WithinRange_ReturnsValue()
    {
        var result = ZoomMath.ClampZoom(150, 25, 400);

        Assert.Equal(150, result);
    }

    [Fact]
    public void OrthoScale_100Percent_Returns1()
    {
        var result = ZoomMath.OrthoScale(100);

        Assert.Equal(1, result);
    }

    [Fact]
    public void OrthoScale_200Percent_Returns0_5()
    {
        var result = ZoomMath.OrthoScale(200);

        Assert.Equal(0.5f, result);
    }

    [Fact]
    public void OrthoScale_50Percent_Returns2()
    {
        var result = ZoomMath.OrthoScale(50);

        Assert.Equal(2, result);
    }

    [Theory]
    [InlineData(100, 1)]
    [InlineData(200, 0.5f)]
    [InlineData(50, 2)]
    [InlineData(25, 4)]
    [InlineData(400, 0.25f)]
    public void OrthoScale_VariousValues(float zoomPercent, float expected)
    {
        var result = ZoomMath.OrthoScale(zoomPercent);

        Assert.Equal(expected, result, 4);
    }

    [Fact]
    public void ParseHeights_ValidValues_ReturnsFiltered()
    {
        var result = ZoomMath.ParseHeights("720, 900,abc,,-5,0,1080");

        Assert.Equal(new[] { 720, 900, 1080 }, result);
    }

    [Fact]
    public void ParseHeights_Null_ReturnsEmpty()
    {
        var result = ZoomMath.ParseHeights(null!);

        Assert.Empty(result);
    }

    [Fact]
    public void ParseHeights_Empty_ReturnsEmpty()
    {
        var result = ZoomMath.ParseHeights("");

        Assert.Empty(result);
    }

    [Fact]
    public void ParseHeights_OnlyGarbage_ReturnsEmpty()
    {
        var result = ZoomMath.ParseHeights("abc,def,xyz");

        Assert.Empty(result);
    }

    [Fact]
    public void ParseHeights_Mixed_FiltersCorrectly()
    {
        var result = ZoomMath.ParseHeights("1,2,3,-1,-2,-3,0,abc");

        Assert.Equal(new[] { 1, 2, 3 }, result);
    }

    [Fact]
    public void BuildStops_IncludesNativePercent()
    {
        var stops = ZoomMath.BuildStops(1440, new[] { 720, 900, 1080 }, 25, 400, 25);

        Assert.Contains(ZoomMath.NativePercent, stops);
    }
}
