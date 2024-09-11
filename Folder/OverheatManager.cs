using HarmonyLib;
using UnityEngine;

namespace BetterDamage
{
    [HarmonyPatch(typeof(Drivetrain))]
    static class OverheatManager
    {
        static float overheatRPMThreshold;
        static float overheatCount;

        [HarmonyPatch("FixedUpdate")]
        static void Postfix(Drivetrain __instance)
        {
            if (!Main.enabled || !Main.settings.enableOverheatDamage)
                return;

            Main.Try(() =>
            {
                GenerateIfNeeded(__instance);

                if (__instance.rpm >= overheatRPMThreshold)
                    overheatCount += Time.fixedDeltaTime;
                else
                {
                    // cooldown
                }

                //if (overheatCount >= )
                //{
                //    // damage
                //}
            });
        }

        static void GenerateIfNeeded(Drivetrain engine)
        {
            if (overheatRPMThreshold == 0)
                overheatRPMThreshold = engine.maxRPM * Main.settings.overheatRPMThresholdPercent / 100;
        }

        public static void Refresh()
        {
            overheatRPMThreshold = 0;
        }

        // __ Overheat __
        // currentRev > ProjectForward(maxCarSpeed / 10) /*find the right ratio*/
        // add to overheat counter => goes up when overheat / goes down on update (get cooling speed)
        // reduce cooling speed depending on radiator state

        // Add map-based overheat and cooldown speed

        // what do I need ?

        // engine max RPM
        // engine current RPM
        // car rigidbody (forward speed)
        // overheat speed
        // cooldown speed
        // radiator state
        // overheat threshold
    }
}