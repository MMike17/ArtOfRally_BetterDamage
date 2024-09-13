using HarmonyLib;
using System.Reflection;
using UnityEngine;

namespace BetterDamage
{
    [HarmonyPatch(typeof(SteeringPerfomanceDamage), nameof(SteeringPerfomanceDamage.SetPerformance))]
    static class SteeringDamageManager
    {
        static float lastDamage;

        static bool Prefix(SteeringPerfomanceDamage __instance)
        {
            if (!Main.enabled || Main.InReplay)
                return true;

            Main.Try(() =>
            {
                float tilt = Main.GetField<float, SteeringPerfomanceDamage>(__instance, "steeringAlignmentEffect", BindingFlags.Instance);

                if (__instance.PerformanceCondition == PerformanceDamage.MAX_PERFORMANCE_CONDITION)
                    Main.SetField<float, SteeringPerfomanceDamage>(__instance, "steeringAlignmentEffect", BindingFlags.Instance, 0);
                else
                {
                    float currentDamage = PerformanceDamage.MAX_PERFORMANCE_CONDITION - __instance.PerformanceCondition;

                    if (CrashDamageManager.tiltToApply == 0 && lastDamage == currentDamage)
                        return;

                    // mapped -1 to 1
                    float tiltPercent = lastDamage == 0 ? 0 : tilt * 20 / lastDamage;
                    tiltPercent = Mathf.Clamp(tiltPercent + CrashDamageManager.tiltToApply, -1, 1);
                    tilt = Mathf.Clamp(tiltPercent * currentDamage / 10, -0.1f, 0.1f) * 0.5f;

                    if (!Main.settings.disableInfoLogs)
                        Main.Log("Applied tilt : " + tilt);

                    // aplly
                    Main.SetField<float, SteeringPerfomanceDamage>(__instance, "steeringAlignmentEffect", BindingFlags.Instance, tilt);
                    GameEntryPoint.EventManager.playerManager.PlayerObject.GetComponent<AxisCarController>().SteeringOutOfAlignmentEffect = tilt;

                    CrashDamageManager.tiltToApply = 0;
                    lastDamage = currentDamage;
                }
            });

            return false;
        }

        public static void Reset() => lastDamage = 0;
    }

    [HarmonyPatch(typeof(PerformanceDamage), nameof(PerformanceDamage.Repair))]
    static class SteeringRepairPatch
    {
        static void Prefix(PerformanceDamage __instance)
        {
            if (!Main.enabled || Main.InReplay || !(__instance is SteeringPerfomanceDamage))
                return;

            Main.Try(() =>
            {
                Main.SetField<float, SteeringPerfomanceDamage>(
                    __instance as SteeringPerfomanceDamage,
                    "steeringAlignmentEffect",
                    BindingFlags.Instance,
                    0
                );

                SteeringDamageManager.Reset();
            });
        }
    }
}