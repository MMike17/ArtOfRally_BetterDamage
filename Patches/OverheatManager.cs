using HarmonyLib;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

using static ConditionTypes;
using static EventStatusEnums;
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

        public static int lastSceneIndex;

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
            if (!Main.enabled || !Main.settings.enableOverheatDamage || GameEntryPoint.EventManager.status != EventStatus.UNDERWAY)
                return;

            Main.Try(() =>
            {
                if (!CheckReady())
                    return;

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
                            CarUtils.DamagePart(
                                player,
                                damagePercent * TURBO_DAMAGE_RATE * Main.settings.overheatTurboDamageMult * Time.fixedDeltaTime,
                                SystemToRepair.TURBO
                            );
                        }
                    }
                }
            });
        }

        static bool CheckReady()
        {
            if (player == null && GameEntryPoint.EventManager.playerManager.PlayerObject != null)
            {
                player = GameEntryPoint.EventManager.playerManager.PlayerObject.GetComponent<PlayerCollider>();

                overheatCount = 0;
                Refresh();
            }

            return player != null;
        }

        public static void Refresh()
        {
            if (player == null)
                return;

            Drivetrain engine = GameEntryPoint.EventManager.playerManager.drivetrain;
            overheatRPMThreshold = engine.maxRPM * Main.settings.overheatRPMThresholdPercent / 100;
            (int min, int max, float temp) map = mapHeatMultipliers.Find(item => lastSceneIndex >= item.min && lastSceneIndex <= item.max);

            // in case someone has custom maps
            if (map == (0, 0, 0f))
            {
                Main.Error("Couldn't find temperature for scene : " + SceneManager.GetActiveScene().name + ". Defaulting to exact value.");
                rpmBalance = engine.maxRPM * Main.settings.overheatRPMBalancePercent / 100;
                return;
            }

            // min = index 3 / max = index 6
            float mapHeatPercent = Mathf.InverseLerp(
                mapHeatMultipliers[3].temp + GetWeatherTempMod(Weather.Snow),
                mapHeatMultipliers[6].temp + GetWeatherTempMod(Weather.Afternoon),
                map.temp + GetWeatherTempMod(GameModeManager.GetRallyDataCurrentGameMode().GetCurrentStage().Weather)
            );

            float thresholdDistance = (Main.settings.overheatRPMThresholdPercent - Main.settings.overheatRPMBalancePercent) / 2;

            rpmBalance = Mathf.Lerp(
                Main.settings.overheatRPMBalancePercent + thresholdDistance, // cold 
                Main.settings.overheatRPMBalancePercent - thresholdDistance, // hot
                mapHeatPercent
            );
            rpmBalance *= engine.maxRPM / 100;
        }

        static int GetWeatherTempMod(Weather weather)
        {
            switch (weather)
            {
                case Weather.Snow:
                    return -3;

                case Weather.Rain:
                    return -2;

                case Weather.Night:
                case Weather.Fog:
                    return -1;

                case Weather.Morning:
                case Weather.Sunset:
                    return 0;

                case Weather.Afternoon:
                    return 1;
            }

            return 0;
        }
    }
}