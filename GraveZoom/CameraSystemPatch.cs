using HarmonyLib;
using LazyBearTechnology;
using UnityEngine;

namespace GraveZoom
{
    // The game computes orthographicSize = height / (100 * pixelSize). Our own
    // zoom is a plain percent where 100 reproduces that exactly and larger
    // means more zoomed in: orthoSize = res.y / (pixelSize * percent).
    // (At percent=100 this must equal res.y/(100*pixelSize) - check the algebra
    // again before touching this line, a previous version had an extra *100 here.)
    [HarmonyPatch(typeof(CameraSystem), nameof(CameraSystem.OnResolutionChanged))]
    internal static class CameraSystem_OnResolutionChanged_Patch
    {
        private static IntVector2? lastKnownResolution;

        private static void Postfix(CameraSystem __instance, IntVector2 res)
        {
            // A real resolution change (not just us re-applying zoom) -> back to 100%
            // for the new resolution, instead of leaving the previous resolution's value.
            if (lastKnownResolution == null || lastKnownResolution.Value.x != res.x || lastKnownResolution.Value.y != res.y)
            {
                lastKnownResolution = res;
                Plugin.ZoomFactor.Value = 100f;
            }

            int nativePixelSize = __instance.ResolutionPiexelSize;
            float orthographicSize = res.y / (nativePixelSize * ZoomController.EffectiveZoom);

            __instance.SetOrthographicSize(orthographicSize);

            Camera worldCamera = __instance.WorldCamera;
            if (worldCamera != null)
            {
                worldCamera.orthographicSize = orthographicSize;
            }

            __instance.MainCamera?.OnResolutionChanged(res.x, res.y);
        }
    }
}
