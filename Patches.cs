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

    [HarmonyPatch(typeof(PlayerCollider), "CheckForPunctureAndPerformanceDamage")]
    static class CarDamageManager
    {
        const float WHEEL_WIDTH = 0.7f;
        const float WHEEL_FRONT_PERCENT = 0.45f;
        const float MAX_TILT_DAMAGE = 0.5f;

        public static float tiltToApply;

        static PlayerCollider player;
        static Vector2[] wheelsSlice; // front 0 1 / back 2 3
        static Vector2 radiatorSlice;

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
                    GenerateIfNeeded(__instance);

                    if (isPerformanceDamageEnabled)
                    {
                        Transform player = GameEntryPoint.EventManager.playerManager.PlayerObject.transform;
                        float damageAngle = Vector3.SignedAngle(player.forward, collInfo.contacts[0].point - player.position, player.up);
                        Main.Log("Damage angle : " + damageAngle);

                        float MAX_CRASH_MAGNITUDE = Main.GetField<float, PlayerCollider>(
                            __instance,
                            "MAX_MAGNITUDE_CRASH",
                            BindingFlags.Instance
                        );

                        float magnitudePercent = Mathf.InverseLerp(MIN_CRASH_MAGNITUDE, MAX_CRASH_MAGNITUDE, collInfo.relativeVelocity.magnitude);

                        if (damageAngle > radiatorSlice.x && damageAngle < radiatorSlice.y)
                        {
                            CarUtils.DamagePart(__instance, magnitudePercent, SystemToRepair.RADIATOR);
                            return;
                        }

                        for (int i = 0; i < wheelsSlice.Length; i++)
                        {
                            Vector2 slice = wheelsSlice[i];

                            if (damageAngle > Mathf.Min(slice.x, slice.y) && damageAngle < Mathf.Max(slice.x, slice.y))
                            {
                                // if we are on the front suspensions
                                if (i <= 1)
                                {
                                    // 0 = -1 / 1 = 1
                                    float side = (i - 0.5f) * 2;
                                    tiltToApply = magnitudePercent * MAX_TILT_DAMAGE * side;
                                    Main.Log("New tilt damage : " + tiltToApply);
                                }

                                CarUtils.DamagePart(__instance, magnitudePercent, SystemToRepair.SUSPENSION);
                                break;
                            }
                        }
                    }

                    // int probability = Random.Range(0, 100);

                    // TODO : Do puncture check (check if we collided in the direction of wheels)
                    // TODO : Do damage headlights check (check if we collided in the direction of headlights)
                }
            });

            return false;
        }

        static void GenerateIfNeeded(PlayerCollider instance)
        {
            // makes sure we regenerate the angles when the car changes
            if (player != null && player == instance && wheelsSlice != null)
                return;

            player = instance;
            wheelsSlice = new Vector2[4];

            Wheel[] wheels = GameEntryPoint.EventManager.playerManager.axles.allWheels;
            Transform playerTr = GameEntryPoint.EventManager.playerManager.PlayerObject.transform;

            Vector3 frontCenterPoint = Vector3.Lerp(wheels[0].transform.position, wheels[1].transform.position, 0.5f);
            Vector3 backCenterPoint = Vector3.Lerp(wheels[2].transform.position, wheels[3].transform.position, 0.5f);
            float wheelDistance = (wheels[1].transform.position - frontCenterPoint).magnitude;

            // we only care about the 4 first wheels (front and back)
            for (int i = 0; i < 4; i++)
            {
                int side = wheels[i].transform.localPosition.x > 0 ? 1 : -1;
                float pointDistance = side * wheelDistance * (1 - WHEEL_FRONT_PERCENT);
                Vector3 frontPos;
                Vector3 backPos;

                if (i < 2)
                {
                    frontPos = frontCenterPoint + playerTr.right * pointDistance;
                    backPos = wheels[i].transform.position - playerTr.forward * WHEEL_WIDTH / 2;

                    Main.AddMarker(playerTr, frontPos + playerTr.forward * 0.9f, 0.1f);
                    Main.AddMarker(playerTr, backPos + playerTr.right * side * 0.1f, 0.1f);
                }
                else
                {
                    frontPos = wheels[i].transform.position + playerTr.forward * WHEEL_WIDTH / 2;
                    backPos = backCenterPoint + playerTr.right * pointDistance;

                    Main.AddMarker(playerTr, frontPos + playerTr.right * side * 0.1f, 0.1f);
                    Main.AddMarker(playerTr, backPos - playerTr.forward * 1f, 0.1f);
                }

                wheelsSlice[i] = new Vector2(
                    Vector3.SignedAngle(playerTr.forward, frontPos - playerTr.position, playerTr.up),
                    Vector3.SignedAngle(playerTr.forward, backPos - playerTr.position, playerTr.up)
                );

                Main.Log("Wheel " + i + " angle :\nmin : " + wheelsSlice[i].x + "\nmax : " + wheelsSlice[i].y);
            }

            radiatorSlice = new Vector2(wheelsSlice[0].x, wheelsSlice[1].x);
            Main.Log("Radiator angle :\nmin : " + radiatorSlice.x + "\nmax : " + radiatorSlice.y);
        }
    }

    [HarmonyPatch(typeof(Arcader), "TryToStickLanding")]
    static class LandingDamageManager
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

    [HarmonyPatch(typeof(SteeringPerfomanceDamage), nameof(SteeringPerfomanceDamage.SetPerformance))]
    static class SteeringDamageManager
    {
        static float lastDamage;

        static bool Prefix(SteeringPerfomanceDamage __instance)
        {
            if (!Main.enabled)
                return true;

            Main.Try(() =>
            {
                float tilt = Main.GetField<float, SteeringPerfomanceDamage>(__instance, "steeringAlignmentEffect", BindingFlags.Instance);

                if (__instance.PerformanceCondition == PerformanceDamage.MAX_PERFORMANCE_CONDITION)
                    Main.SetField<float, SteeringPerfomanceDamage>(__instance, "steeringAlignmentEffect", BindingFlags.Instance, 0);
                else
                {
                    float currentDamage = PerformanceDamage.MAX_PERFORMANCE_CONDITION - __instance.PerformanceCondition;

                    if (CarDamageManager.tiltToApply == 0 && lastDamage == currentDamage)
                        return;

                    // mapped -1 to 1
                    float tiltPercent = lastDamage == 0 ? 0 : tilt * 20 / lastDamage;
                    Main.Log("Current tilt percent : " + tiltPercent);
                    tiltPercent = Mathf.Clamp(tiltPercent + CarDamageManager.tiltToApply, -1, 1);
                    Main.Log("New tilt percent : " + tiltPercent);
                    tilt = Mathf.Clamp(tiltPercent * currentDamage / 10, -0.1f, 0.1f) * 0.5f;

                    Main.Log("Applied tilt : " + tilt);

                    // aplly
                    Main.SetField<float, SteeringPerfomanceDamage>(__instance, "steeringAlignmentEffect", BindingFlags.Instance, tilt);
                    GameEntryPoint.EventManager.playerManager.PlayerObject.GetComponent<AxisCarController>().SteeringOutOfAlignmentEffect = tilt;

                    CarDamageManager.tiltToApply = 0;
                    lastDamage = currentDamage;
                }
            });

            return false;
        }

        public static void Reset() => lastDamage = 0;
    }

    [HarmonyPatch(typeof(PerformanceDamage), nameof(PerformanceDamage.Repair))]
    static class SteeringRepairPatch
    {
        static void Prefix(PerformanceDamage __instance)
        {
            if (!Main.enabled || !(__instance is SteeringPerfomanceDamage))
                return;

            Main.Log("Detected repair part");

            Main.Try(() =>
            {
                Main.SetField<float, SteeringPerfomanceDamage>(
                    __instance as SteeringPerfomanceDamage,
                    "steeringAlignmentEffect",
                    BindingFlags.Instance,
                    0
                );

                SteeringDamageManager.Reset();
            });
        }
    }
}