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

        static Coroutine shiftRoutine;
        static PlayerCollider player;

        [HarmonyPatch(nameof(Drivetrain.Shift))]
        static void Postfix(Drivetrain __instance, int m_gear)
        {
            if (!Main.enabled || GameEntryPoint.EventManager.status != EventStatus.UNDERWAY || !Main.settings.enableGearboxDamage)
                return;

            Main.Try(() =>
            {
                GenerateIfNeeded();

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

        static void GenerateIfNeeded()
        {
            if (player == null)
                player = GameEntryPoint.EventManager.playerManager.PlayerObject.GetComponent<PlayerCollider>();
        }
    }
}