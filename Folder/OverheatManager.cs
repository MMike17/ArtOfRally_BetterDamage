using HarmonyLib;
using UnityEngine;

using static RepairsManagerUI;

namespace BetterDamage
{
    [HarmonyPatch(typeof(Drivetrain))]
    static class OverheatManager
    {
        const float MAX_OVERHEAT = 1.5f;
        const float ENGINE_DAMAGE_RATE = 0.02f;

        static PlayerCollider player;
        static float overheatRPMThreshold;
        static float overheatCount;

        [HarmonyPatch("FixedUpdate")]
        static void Postfix(Drivetrain __instance)
        {
            if (!Main.enabled || !Main.settings.enableOverheatDamage)
                return;

            Main.Try(() =>
            {
                GenerateIfNeeded(__instance);

                if (__instance.rpm >= overheatRPMThreshold)
                {
                    overheatCount = Mathf.MoveTowards(overheatCount, MAX_OVERHEAT, Time.fixedDeltaTime);
                }
                else
                {
                    float radiatorCondition = GameEntryPoint.EventManager.playerManager.performanceDamageManager
                        .GetConditionOfPart(SystemToRepair.RADIATOR);

                    overheatCount = Mathf.MoveTowards(overheatCount, 0,
                        Time.fixedDeltaTime * Main.settings.overheatCooldownSpeedMult * (0.1f + radiatorCondition));
                }

                if (overheatCount >= MAX_OVERHEAT)
                {
                    CarUtils.DamagePart(player, ENGINE_DAMAGE_RATE * Time.fixedDeltaTime, SystemToRepair.ENGINE);
                }
            });
        }

        static void GenerateIfNeeded(Drivetrain engine)
        {
            if (player == null)
            {
                player = GameEntryPoint.EventManager.playerManager.PlayerObject.GetComponent<PlayerCollider>();
                overheatCount = 0;

                Refresh();
            }
        }

        public static void Refresh()
        {
            if (player == null)
                return;

            overheatRPMThreshold = GameEntryPoint.EventManager.playerManager.drivetrain.maxRPM *
                (100 - Main.settings.overheatRPMThresholdPercent) / 100;
        }

        // __ Overheat __
        // currentRev > ProjectForward(maxCarSpeed / 10) /*find the right ratio*/ (turbo)

        // TODO : Add map-based overheat and cooldown speed
        // TODO : Add turbo damage

        // __ what do I need ? __
        // car rigidbody (forward speed)
    }
}