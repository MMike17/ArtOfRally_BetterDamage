using HarmonyLib;
using System.Collections;
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
        const int MIN_TURBO_SPEED = 10; // 20 km/h
        const int MAX_TURBO_SPEED = 50; // 100 km/h
        const float TURBO_DAMAGE_RATE = 0.1f;
        const float GEARBOX_DAMAGE_RATE = 0.1f;

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
        static Coroutine shiftRoutine;

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

        [HarmonyPatch(nameof(Drivetrain.Shift))]
        static void Postfix(Drivetrain __instance, int m_gear)
        {
            if (!Main.enabled || Main.InReplay || !Main.settings.enableGearboxDamage)
                return;

            Main.Try(() =>
            {
                if (!Main.settings.disableInfoLogs)
                    Main.Log("Detected shifting (from : " + __instance.gear + " to " + m_gear + ")");

                // ignore neutral
                if (__instance.gear != 1 && m_gear != 1 && __instance.gear != m_gear)
                {
                    if (shiftRoutine != null)
                        __instance.StopCoroutine(nameof(WaitForEndOfShifting));

                    __instance.StartCoroutine(WaitForEndOfShifting(__instance, __instance.gear < m_gear));
                }
            });
        }

        static IEnumerator WaitForEndOfShifting(Drivetrain engine, bool shiftUp)
        {
            if (!Main.settings.disableInfoLogs)
                Main.Log("Waiting for end of shift");

            yield return new WaitUntil(() => engine.changingGear == false);
            yield return new WaitForSeconds(0.5f);

            Main.Try(() =>
            {
                AxisCarController controller = GameEntryPoint.EventManager.playerManager.PlayerObject.GetComponent<AxisCarController>();

                if (shiftUp && controller.throttleInput > 0 && engine.rpm < engine.shiftDownRPM)
                {
                    if (!Main.settings.disableInfoLogs)
                        Main.Log("Detected under rev with ratio : " + engine.shiftDownRPM / engine.rpm);

                    CarUtils.DamagePart(player, GEARBOX_DAMAGE_RATE * engine.shiftDownRPM / engine.rpm, SystemToRepair.GEARBOX);
                }
                else if (engine.rpm > engine.shiftUpRPM)
                {
                    if (!Main.settings.disableInfoLogs)
                        Main.Log("Detected over rev with ratio : " + engine.rpm / engine.shiftUpRPM);

                    CarUtils.DamagePart(player, GEARBOX_DAMAGE_RATE * engine.rpm / engine.shiftUpRPM, SystemToRepair.GEARBOX);
                }

                shiftRoutine = null;
            });
        }

        public static void Refresh()
        {
            if (player == null)
                return;

            Drivetrain engine = GameEntryPoint.EventManager.playerManager.drivetrain;
            overheatRPMThreshold = engine.maxRPM * Main.settings.overheatRPMThresholdPercent / 100;

            int currentScene = SceneManager.GetActiveScene().buildIndex;
            float currentTemp = mapHeatMultipliers.Find(item => currentScene >= item.min || currentScene <= item.max).temp;

            // min = index 3 / max = index 6
            float mapHeatPercent = Mathf.InverseLerp(mapHeatMultipliers[3].temp, mapHeatMultipliers[6].temp, currentTemp);
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