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
            if (!Main.enabled)
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
                    Main.Log("Current tilt percent : " + tiltPercent);
                    tiltPercent = Mathf.Clamp(tiltPercent + CrashDamageManager.tiltToApply, -1, 1);
                    Main.Log("New tilt percent : " + tiltPercent);
                    tilt = Mathf.Clamp(tiltPercent * currentDamage / 10, -0.1f, 0.1f) * 0.5f;

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
            if (!Main.enabled || !(__instance is SteeringPerfomanceDamage))
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