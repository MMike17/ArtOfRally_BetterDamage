using HarmonyLib;
using System.Collections;
using UnityEngine;

using static EventStatusEnums;
using static RepairsManagerUI;

namespace BetterDamage.Patches
{
    [HarmonyPatch(typeof(Drivetrain))]
    static class TransmissionManager
    {
        const float GEARBOX_DAMAGE_RATE = 0.1f;
        const float ENGINE_DAMAGE_RATE = 0.05f;
        const float SPEED_RATIO = 2.2f;

        static Coroutine shiftRoutine;
        static PlayerCollider player;
        static int lastGear;

        [HarmonyPatch(nameof(Drivetrain.Shift))]
        static void Postfix(Drivetrain __instance, int m_gear)
        {
            if (!Main.enabled || GameEntryPoint.EventManager.status != EventStatus.UNDERWAY || !Main.settings.enableGearboxDamage)
                return;

            // ignore neutral
            if (__instance.gear != 1)
                lastGear = __instance.gear;

            Main.Try(() =>
            {
                GenerateIfNeeded();

                if (!Main.settings.disableInfoLogs)
                    Main.Log("Detected shifting (from : " + __instance.gear + " to " + m_gear + ")");

                // ignore neutral
                if (__instance.gear != m_gear)
                {
                    if (shiftRoutine != null)
                        __instance.StopCoroutine(nameof(WaitForEndOfShifting));

                    __instance.StartCoroutine(WaitForEndOfShifting(__instance, m_gear));
                }
            });
        }

        static IEnumerator WaitForEndOfShifting(Drivetrain engine, int targetGear)
        {
            if (targetGear == 1)
                yield break;

            if (!Main.settings.disableInfoLogs)
                Main.Log("Waiting for end of shift");

            yield return new WaitUntil(() => engine.changingGear == false);

            AxisCarController controller = GameEntryPoint.EventManager.playerManager.PlayerObject.GetComponent<AxisCarController>();
            float ratio;
            float timer = 0;
            float maxRPM = 0;

            while (timer < 0.5f)
            {
                timer += Time.deltaTime;

                if (engine.clutch.GetClutchPosition() > 0)
                {
                    // money shift reverse
                    if (targetGear == 0)
                    {
                        if (OverheatManager.carSpeed * SPEED_RATIO > Main.settings.reverseSpeedThreshold)
                        {
                            ratio = OverheatManager.carSpeed * SPEED_RATIO / Main.settings.reverseSpeedThreshold / 5;

                            if (!Main.settings.disableInfoLogs)
                                Main.Log("Detected forced reverse : " + ratio);

                            CarUtils.DamagePart(player, GEARBOX_DAMAGE_RATE * ratio, SystemToRepair.GEARBOX);
                            shiftRoutine = null;
                            yield break;
                        }
                    }
                    else if (targetGear < lastGear && engine.rpm > engine.shiftUpRPM) // over rev
                    {
                        ratio = engine.rpm / engine.shiftUpRPM;

                        if (!Main.settings.disableInfoLogs)
                            Main.Log("Detected over rev with ratio : " + ratio);

                        if (maxRPM == 0)
                            maxRPM = engine.rpm;
                        else if (maxRPM < engine.rpm)
                            maxRPM = engine.rpm;
                        else
                        {
                            CarUtils.DamagePart(player, GEARBOX_DAMAGE_RATE * ratio, SystemToRepair.GEARBOX);
                            CarUtils.DamagePart(player, ENGINE_DAMAGE_RATE * ratio, SystemToRepair.ENGINE);
                            shiftRoutine = null;
                            yield break;
                        }
                    }
                }

                yield return null;
            }

            ratio = engine.shiftDownRPM / engine.rpm;

            // engine lugging
            if (targetGear != 0 && lastGear < targetGear && controller.throttleInput > 0 && engine.rpm < engine.shiftDownRPM)
            {
                if (!Main.settings.disableInfoLogs)
                    Main.Log("Detected under rev with ratio : " + ratio);

                CarUtils.DamagePart(player, GEARBOX_DAMAGE_RATE * ratio, SystemToRepair.GEARBOX);
                CarUtils.DamagePart(player, ENGINE_DAMAGE_RATE * ratio, SystemToRepair.ENGINE);
            }

            shiftRoutine = null;
        }

        static void GenerateIfNeeded()
        {
            if (player == null)
                player = GameEntryPoint.EventManager.playerManager.PlayerObject.GetComponent<PlayerCollider>();
        }
    }
}