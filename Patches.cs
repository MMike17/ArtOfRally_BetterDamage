using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

using static RepairsManagerUI;
using Random = UnityEngine.Random;

namespace BetterDamage
{
    // Patch model
    // [HarmonyPatch(typeof(), nameof())]
    // [HarmonyPatch(typeof(), MethodType.)]
    // static class type_method_Patch
    // {
    // 	static void Prefix()
    // 	{
    // 		//
    // 	}

    // 	static void Postfix()
    // 	{
    // 		//
    // 	}
    // }

    // TARGETS :

    // TODO : damage tires when you drift (detect when we drift/slip / chance of puncture)
    // TODO : damage gearbox when you shift down and over rev / shift R when going forward / shift 1 when going back(complex to detect)
    // TODO : damage engine whe you over rev (detect over rev)
    // TODO : damage suspensions when bump or land (detect the direction of bump)
    // TODO : damage radiator when bump (detect the direction of bump)
    // TODO : damage turbo when overheat (detect when we overheat ? compare rev to forward speed / rev > ProjectForward(maxSpeed / 10))

    // replaces the way car damage is decided
    [HarmonyPatch(typeof(PlayerCollider), "CheckForPunctureAndPerformanceDamage")]
    static class PlayerCollider_CheckForPunctureAndPerformanceDamage_Patch
    {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            // skip execution
            if (Main.enabled)
                yield break;
            else
            {
                // execute normal code
                foreach (CodeInstruction instruction in instructions)
                    yield return instruction;
            }
        }

        static void Prefix(PlayerCollider __instance, Collision collInfo)
        {
            if (!Main.enabled)
                return;

            Main.Try(() =>
            {
                float MIN_CRASH_MAGNITUDE = Main.GetField<float, PlayerCollider>(__instance, "MIN_MAGNITUDE_CRASH", BindingFlags.Instance);
                bool isPerformanceDamageEnabled = Main.GetField<bool, PlayerCollider>(
                    __instance,
                    "isPerformanceDamageEnabled",
                    BindingFlags.Instance
                );

                if (GameModeManager.GameMode != GameModeManager.GAME_MODES.FREEROAM)
                {
                    bool crash = collInfo.relativeVelocity.magnitude > MIN_CRASH_MAGNITUDE;

                    if (isPerformanceDamageEnabled)
                    {
                        if (collInfo.collider.CompareTag("Road"))
                        {
                            // skip the normal damage calculations
                            return;
                        }
                        else if (crash)
                        {
                            //float MAX_CRASH_MAGNITUDE = Main.GetField<float, PlayerCollider>(
                            //    __instance,
                            //    "MAX_MAGNITUDE_CRASH",
                            //    BindingFlags.Instance
                            //);

                            // TODO : Damage radiator
                            // TODO : Damage suspensions
                        }
                    }

                    if (!crash)
                        return;

                    int probability = Random.Range(0, 100);

                    // TODO : Do puncture check (check if we collided in the direction of wheels)
                    // TODO : Do damage headlights check (check if we collided in the direction of headlights)
                }
            });
        }
    }
}