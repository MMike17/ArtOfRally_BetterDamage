using HarmonyLib;


namespace BetterDamage
{
    // disable wear and tear
    [HarmonyPatch(typeof(PerformanceDamageManager), nameof(PerformanceDamageManager.DoWearAndTearStageDamage))]
    static class Patch
    {
        static bool Prefix() => !Main.enabled || !Main.settings.disableWearAndTear;
    }
}