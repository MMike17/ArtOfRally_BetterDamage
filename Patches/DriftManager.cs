using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

using static EventStatusEnums;
using Random = UnityEngine.Random;

namespace BetterDamage
{
    [HarmonyPatch(typeof(Wheel), "FixedUpdate")]
    static class DriftManager
    {
        const float DRIFT_SLIP_THRESHOLD = 0.9f;
        const float DRIFT_DURATION_THRESHOLD = 20;
        const float DRIFT_PUNCTURE_COOLDOWN = 60 * 5;

        static Dictionary<Wheel, float> wheelDriftDuration;
        static Dictionary<Wheel, bool> wheelInCooldown;

        static void Postfix(Wheel __instance)
        {
            if (!Main.enabled || GameEntryPoint.EventManager.status != EventStatus.UNDERWAY || !Main.settings.enableDriftDamage)
                return;

            Main.Try(() =>
            {
                if (wheelDriftDuration == null)
                {
                    wheelDriftDuration = new Dictionary<Wheel, float>();
                    wheelInCooldown = new Dictionary<Wheel, bool>();
                }

                if (!wheelDriftDuration.ContainsKey(__instance))
                {
                    wheelDriftDuration.Add(__instance, 0);
                    wheelInCooldown.Add(__instance, false);

                    // clean up dictionary
                    foreach (Wheel wheel in wheelDriftDuration.Keys.ToArray())
                    {
                        if (wheel == null)
                        {
                            wheelDriftDuration.Remove(wheel);
                            wheelInCooldown.Remove(wheel);
                        }
                    }
                }

                if (__instance.lateralSlip * (__instance.lateralSlip > 0 ? 1 : -1) >= DRIFT_SLIP_THRESHOLD)
                    wheelDriftDuration[__instance] += Time.fixedDeltaTime;

                if (wheelInCooldown[__instance])
                {
                    if (wheelDriftDuration[__instance] >= DRIFT_PUNCTURE_COOLDOWN)
                    {
                        wheelDriftDuration[__instance] -= DRIFT_PUNCTURE_COOLDOWN;
                        wheelInCooldown[__instance] = false;
                    }
                }
                else if (wheelDriftDuration[__instance] >= DRIFT_DURATION_THRESHOLD)
                {
                    wheelDriftDuration[__instance] -= DRIFT_DURATION_THRESHOLD;

                    if (Random.Range(0f, 100f) < Main.settings.driftPunctureProbability)
                    {
                        // check if another tire is punctured
                        bool hasPuncture = false;

                        foreach (Wheel wheel in wheelDriftDuration.Keys)
                        {
                            if (wheel.tirePuncture)
                            {
                                hasPuncture = true;
                                break;
                            }
                        }

                        if (!hasPuncture)
                        {
                            CarUtils.PunctureTire(
                                GameEntryPoint.EventManager.playerManager.PlayerObject.GetComponent<PlayerCollider>(),
                                __instance
                            );

                            wheelInCooldown[__instance] = true;
                        }
                    }
                }
            });
        }

        public static void Reset()
        {
            wheelDriftDuration = null;
            wheelInCooldown = null;
        }
    }

    [HarmonyPatch(typeof(StageScreen), nameof(StageScreen.Restart))]
    static class RestartStagePatch
    {
        static void Postfix()
        {
            if (!Main.enabled || !Main.settings.enableDriftDamage)
                return;

            Main.Try(() => DriftManager.Reset());
        }
    }

    [HarmonyPatch(typeof(PreStageScreen), nameof(PreStageScreen.StartStage))]
    static class StartStagePatch
    {
        static void Postfix()
        {
            if (!Main.enabled || !Main.settings.enableDriftDamage)
                return;

            Main.Try(() => DriftManager.Reset());
        }
    }
}