using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace BetterDamage
{
    static class WheelsManager
    {
        static GameEntryPoint entry;
        static PlayerCollider player;
        static List<Wheel> wheels;
        static bool onGround;

        static bool isReady => player != null && wheels != null && wheels.Count == 4;

        public static void OnUpdate()
        {
            if (GameModeManager.GameMode == GameModeManager.GAME_MODES.FREEROAM)
                return;

            if (entry == null)
                entry = GameObject.FindObjectOfType<GameEntryPoint>();

            if (entry == null ||
                Main.GetField<EventManager, GameEntryPoint>(entry, "eventManager", BindingFlags.Static) == null ||
                GameEntryPoint.EventManager.status != EventStatusEnums.EventStatus.UNDERWAY)
                return;

            GenerateIfNeeded();

            if (isReady)
            {
                bool currentOnGround = false;

                foreach (Wheel wheel in wheels)
                {
                    if (wheel.onGroundDown)
                    {
                        currentOnGround = true;
                        break;
                    }
                }

                if (!onGround && currentOnGround)
                {
                    Main.Log("Detected landing");

                    // TODO : call custom collision event

                    // Where could I find a ref to the player's rigidbody ?

                    //float landingForce = -collInfo.relativeVelocity.y;
                    //Main.Log("Landed with force : " + landingForce);

                    //if (Main.settings.enableLandingDamage && landingForce > Main.settings.minLandingThreshold)
                    //{
                    //    // tire puncture
                    //    if (Random.Range(0, 100) < Main.settings.landingPunctureProbability)
                    //    {
                    //        List<Wheel> wheels = Main.GetField<List<Wheel>, PlayerCollider>(__instance, "wheels", BindingFlags.Instance);
                    //        List<Wheel> availableWheels = new List<Wheel>();

                    //        wheels.ForEach(wheel =>
                    //        {
                    //            if (!wheel.tirePuncture)
                    //                availableWheels.Add(wheel);
                    //        });

                    //        // all wheels are punctured => abort
                    //        if (availableWheels.Count == 0)
                    //        {
                    //            Main.Log("All wheels are punctured. Aborting.");
                    //            return;
                    //        }

                    //        CarUtils.PunctureTire(__instance, availableWheels[Random.Range(0, availableWheels.Count)]);
                    //    }
                    //    else // damage suspension (we don't damage suspension and puncture tire at the same time)
                    //    {
                    //        float magnitudePercent = Mathf.InverseLerp(
                    //            Main.settings.minLandingThreshold,
                    //            Main.settings.maxLandingThreshold,
                    //            landingForce
                    //        );

                    //        CarUtils.DamagePart(__instance, magnitudePercent, SystemToRepair.SUSPENSION);
                    //    }
                    //}
                }

                onGround = currentOnGround;
            }
        }

        static void GenerateIfNeeded()
        {
            if (player == null)
                player = GameObject.FindObjectOfType<PlayerCollider>();

            if (wheels == null || wheels.Find(wheel => wheel == null) != null)
            {
                if (player != null)
                {
                    wheels = Main.GetField<List<Wheel>, PlayerCollider>(player, "wheels", BindingFlags.Instance);
                    onGround = false;

                    if (wheels == null)
                    {
                        Main.Error("Couldn't retrive refs to wheels. This is a major bug.");
                        return;
                    }

                    foreach (Wheel wheel in wheels)
                    {
                        if (wheel.onGroundDown)
                        {
                            onGround = true;
                            break;
                        }
                    }
                }
            }
        }
    }
}
