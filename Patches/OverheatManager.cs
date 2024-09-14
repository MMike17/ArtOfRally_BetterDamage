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
        const float MAX_OVERHEAT = 2f;
        const float ENGINE_DAMAGE_RATE = 0.02f;
        const int MIN_TURBO_SPEED = 4; // 30 km/h => 8 m/s
        const int MAX_TURBO_SPEED = 14; // 100 km/hm => 28 m/s
        const float TURBO_DAMAGE_RATE = 0.1f;

        static List<(int min, int max, float temp)> mapHeatMultipliers = new List<(int, int, float)>()
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
        static float overheatCount;
        static float rpmBalance;

        [HarmonyPatch("FixedUpdate")]
        static void Postfix(Drivetrain __instance)
        {
            if (!Main.enabled || Main.InReplay || !Main.settings.enableOverheatDamage)
                return;

            Main.Try(() =>
            {
                GenerateIfNeeded();

                if (__instance.rpm >= overheatRPMThreshold)
                    overheatCount = Mathf.MoveTowards(overheatCount, MAX_OVERHEAT, Main.settings.overheatSpeedMult * Time.fixedDeltaTime);
                else
                {
                    // progressive cooldown
                    float rpmPercent = 1;

                    if (__instance.rpm >= rpmBalance)
                        rpmPercent = Mathf.InverseLerp(Main.settings.overheatRPMThresholdPercent, rpmBalance, __instance.rpm);

                    // apply radiator condition
                    float radiatorCondition = GameEntryPoint.EventManager.playerManager.performanceDamageManager
                        .GetConditionOfPart(SystemToRepair.RADIATOR);
                    float cooldownSpeed = Main.settings.overheatCooldownSpeedMult * (0.5f + radiatorCondition) * Time.fixedDeltaTime;

                    overheatCount = Mathf.MoveTowards(overheatCount, 0, cooldownSpeed * rpmPercent);
                }

                if (overheatCount >= MAX_OVERHEAT)
                {
                    CarUtils.DamagePart(player, ENGINE_DAMAGE_RATE * Time.fixedDeltaTime, SystemToRepair.ENGINE);

                    if (CarManager.GetCarStatsForCar(GameModeManager.GetSeasonDataCurrentGameMode().SelectedCar).Aspiration !=
                        CarSpecs.EngineAspiration.NATURAL)
                    {
                        float forwardSpeed = Vector3.Project(
                            GameEntryPoint.EventManager.playerManager.playerRigidBody.velocity,
                            player.transform.forward
                        ).magnitude;

                        float rpmPercent = Mathf.InverseLerp(overheatRPMThreshold, __instance.maxRPM, __instance.rpm);
                        float speedPercent = Mathf.InverseLerp(MIN_TURBO_SPEED, MAX_TURBO_SPEED, forwardSpeed);

                        if (speedPercent < rpmPercent)
                        {
                            float damagePercent = rpmPercent - speedPercent;
                            CarUtils.DamagePart(player, damagePercent * TURBO_DAMAGE_RATE * Time.fixedDeltaTime, SystemToRepair.TURBO);
                        }
                    }
                }
            });
        }

        static void GenerateIfNeeded()
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

            Drivetrain engine = GameEntryPoint.EventManager.playerManager.drivetrain;
            overheatRPMThreshold = engine.maxRPM * Main.settings.overheatRPMThresholdPercent / 100;

            int currentScene = SceneManager.GetActiveScene().buildIndex;
            (int min, int max, float temp) map = mapHeatMultipliers.Find(item => currentScene >= item.min || currentScene <= item.max);

            // in case someone has custom maps
            if (map == (0, 0, 0f))
            {
                Main.Error("Couldn't find temperature for scene : " + SceneManager.GetActiveScene().name + ". Defaulting to exact value.");
                rpmBalance = engine.maxRPM * Main.settings.overheatRPMBalancePercent / 100;
                return;
            }

            // min = index 3 / max = index 6
            float mapHeatPercent = Mathf.InverseLerp(mapHeatMultipliers[3].temp, mapHeatMultipliers[6].temp, map.temp);
            float thresholdDistance = (Main.settings.overheatRPMThresholdPercent - Main.settings.overheatRPMBalancePercent) / 2;

            rpmBalance = Mathf.Lerp(
                Main.settings.overheatRPMBalancePercent + thresholdDistance, // cold 
                Main.settings.overheatRPMBalancePercent - thresholdDistance, // hot
                mapHeatPercent
            );
            rpmBalance *= engine.maxRPM / 100;
        }
    }
}