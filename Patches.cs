using HarmonyLib;
using System.Collections;
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

    [HarmonyPatch(typeof(Arcader), "TryToStickLanding")]
    static class Arcader_TryToStickLanding_Patch
    {
        static PlayerCollider collider;
        static Coroutine waitRoutine;

        static void Prefix(Arcader __instance)
        {
            if (!Main.enabled ||
                GameModeManager.GameMode == GameModeManager.GAME_MODES.FREEROAM ||
                GameEntryPoint.EventManager.status != EventStatusEnums.EventStatus.UNDERWAY ||
                !Main.settings.enableLandingDamage)
                return;

            if (waitRoutine != null)
                __instance.StopCoroutine("WaitForLanding");

            waitRoutine = __instance.StartCoroutine(WaitForLanding(__instance));
        }

        static IEnumerator WaitForLanding(Arcader __instance)
        {
            yield return new WaitForSeconds(0.1f);
            yield return new WaitUntil(() => !Main.GetField<bool, Arcader>(__instance, "isCarAirborne", BindingFlags.Instance));

            Rigidbody rigidbody = Main.GetField<Rigidbody, Arcader>(__instance, "body", BindingFlags.Instance);
            float landingForce = -rigidbody.velocity.y;

            Main.Log("Detected landing with force : " + landingForce);

            if (landingForce > Main.settings.minLandingThreshold)
            {
                if (collider == null)
                    collider = GameObject.FindObjectOfType<PlayerCollider>();

                if (collider == null)
                {
                    Main.Error("Couldn't find PlayerCollider. This is a major bug.");
                    yield break;
                }

                // tire puncture
                if (Random.Range(0, 100) < Main.settings.landingPunctureProbability)
                {
                    List<Wheel> wheels = Main.GetField<List<Wheel>, PlayerCollider>(collider, "wheels", BindingFlags.Instance);
                    List<Wheel> availableWheels = new List<Wheel>();

                    wheels.ForEach(wheel =>
                    {
                        if (!wheel.tirePuncture)
                            availableWheels.Add(wheel);
                    });

                    if (availableWheels.Count == 0)
                    {
                        Main.Log("All wheels are punctured. Aborting.");
                        yield break;
                    }

                    CarUtils.PunctureTire(collider, availableWheels[Random.Range(0, availableWheels.Count)]);
                }
                else // damage suspension (we don't damage suspension and puncture tire at the same time)
                {
                    float magnitudePercent = Mathf.InverseLerp(
                        Main.settings.minLandingThreshold,
                        Main.settings.maxLandingThreshold,
                        landingForce
                    ) * Main.settings.landingDamageMultiplier * 0.07f;

                    CarUtils.DamagePart(collider, magnitudePercent, SystemToRepair.SUSPENSION);
                }
            }
        }
    }
}