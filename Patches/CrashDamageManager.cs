using HarmonyLib;
using System.Reflection;
using UnityEngine;

using static RepairsManagerUI;
using Random = UnityEngine.Random;

namespace BetterDamage
{
    // TODO : Fix body damage super 

    [HarmonyPatch(typeof(PlayerCollider), "CheckForPunctureAndPerformanceDamage")]
    static class CrashDamageManager
    {
        const float WHEEL_WIDTH = 0.7f;
        const float WHEEL_FRONT_PERCENT = 0.45f;
        const float MAX_TILT_DAMAGE = 0.5f;
        const float BODY_DAMAGE_RATE = 0.1f;

        public static float tiltToApply;

        static PlayerCollider player;
        static Vector2[] wheelsSlice; // front 0 1 / back 2 3
        static Vector2 radiatorSlice;
        static Vector2 backSlice;

        static bool Prefix(PlayerCollider __instance, Collision collInfo)
        {
            if (!Main.enabled || Main.InReplay)
                return true;

            Main.Try(() =>
            {
                float MIN_CRASH_MAGNITUDE = Main.GetField<float, PlayerCollider>(__instance, "MIN_MAGNITUDE_CRASH", BindingFlags.Instance);

                if (GameModeManager.GameMode != GameModeManager.GAME_MODES.FREEROAM && collInfo.relativeVelocity.magnitude > MIN_CRASH_MAGNITUDE)
                {
                    GenerateIfNeeded(__instance);

                    bool isPerformanceDamageEnabled = Main.GetField<bool, PlayerCollider>(
                        __instance,
                        "isPerformanceDamageEnabled",
                        BindingFlags.Instance
                    );

                    Transform player = GameEntryPoint.EventManager.playerManager.PlayerObject.transform;
                    float damageAngle = Vector3.SignedAngle(player.forward, collInfo.contacts[0].point - player.position, player.up);

                    float MAX_CRASH_MAGNITUDE = Main.GetField<float, PlayerCollider>(__instance, "MAX_MAGNITUDE_CRASH", BindingFlags.Instance);
                    float magnitudePercent = Mathf.InverseLerp(MIN_CRASH_MAGNITUDE, MAX_CRASH_MAGNITUDE, collInfo.relativeVelocity.magnitude);

                    float probability = Random.Range(0f, 100f);

                    if (!Main.settings.disableInfoLogs)
                        Main.Log("Crash at angle : " + damageAngle + " with magnitude : " + Mathf.FloorToInt(magnitudePercent * 100) + "%");

                    if (isPerformanceDamageEnabled)
                    {
                        if (damageAngle > radiatorSlice.x && damageAngle < radiatorSlice.y)
                        {
                            if (isPerformanceDamageEnabled)
                                CarUtils.DamagePart(__instance, magnitudePercent, SystemToRepair.RADIATOR);

                            if (probability < Main.settings.crashHeadlightProbability)
                                GameEntryPoint.EventManager.playerManager.headlightManager.ReduceHeadlightStrength();
                        }

                        if (Main.settings.enableGearboxDamage && (damageAngle < backSlice.x || damageAngle > backSlice.y))
                            CarUtils.DamagePart(__instance, magnitudePercent, SystemToRepair.GEARBOX);

                        CarUtils.DamagePart(__instance, magnitudePercent * BODY_DAMAGE_RATE, SystemToRepair.CLEANCAR);
                    }

                    for (int i = 0; i < wheelsSlice.Length; i++)
                    {
                        Vector2 slice = wheelsSlice[i];

                        if (damageAngle > Mathf.Min(slice.x, slice.y) && damageAngle < Mathf.Max(slice.x, slice.y))
                        {
                            if (isPerformanceDamageEnabled)
                            {
                                // if we are on the front suspensions
                                if (i <= 1)
                                {
                                    // 0 = -1 / 1 = 1
                                    float side = (i - 0.5f) * 2;
                                    tiltToApply = magnitudePercent * MAX_TILT_DAMAGE * side;

                                    if (!Main.settings.disableInfoLogs)
                                        Main.Log("New tilt damage : " + tiltToApply);
                                }

                                CarUtils.DamagePart(__instance, magnitudePercent, SystemToRepair.SUSPENSION);
                                break;
                            }

                            if (probability < Main.settings.crashPunctureProbability)
                            {
                                CarUtils.PunctureTire(__instance, GameEntryPoint.EventManager.playerManager.axles.allWheels[i]);
                                break;
                            }
                        }
                    }
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

                if (!Main.settings.disableInfoLogs)
                    Main.Log("Wheel " + i + " angle :\nmin : " + wheelsSlice[i].x + "\nmax : " + wheelsSlice[i].y);
            }

            radiatorSlice = new Vector2(wheelsSlice[0].x, wheelsSlice[1].x);

            if (!Main.settings.disableInfoLogs)
                Main.Log("Radiator angle :\nmin : " + radiatorSlice.x + "\nmax : " + radiatorSlice.y);

            backSlice = new Vector2(wheelsSlice[2].y, wheelsSlice[3].y);

            if (!Main.settings.disableInfoLogs)
                Main.Log("Gearbox back angle :\nmin : " + backSlice.x + "\nmax : " + backSlice.y);
        }
    }
}