using HarmonyLib;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

using static RepairsManagerUI;

namespace BetterDamage
{
    [HarmonyPatch(typeof(Drivetrain))]
    static class OverheatManager
    {
        const float REFERENCE_TEMPERATURE = 15f;
        const float MAX_OVERHEAT = 2f;
        const float ENGINE_DAMAGE_RATE = 0.03f;

        static List<(int, int, float)> mapHeatMultipliers = new List<(int, int, float)>()
        {
            (9, 14, 3.24f), // Finland
            (15, 20, 22.6f), // Sardinia
            (21, 26, 12.4f), // Japan
            (27, 32, 2.8f), // Norway
            (33, 38, 10.8f), // Germany
            (39, 44, 25.2f), // Kenya
            (46, 51, 25.9f) // Indonesia
        };

        static PlayerCollider player;
        static float overheatRPMThreshold;
        public static float overheatCount;
        static float mapMultiplier;

        [HarmonyPatch("FixedUpdate")]
        static void Postfix(Drivetrain __instance)
        {
            if (!Main.enabled || !Main.settings.enableOverheatDamage)
                return;

            Main.Try(() =>
            {
                GenerateIfNeeded(__instance);

                // TODO : Add map-based overheat and cooldown speed
                if (__instance.rpm >= overheatRPMThreshold)
                {
                    overheatCount = Mathf.MoveTowards(overheatCount, MAX_OVERHEAT, Time.fixedDeltaTime);
                }
                else
                {
                    // progressive cooldown
                    float rpmPercent = 1;

                    if (__instance.rpm >= Main.settings.overheatRPMBalancePercent)
                    {
                        rpmPercent = Mathf.InverseLerp(
                            Main.settings.overheatRPMThresholdPercent,
                            Main.settings.overheatRPMBalancePercent,
                            __instance.rpm
                        );
                    }

                    // apply radiator condition
                    float radiatorCondition = GameEntryPoint.EventManager.playerManager.performanceDamageManager
                        .GetConditionOfPart(SystemToRepair.RADIATOR);
                    float cooldownSpeed = Main.settings.overheatCooldownSpeedMult * (0.5f + radiatorCondition) * Time.fixedDeltaTime;

                    overheatCount = Mathf.MoveTowards(overheatCount, 0, cooldownSpeed * rpmPercent);
                }

                if (overheatCount >= MAX_OVERHEAT)
                {
                    CarUtils.DamagePart(player, ENGINE_DAMAGE_RATE * Time.fixedDeltaTime, SystemToRepair.ENGINE);

                    if (overheatCount == MAX_OVERHEAT)
                    {
                        // TODO : Damage turbo here
                    }
                }
            });
        }

        static void GenerateIfNeeded(Drivetrain engine)
        {
            if (player == null)
            {
                player = GameEntryPoint.EventManager.playerManager.PlayerObject.GetComponent<PlayerCollider>();
                overheatCount = 0;

                int currentScene = SceneManager.GetActiveScene().buildIndex;
                var map = mapHeatMultipliers.Find(item => currentScene >= item.Item1 || currentScene <= item.Item2);
                mapMultiplier = map.Item3 / REFERENCE_TEMPERATURE;

                Refresh();
            }
        }

        public static void Refresh()
        {
            if (player == null)
                return;

            overheatRPMThreshold = GameEntryPoint.EventManager.playerManager.drivetrain.maxRPM * Main.settings.overheatRPMThresholdPercent / 100;
        }

        // __ Overheat __
        // currentRev > ProjectForward(maxCarSpeed / 10) /*find the right ratio*/ (turbo)

        // __ what do I need ? __
        // car rigidbody (forward speed)
    }
}