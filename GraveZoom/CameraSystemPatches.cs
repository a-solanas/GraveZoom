using HarmonyLib;

namespace GraveZoom
{
    // The game derives every camera size (camera, virtual cameras, chunk visibility,
    // render targets) from this one function, so scaling its result is enough.
    // vanilla = height / (100 * pixelSize); at zoom 100 the scale is exactly 1.
    [HarmonyPatch(typeof(CameraSystem), nameof(CameraSystem.CalculateOrthographicSize))]
    internal static class CameraSystemPatches
    {
        private static void Postfix(ref float __result)
        {
            __result *= ZoomMath.OrthoScale(ZoomController.EffectiveZoom);
        }
    }
}
