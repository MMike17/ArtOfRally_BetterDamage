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
    // TODO : damage suspensions when bump (detect the direction of bump
    // TODO : damage turbo when overheat (detect when we overheat ? compare rev to forward speed / rev > ProjectForward(maxSpeed / 10))

    // replaces the way car damage is decided
    [HarmonyPatch(typeof(PlayerCollider), "CheckForPunctureAndPerformanceDamage")]
    static class PlayerCollider_CheckForPunctureAndPerformanceDamage_Patch
    {
        const float WHEEL_CHECK_ANGLE = 10;

        static Dictionary<Wheel, float> toWheelsAngles;
        static float radiatorAngle;

        static bool Prefix(PlayerCollider __instance, Collision collInfo)
        {
            if (!Main.enabled)
                return true;

            Main.Try(() =>
            {
                float MIN_CRASH_MAGNITUDE = Main.GetField<float, PlayerCollider>(__instance, "MIN_MAGNITUDE_CRASH", BindingFlags.Instance);
                bool isPerformanceDamageEnabled = Main.GetField<bool, PlayerCollider>(
                    __instance,
                    "isPerformanceDamageEnabled",
                    BindingFlags.Instance
                );

                if (GameModeManager.GameMode != GameModeManager.GAME_MODES.FREEROAM && collInfo.relativeVelocity.magnitude > MIN_CRASH_MAGNITUDE)
                {
                    GenerateIfNeeded();

                    if (isPerformanceDamageEnabled)
                    {
                        Transform player = GameEntryPoint.EventManager.playerManager.PlayerObject.transform;
                        float damageAngle = Vector3.SignedAngle(player.forward, collInfo.contacts[0].point - player.position, player.up);

                        if ((damageAngle > 0 ? damageAngle : -damageAngle) < radiatorAngle)
                        {
                            float MAX_CRASH_MAGNITUDE = Main.GetField<float, PlayerCollider>(
                                __instance,
                                "MAX_MAGNITUDE_CRASH",
                                BindingFlags.Instance
                            );

                            float magnitudePercent = Mathf.InverseLerp(
                                MIN_CRASH_MAGNITUDE,
                                MAX_CRASH_MAGNITUDE,
                                collInfo.relativeVelocity.magnitude
                            );

                            CarUtils.DamagePart(__instance, magnitudePercent, SystemToRepair.RADIATOR);
                            return;
                        }

                        // TODO : Damage suspensions
                    }

                    // int probability = Random.Range(0, 100);

                    // TODO : Do puncture check (check if we collided in the direction of wheels)
                    // TODO : Do damage headlights check (check if we collided in the direction of headlights)
                }
            });

            return false;
        }

        static void GenerateIfNeeded()
        {
            bool needsRefresh = toWheelsAngles == null;

            if (toWheelsAngles != null)
            {
                foreach (KeyValuePair<Wheel, float> pair in toWheelsAngles)
                {
                    if (pair.Key == null)
                        needsRefresh = true;
                }
            }

            if (radiatorAngle == 0)
                needsRefresh = true;

            if (!needsRefresh)
                return;

            toWheelsAngles = new Dictionary<Wheel, float>();

            Wheel[] wheels = GameEntryPoint.EventManager.playerManager.axles.allWheels;
            Transform player = GameEntryPoint.EventManager.playerManager.PlayerObject.transform;

            // we only care about the 4 first wheels (front and back)
            for (int i = 0; i < 4; i++)
            {
                float wheelAngle = Vector3.SignedAngle(player.forward, wheels[i].transform.position - player.position, player.up);
                toWheelsAngles.Add(wheels[i], wheelAngle);

                Main.Log("Wheel " + i + " angle :\nmin : " + (wheelAngle - WHEEL_CHECK_ANGLE) + "\nmax : " + (wheelAngle + WHEEL_CHECK_ANGLE));
            }

            radiatorAngle = toWheelsAngles[wheels[1]] - WHEEL_CHECK_ANGLE / 2;
            Main.Log("Radiator angle :\nmin : " + (-radiatorAngle) + "\nmax : " + radiatorAngle);
        }
    }

    // apply damage on landing (suspensions and tires)
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

            float landingForce = -GameEntryPoint.EventManager.playerManager.playerRigidBody.velocity.y;

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
                float magnitudePercent = Mathf.InverseLerp(
                    Main.settings.minLandingThreshold,
                    Main.settings.maxLandingThreshold,
                    landingForce
                );

                if (magnitudePercent * 100 >= Main.settings.landingPunctureThreshold &&
                    Random.Range(0, 100) < Main.settings.landingPunctureProbability)
                {
                    List<Wheel> availableWheels = new List<Wheel>();

                    foreach (Wheel wheel in GameEntryPoint.EventManager.playerManager.axles.allWheels)
                    {
                        if (!wheel.tirePuncture)
                            availableWheels.Add(wheel);
                    }

                    if (availableWheels.Count == 0)
                    {
                        Main.Log("All wheels are punctured. Aborting.");
                        yield break;
                    }

                    CarUtils.PunctureTire(collider, availableWheels[Random.Range(0, availableWheels.Count)]);
                }
                else // damage suspension (we don't damage suspension and puncture tire at the same time)
                {
                    magnitudePercent *= Main.settings.landingDamageMultiplier * 0.1f;
                    CarUtils.DamagePart(collider, magnitudePercent, SystemToRepair.SUSPENSION);
                }
            }
        }
    }
}