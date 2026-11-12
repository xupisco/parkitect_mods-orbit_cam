using System.Reflection;
using HarmonyLib;
using Moona;
using UnityEngine;

namespace PerspectiveCamera
{
    /// <summary>
    /// The game's audio places sounds by where they are on screen, which suits its own camera, high up and looking down:
    /// what is on screen is close. From this camera's lower angle, a ride far away near the horizon is on screen too and
    /// plays loud, and a sound behind the camera lands mirrored on screen. While this camera is on, sounds are placed by
    /// their real distance and by the side they are on. Anything up to as far as the spot the camera looks at plays as
    /// the game would, with the game's own mix by zoom, and only what lies beyond it fades.
    /// </summary>
    [HarmonyPatch]
    internal static class PerspectiveCameraAudio
    {
        // In tiles past the spot the camera looks at: sounds this much further still play at full volume, and fade out
        // over the next FadeDistance.
        private const float FullVolumeDistance = 3f;
        private const float FadeDistance = 30f;
        // Sounds to one side are panned only this much of the way, so nothing plays in one ear alone.
        private const float PanAmount = 0.7f;

        private static MethodBase TargetMethod()
        {
            return AccessTools.Method(typeof(MFlatPanning), "UpdateFlatPosition");
        }

        private static bool Prefix(MInstance target, Vector3 position, ref MInstance.FunctionResult __result)
        {
            // Only for this camera: the game's own camera (F10) keeps the game's placement, and POV Cam turns this
            // camera off and places sounds itself.
            PerspectiveCamera camera = PerspectiveCamera.Current;
            if (camera == null || !camera.isActiveAndEnabled || camera.Camera == null)
            {
                return true;
            }

            Transform listener = camera.Camera.transform;
            Vector3 offset = position - listener.position;
            float tiles = offset.magnitude;
            float focus = camera.CameraTarget != null ? Vector3.Distance(listener.position, camera.CameraTarget.position) : 0f;
            float beyond = tiles - focus - FullVolumeDistance;
            double distance = Mathf.Clamp01(beyond / FadeDistance) * MManager.Config.FlatMaxDistance;
            float side = tiles > 0.001f ? Vector3.Dot(offset / tiles, listener.right) : 0f;
            float pan = MFlatPanning.ViewportToPan(0.5f + 0.5f * side * PanAmount);

            MInstanceParameters parameters = MRtpcManager.GetInstanceParameter(target);
            if (parameters.Distance == distance && parameters.Pan == pan)
            {
                __result = MInstance.FunctionResult.NoActionNeeded;
                return false;
            }
            parameters.Distance = distance;
            parameters.Pan = pan;
            parameters.IsDirty = true;
            __result = MInstance.FunctionResult.UpdateRequested;
            return false;
        }
    }
}
