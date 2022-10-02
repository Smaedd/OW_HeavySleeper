using OWML.Common;
using HarmonyLib;
using UnityEngine;

namespace HeavySleeper
{
    [HarmonyPatch]
    public static class SunPatch
    {
        private const float DEFAULT_LOOP_MINUTES = HeavySleeper.DEFAULT_LOOP_DURATION / 60f;
        private static float SunMinutesElapsed()
        {
            // 'Elapsed' = RealElapsed + (Default - Duration)
            return TimeLoop.GetMinutesElapsed() + (DEFAULT_LOOP_MINUTES - TimeLoop.GetLoopDuration() / 60f);
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(SunController), nameof(SunController.Update))]
        private static bool SunVisualFix(SunController __instance)
        {
            // We only need to fix visuals if light sleeper is on
            if (!HeavySleeper.Instance._lightSleeperMode)
                return true;

            // We only want to change the default, non-supernova state.
            if (__instance._supernovaStarted || __instance._collapseStarted)
                return true;

            // Recreate this path using new elapsed counter
            float elapsed = SunMinutesElapsed();

            float progression = Mathf.InverseLerp(__instance._progressionStartTime, __instance._progressionEndTime, elapsed);
            float scaleProg = Mathf.InverseLerp(__instance._scaleStartTime, __instance._scaleEndTime, elapsed);
            float scale = Mathf.Lerp(1f, __instance._endScale, Mathf.SmoothStep(0f, 1f, scaleProg));

            __instance.UpdateScale(scale);

            Color atmosphereColor = __instance._atmosphereColor.Evaluate(progression).linear;
            __instance._atmosphereMaterial.SetColor(__instance._propID_SkyColor, atmosphereColor);
            __instance._sunProxyEffects.UpdateAtmosphereColor(atmosphereColor);
            __instance._fogMaterial.SetColor(__instance._propID_Tint, __instance._atmosphereColor.Evaluate(progression));
            __instance._fog.fogTint = __instance._atmosphereColor.Evaluate(progression);
            __instance._surfaceMaterial.Lerp(__instance._startSurfaceMaterial, __instance._endSurfaceMaterial, progression);
            __instance._solarFlareEmitter.tint = __instance._solarFlareTint.Evaluate(progression);
            __instance._sunLight.sunIntensity = Mathf.Lerp(__instance._sunLightIntensity, __instance._endLightIntensity, progression);
            __instance._sunLight.sunColor = __instance._lightColor.Evaluate(progression);

            // All complete, we can skip original function.
            return false;
        }
    }
}
