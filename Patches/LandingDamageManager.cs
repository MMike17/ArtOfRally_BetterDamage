using HarmonyLib;
using System.Collections;
using System.Reflection;
using UnityEngine;

using static EventStatusEnums;
using static RepairsManagerUI;
using Random = UnityEngine.Random;

namespace BetterDamage
{
    [HarmonyPatch(typeof(Arcader), "TryToStickLanding")]
    static class LandingDamageManager
    {
        static PlayerCollider collider;
        static Coroutine waitRoutine;

        static void Prefix(Arcader __instance)
        {
            if (!Main.enabled ||
                GameModeManager.GameMode == GameModeManager.GAME_MODES.FREEROAM ||
                !Main.settings.enableLandingDamage ||
                GameEntryPoint.EventManager.status != EventStatus.UNDERWAY)
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

            if (!Main.settings.disableInfoLogs)
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
                    Random.Range(0f, 100f) < Main.settings.landingPunctureProbability)
                {
                    Wheel[] wheels = GameEntryPoint.EventManager.playerManager.axles.allWheels;

                    foreach (Wheel wheel in wheels)
                    {
                        if (wheel.tirePuncture)
                        {
                            if (!Main.settings.disableInfoLogs)
                                Main.Log("A tire is already punctured. Aborting.");

                            yield break;
                        }
                    }

                    CarUtils.PunctureTire(collider, wheels[Random.Range(0, wheels.Length)]);
                }
                else // damage suspension (we don't damage suspension and puncture tire at the same time)
                {
                    magnitudePercent *= Main.settings.landingDamageMultiplier * 0.1f;
                    CarUtils.DamagePart(collider, magnitudePercent, SystemToRepair.SUSPENSION);
                }
            }

            waitRoutine = null;
        }
    }
}