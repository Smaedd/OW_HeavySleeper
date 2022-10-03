using OWML.Common;
using HarmonyLib;
using UnityEngine;
using System;

namespace HeavySleeper
{
    [HarmonyPatch]
    public static class Patches
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

        [HarmonyPrefix]
        [HarmonyPatch(typeof(TranslatorWord), MethodType.Constructor)]
        [HarmonyPatch(new Type[] { typeof(string), typeof(int), typeof(int), typeof(bool), typeof(float) })]
        private static void TranslatorFix(ref string translatedText)
        {
            if (!HeavySleeper.Instance._lightSleeperMode)
                return;

            float timeLoopSec = TimeLoop._loopDuration; // 1320 by default
            float timeLoopMin = timeLoopSec / 60f;      // 22 by default

            float elapsedSec = TimeLoop.GetSecondsElapsed();
            float origElapsedSec = elapsedSec + HeavySleeper.DEFAULT_LOOP_DURATION - timeLoopSec;

            float elapsedMin = TimeLoop.GetMinutesElapsed();

            const float sinceSolarSec = 2501f;
            const float redGiantTimeSec = 690f;

            if (translatedText.Contains("<"))
            {
                string timeString = string.Concat(Mathf.Floor(elapsedMin));
                translatedText = translatedText.Replace("<TimeMinutes>", timeString);

                timeString = string.Concat(timeLoopMin - Mathf.Floor(elapsedMin));
                translatedText = translatedText.Replace("<TimeMinutesRemaining>", timeString);

                timeString = string.Concat(Mathf.Floor((elapsedSec + sinceSolarSec) / 60f));
                translatedText = translatedText.Replace("<TimeMinutesSolarActivity>", timeString);

                timeString = string.Concat((int)elapsedSec % 60);
                translatedText = translatedText.Replace("<TimeSeconds>", timeString);

                // Remaining = Red giant (need to be offset as scale is changed in light sleeper mode)
                timeString = string.Concat(Mathf.Max(0f, Mathf.Floor((redGiantTimeSec - origElapsedSec) / 60f)));
                translatedText = translatedText.Replace("<RemainingMinutes>", timeString);

                timeString = string.Concat(Mathf.Max(0f, (redGiantTimeSec - Mathf.Floor(origElapsedSec)) % 60f));
                translatedText = translatedText.Replace("<RemainingSeconds>", timeString);

                // These 2 are actually referring to collapse, not red giant
                timeString = string.Concat(timeLoopMin - Mathf.Floor(elapsedMin));
                translatedText = translatedText.Replace("<MinutesToRedGiant>", timeString);

                timeString = string.Concat((timeLoopSec - Mathf.Floor(elapsedSec)) % 60f);
                translatedText = translatedText.Replace("<SecondsToRedGiant>", timeString);

                timeString = string.Concat(Mathf.Floor((origElapsedSec - redGiantTimeSec) / 60f));
                translatedText = translatedText.Replace("<MinutesSinceRedGiant>", timeString);

                timeString = string.Concat((Mathf.Floor(origElapsedSec) - redGiantTimeSec) % 60f);
                translatedText = translatedText.Replace("<SecondsSinceRedGiant>", timeString);
            }
        }
    }
}
